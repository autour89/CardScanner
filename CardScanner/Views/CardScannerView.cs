using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Accelerate;
using AVFoundation;
using CardScanner.Views.Controls;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;
using Vision;
using VisionKit;
using Xamarin.Essentials;
using static Xamarin.Essentials.Permissions;

namespace CardScanner.Views
{
    public class CardScannerView : UIViewController, IAVCaptureVideoDataOutputSampleBufferDelegate
    {
        private const string ViewTitle = "Card scanner";

        private AVCaptureSession _captureSession;
        private AVCaptureVideoDataOutput _videoOutput;
        private AVCaptureVideoPreviewLayer _previewLayer;

        public Action<string, string, string, DateTime> OnCompleted { get; set; }

        public string CreditCardNumber { get; private set; }

        public string CreditCardName { get; private set; }

        public String CreditCardCVV { get; set; }

        public DateTime CreditCardDate { get; private set; }

        public CardScannerView() { }

        public CardScannerView(IntPtr ptr) : base(ptr) { }

        public static UINavigationController GetScanner()
        {
            return new UINavigationController(new CardScannerView());
        }

        //public override void LoadView()
        //{
        //    base.LoadView();

        //    View = new()
        //    {
        //        TranslatesAutoresizingMaskIntoConstraints = false
        //    };
        //}

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialise();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            _previewLayer.Frame = View.Bounds;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            Run(true);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            Run(false);
        }

        private void Initialise()
        {
            Title = ViewTitle;

            _captureSession = new AVCaptureSession()
            {
                SessionPreset = AVCaptureSession.Preset1920x1080
            };
            _previewLayer = new AVCaptureVideoPreviewLayer(session: _captureSession)
            {
                VideoGravity = AVLayerVideoGravity.ResizeAspect,
                //Orientation = AVCaptureVideoOrientation.LandscapeRight
            };

            _ = SetupCaptureSession();
        }

        private async Task SetupCaptureSession()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status is not PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.Camera>();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddCameraInput();
                AddPreviewLayer();
                AddVideoOutput();
                AddGuideView();
            });
        }

        private void AddCameraInput()
        {
            var input = new AVCaptureDeviceInput(AVCaptureDevice.GetDefaultDevice(AVMediaType.Video), out NSError error);

            _captureSession.AddInput(input);
        }

        private void AddPreviewLayer()
        {
            View.Layer.AddSublayer(_previewLayer);
        }

        private void AddVideoOutput()
        {
            var videoSettingsDict = new NSMutableDictionary();
            videoSettingsDict.Add(CVPixelBuffer.PixelFormatTypeKey, NSNumber.FromUInt32((uint)CVPixelFormatType.CV32BGRA));

            _videoOutput = new AVCaptureVideoDataOutput()
            {
                WeakVideoSettings = videoSettingsDict,
            };

            DispatchQueue dispatchQueue = new DispatchQueue("VideoCaptureQueue");

            _videoOutput.SetSampleBufferDelegateQueue(this, dispatchQueue);

            _captureSession.AddOutput(_videoOutput);

            _captureSession.CommitConfiguration();

            var connection = _videoOutput.ConnectionFromMediaType(AVMediaType.Video);
            connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeRight;
        }

        private void AddGuideView()
        {
            var widht = UIScreen.MainScreen.Bounds.Width - (UIScreen.MainScreen.Bounds.Width * 0.2);
            var height = widht - (widht * 0.45);
            var viewX = (UIScreen.MainScreen.Bounds.Width / 2) - (widht / 2);
            var viewY = (UIScreen.MainScreen.Bounds.Height / 2) - (height / 2) - 100;

            var viewGuide = new PartialTransparentView(new CGRect(width: widht, height: height, x: viewX, y: viewY))
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            View.AddSubview(viewGuide);

            viewGuide.LeftAnchor.ConstraintEqualTo(View.LeftAnchor, 0).Active = true;
            viewGuide.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, 0).Active = true;
            viewGuide.TopAnchor.ConstraintEqualTo(View.TopAnchor, 0).Active = true;
            viewGuide.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, 0).Active = true;

            View.BringSubviewToFront(viewGuide);

            View.BackgroundColor = UIColor.Black;
        }

        private void ScanCompleted()
        {
            OnCompleted?.Invoke(CreditCardNumber, CreditCardName, CreditCardCVV, CreditCardDate);

            DismissViewController(true, () => { });
        }


        private void Run(bool start)
        {
            if (start)
            {
                _captureSession.StartRunning();
            }
            else
            {
                _captureSession.StopRunning();
            }
        }

        private void ExtractPaymentCardData(CMSampleBuffer imageBuffer)
        {
            try
            {
                var imageRequestHandler = new VNImageRequestHandler(GetImageFromSampleBuffer(imageBuffer), options: new NSDictionary());

                var textRequest = new VNRecognizeTextRequest(new VNRequestCompletionHandler((request, error) =>
                {
                    var results = request.GetResults<VNRecognizedTextObservation>();

                    var items = results.SelectMany(x => x.TopCandidates(20)).Select(x => x.String);

                }))
                {
                    RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
                    UsesLanguageCorrection = true
                };

                imageRequestHandler.Perform(new VNRequest[] { textRequest }, out NSError error);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                imageBuffer?.Dispose();
            }
        }


        private CIImage GetImageFromSampleBuffer(CMSampleBuffer imageBuffer)
        {
            using (var pixelBuffer = imageBuffer.GetImageBuffer() as CVPixelBuffer)
            {
                pixelBuffer.Lock((CVPixelBufferLock)0);

                // Prepare to decode buffer
                var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;

                // Decode buffer - Create a new colorspace
                using (var cs = CGColorSpace.CreateDeviceRGB())
                using (var context = new CGBitmapContext(pixelBuffer.BaseAddress, pixelBuffer.Width, pixelBuffer.Height, 8, pixelBuffer.BytesPerRow, cs, (CGImageAlphaInfo)flags))
                using (var cgImage = context.ToImage())
                {

                    pixelBuffer.Unlock((CVPixelBufferLock)0);

                    return cgImage;
                }
            }
        }

        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection) => ExtractPaymentCardData(sampleBuffer);
    }
}

