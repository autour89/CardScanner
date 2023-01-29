using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Accelerate;
using AVFoundation;
using CardScanner.ViewModels;
using CardScanner.Views.Controls;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using HomeKit;
using ImageIO;
using JavaScriptCore;
using ObjCRuntime;
using UIKit;
using Vision;
using Xamarin.Essentials;
using static CoreFoundation.DispatchSource;

namespace CardScanner.Views
{
    public enum CardValueType { Number, Name, Expiration }

    public partial class PaymentCardScanner : UIViewController, IAVCaptureVideoDataOutputSampleBufferDelegate
    {
        private const string CaptureQueueLabel = "Video Queue";
        private const int CardMinLength = 13;
        private const int CardMaxLength = 19;
        private const int NameMinLength = 6;
        private const int NameMaxLength = 30;
        private const int DateLength = 5;
        const int MaxQueueCount = 30;
        const int MaxCandidates = 100;
        const string needAccessTitle = "Need Camera Access";
        const string cameraAccessMessage = "Camera access is required to make full use of this app.";
        const string allowCameraTitle = "Allow Camera";
        const string cancelTitle = "Cancel";
        const string addCardTitle = "Add Card";

        int _queueCount;
        Dictionary<CardValueType, List<string>> _candidates;
        ICollection<int> _cardNumberSplitParts;
        ICollection<int> _cardNameSplitParts;

        private AVCaptureDevice _camera;
        private AVCaptureSession _captureSession;
        private AVCaptureVideoPreviewLayer _previewLayer;

        private UITapGestureRecognizer _cardNumberGestureRecognizer;
        private UITapGestureRecognizer _cardNameGestureRecognizer;
        private UITapGestureRecognizer _cardExpirationGestureRecognizer;
        private CGRect _cardRect;

        public Action<string, string, DateTime> OnCompleted { get; set; }

        public string CardNumber { get; private set; }

        public string CardName { get; private set; }

        public DateTime Expiration { get; private set; }

        private bool _flashOn;
        public bool FlashOn
        {
            get => _flashOn;
            private set
            {
                _flashOn = value;

                FlashlightButton.Selected = value;

                FlashlightButton.TintColor = value ? UIColor.White : UIColor.Gray.ColorWithAlpha(.5f);

                if (_camera?.HasTorch ?? false)
                {
                    TapticFeedback();

                    try
                    {
                        _camera.LockForConfiguration(out _);

                        _camera.TorchMode = value ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
                    }
                    finally
                    {
                        _camera.UnlockForConfiguration();
                    }
                }
            }
        }

        private bool _runSession;
        private bool RunSession
        {
            get => _runSession;
            set
            {
                _runSession = value;

                DispatchQueue.DefaultGlobalQueue.DispatchAsync(() =>
                {
                    if (value)
                    {
                        _captureSession?.StartRunning();
                    }
                    else
                    {
                        _captureSession?.StopRunning();
                    }

                    InvokeOnMainThread(() => GuideView.Hidden = !value);
                });
            }
        }

        public PaymentCardScanner() : base(nameof(PaymentCardScanner), default) { }

        public static UINavigationController ScannerView => new UINavigationController(new PaymentCardScanner())
        {
            NavigationBarHidden = false,
            Title = addCardTitle,
            ModalPresentationStyle = UIModalPresentationStyle.FullScreen
        };

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialise();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            Unload();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);

            SetCameraOrientation();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            _previewLayer.Frame = View.Bounds;
        }


        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection) => OutputRecorder(sampleBuffer);

        partial void OnCloseTap()
        {
            if (FlashOn)
            {
                FlashOn = false;
            }

            DismissViewController(true, () => RunSession = false);
        }

        partial void OnConfirnTap()
        {
            OnCloseTap();

            OnCompleted?.Invoke(CardNumber, CardName, Expiration);
        }

        partial void OnFlashlightTap() => FlashOn = !FlashOn;

        private void Initialise()
        {
            _candidates = new Dictionary<CardValueType, List<string>>();

            _candidates[CardValueType.Number] = new List<string>();
            _candidates[CardValueType.Name] = new List<string>();
            _candidates[CardValueType.Expiration] = new List<string>();

            _cardNumberSplitParts = new List<int> { 3, 4 };

            _cardNameSplitParts = new List<int> { 2, 3 };

            _captureSession = new AVCaptureSession();

            _previewLayer = new AVCaptureVideoPreviewLayer(session: _captureSession)
            {
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };

            SetupStyles();

            ResetForm();

            CheckPermissions();
        }

        private void SetupStyles()
        {
            GuideView.AddBorder();

            GuideView.Hidden = true;

            View.BackgroundColor = UIColor.White;

            CloseButton.TintColor = UIColor.Gray;

            FlashlightButton.TintColor = UIColor.Gray.ColorWithAlpha(.5f);

            CardHeightConstraint.Constant = CardWidthConstraint.Constant - (CardWidthConstraint.Constant * .37f);

            _cardRect = new CGRect(GuideView.Card.Frame.X, GuideView.Card.Frame.Y, CardWidthConstraint.Constant, CardHeightConstraint.Constant);
        }

        private void ResetForm()
        {
            NumberLabel.TextColor = UIColor.White.ColorWithAlpha(.9f);

            NameLabel.TextColor = UIColor.White.ColorWithAlpha(.9f);

            ExpirationDateLabel.TextColor = UIColor.White.ColorWithAlpha(.9f);

            _cardNumberGestureRecognizer = new UITapGestureRecognizer(() =>
            {
                CardNumber = string.Empty;
                NumberLabel.Text = string.Empty;
            });

            _cardNameGestureRecognizer = new UITapGestureRecognizer(() =>
            {
                CardName = string.Empty;
                NameLabel.Text = string.Empty;
            });

            _cardExpirationGestureRecognizer = new UITapGestureRecognizer(() =>
            {
                Expiration = default;
                ExpirationDateLabel.Text = string.Empty;
            });

            NumberLabel.AddGestureRecognizer(_cardNumberGestureRecognizer);

            NameLabel.AddGestureRecognizer(_cardNameGestureRecognizer);

            ExpirationDateLabel.AddGestureRecognizer(_cardExpirationGestureRecognizer);

            NumberLabel.Text = string.Empty;

            ExpirationDateLabel.Text = string.Empty;

            NameLabel.Text = string.Empty;

            CardNumber = string.Empty;

            CardName = string.Empty;

            Expiration = default;
        }

        private void SetupCaptureSession()
        {
            AddCameraInput();
            AddPreviewLayer();
            AddVideoOutput();

            RunSession = true;
        }

        private void AddCameraInput()
        {
            _camera = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);

            var input = new AVCaptureDeviceInput(_camera, out NSError error);

            FlashlightButton.Hidden = !_camera.HasTorch;

            if (_captureSession.CanAddInput(input))
            {
                _captureSession.AddInput(input);

                if (_captureSession.CanSetSessionPreset(AVCaptureSession.Preset3840x2160))
                {
                    _captureSession.SessionPreset = AVCaptureSession.Preset3840x2160;
                }
                else if (_captureSession.CanSetSessionPreset(AVCaptureSession.Preset1920x1080))
                {
                    _captureSession.SessionPreset = AVCaptureSession.Preset1920x1080;
                }
            }
        }

        private void AddPreviewLayer()
        {
            CameraView.Layer.AddSublayer(_previewLayer);
        }

        private void AddVideoOutput()
        {
            var settings = new AVVideoSettingsUncompressed()
            {
                PixelFormatType = CoreVideo.CVPixelFormatType.CV32BGRA
            };

            var videoOutput = new AVCaptureVideoDataOutput()
            {
                WeakVideoSettings = settings.Dictionary,
                AlwaysDiscardsLateVideoFrames = true
            };

            var videoCaptureQueue = new DispatchQueue(CaptureQueueLabel);

            videoOutput.SetSampleBufferDelegateQueue(this, videoCaptureQueue);

            if (_captureSession.CanAddOutput(videoOutput))
            {
                _captureSession.AddOutput(videoOutput);
            }

            _captureSession.CommitConfiguration();

            SetCameraOrientation();
        }

        private void SetCameraOrientation()
        {
            var ornt = UIDevice.CurrentDevice.Orientation;

            if (_previewLayer.Connection != null)
            {
                _previewLayer.Connection.VideoOrientation = UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight ? AVCaptureVideoOrientation.LandscapeRight : AVCaptureVideoOrientation.LandscapeLeft;
            }
        }

        private void CheckPermissions()
        {
            var permissions = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);

            if (permissions == AVAuthorizationStatus.Authorized)
            {
                SetupCaptureSession();
            }
            else if (permissions == AVAuthorizationStatus.Denied)
            {
                InvokeOnMainThread(AlertCameraAccessNeeded);
            }
            else
            {
                AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, (granted) =>
                {
                    InvokeOnMainThread(() =>
                    {
                        if (granted)
                        {
                            SetupCaptureSession();
                        }
                        else
                        {
                            OnCloseTap();
                        }
                    });
                });
            }
        }

        private void AlertCameraAccessNeeded()
        {
            var alertView = new AlertView()
            {
                Title = needAccessTitle,
                Message = cameraAccessMessage,
                OnDismiss = () => DismissViewController(true, default)
            };

            alertView.PopoverPresentationController.SourceView = View;
            alertView.PopoverPresentationController.SourceRect = new CGRect(View.Bounds.GetMidX(), View.Bounds.GetMidY(), 0, 0);
            alertView.PopoverPresentationController.PermittedArrowDirections = new UIPopoverArrowDirection();

            alertView.AddAction(UIAlertAction.Create(allowCameraTitle, UIAlertActionStyle.Default, (_) =>
            {
                alertView.DismissModalViewController(true);
                UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString));
            }));
            alertView.AddAction(UIAlertAction.Create(cancelTitle, UIAlertActionStyle.Destructive, (_) => alertView.DismissModalViewController(true)));

            PresentModalViewController(alertView, true);
        }

        private void OutputRecorder(CMSampleBuffer sampleBuffer)
        {
            using (sampleBuffer)
            using (var imageBuffer = sampleBuffer.GetImageBuffer())
            using (var ciImage = new CIImage(imageBuffer))
            using (var resizeFilter = GetImageFilter(ciImage))
            using (var outputImage = resizeFilter.OutputImage)
            using (var croppedImage = outputImage.ImageByCroppingToRect(_cardRect))
            using (var imageRequestHandler = new VNImageRequestHandler(croppedImage, options: new NSDictionary()))
            using (var textRequest = GetTextRequest())
            {
                imageRequestHandler.Perform(new VNRequest[] { textRequest }, out NSError error);
            }
        }

        private VNRecognizeTextRequest GetTextRequest()
        {
            var completionHandler = new VNRequestCompletionHandler((request, error) =>
            {
                var texts = request.GetResults<VNRecognizedTextObservation>()?.SelectMany(x => x.TopCandidates(MaxCandidates))?.Where(x => x != null && !string.IsNullOrEmpty(x.String))?.Select(x => x.String);

                if (texts != null && texts.Any())
                {
                    ExtractPaymentCardData(texts);
                }
            });

            return new VNRecognizeTextRequest(completionHandler)
            {
                RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
                UsesLanguageCorrection = true
            };
        }

        private void ExtractPaymentCardData(IEnumerable<string> candidates)
        {
            if (_queueCount <= MaxQueueCount)
            {
                AddQueue(candidates);
            }
            else
            {
                SetCardNumber();
                SetCardName();
                SetExpiration();

                ResetQueue();
            }
        }

        private void AddQueue(IEnumerable<string> candidates, bool update = true)
        {
            _candidates[CardValueType.Number].AddRange(candidates.Where(x => x.IsOnlyNumbers() && _cardNumberSplitParts.Any(part => part == x.Split().Length) && GetLength(x) >= CardMinLength && GetLength(x) <= CardMaxLength));
            _candidates[CardValueType.Name].AddRange(candidates.Where(x => x.IsisOnlyAlpha() && x.IsAllUpper() && _cardNameSplitParts.Any(part => part == x.Split().Length) && GetLength(x) >= NameMinLength && GetLength(x) <= NameMaxLength));
            _candidates[CardValueType.Expiration].AddRange(candidates.Where(x => x.IsExpirationDate()));

            if (update)
            {
                _queueCount++;
            }

        }

        private void ResetQueue()
        {

            _candidates[CardValueType.Number].Clear();
            _candidates[CardValueType.Name].Clear();
            _candidates[CardValueType.Expiration].Clear();

            _queueCount = 0;
        }

        private void SetCardNumber()
        {
            var candidates = _candidates[CardValueType.Number];

            if (candidates.Any())
            {
                var avarageLength = candidates.Average(x => x.Length);

                var cardNumber = candidates.FirstOrDefault(x => x.Length >= avarageLength);

                if (cardNumber != null && cardNumber != CardNumber)
                {
                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        NumberLabel.Text = CardNumber = cardNumber;
                    });
                }
            }
        }

        private void SetCardName()
        {
            var candidates = _candidates[CardValueType.Name];

            if (candidates.Any())
            {
                var avarageLength = candidates.Average(x => x.Length);

                var cardName = candidates.FirstOrDefault(x => x.Length >= avarageLength);

                if (cardName != null && cardName != CardName)
                {
                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        NameLabel.Text = CardName = cardName;
                    });
                }
            }
        }

        private void SetExpiration()
        {
            var candidates = _candidates[CardValueType.Expiration].Select(x => x.ExpirationDate().ToFileTimeUtc());

            if (candidates != null && candidates.Any())
            {
                var avarage = candidates.Average();

                var expDate = new DateTime(candidates.FirstOrDefault(x => x >= avarage));

                if (expDate != default && DateTime.Compare(Expiration, expDate) != 0)
                {
                    Expiration = expDate;

                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        ExpirationDateLabel.Text = Expiration.ToString("MM / yy");
                    });
                }
            }
        }

        private void TapticFeedback() => new UINotificationFeedbackGenerator().NotificationOccurred(UINotificationFeedbackType.Success);

        private int GetLength(string input) => input.Trim().Replace(" ", string.Empty).Length;

        private CGRect GetCardLocation(CGRect frame)
        {
            var scaleX = _cardRect.Width + (_cardRect.Width * (_previewLayer.Frame.Width / frame.Width));
            var scaleY = _cardRect.Height + (_cardRect.Height * (_previewLayer.Frame.Height / frame.Height));
            var cardLocation = new CGRect(_cardRect.X, _cardRect.Y, _cardRect.Width, _cardRect.Height).Inset(-scaleX, -scaleY);
            cardLocation.X = (frame.Width / 2) - (cardLocation.Width / 2);
            cardLocation.Y = (frame.Height / 2) - (cardLocation.Height / 2);

            return cardLocation;
        }

        private CIFilter GetImageFilter(CIImage cIImage)
        {
            var resizeFilter = CIFilter.FromName(name: "CILanczosScaleTransform");

            var scale = _previewLayer.Frame.Height / cIImage.Extent.Height;
            var aspectRatio = _previewLayer.Frame.Width / (cIImage.Extent.Width * scale);

            resizeFilter.SetValueForKey(NSObject.FromObject(cIImage), key: CIFilterInputKey.Image);
            resizeFilter.SetValueForKey(NSObject.FromObject(scale), key: CIFilterInputKey.Scale);
            resizeFilter.SetValueForKey(NSObject.FromObject(aspectRatio), key: CIFilterInputKey.AspectRatio);

            return resizeFilter;
        }

        private void Unload()
        {
            NumberLabel.RemoveGestureRecognizer(_cardNumberGestureRecognizer);

            NameLabel.RemoveGestureRecognizer(_cardNameGestureRecognizer);

            ExpirationDateLabel.RemoveGestureRecognizer(_cardExpirationGestureRecognizer);
        }
    }
}


