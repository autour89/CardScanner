using System;
using Foundation;
using UIKit;
using Vision;
using VisionKit;
using CoreGraphics;
using System.Linq;
using CoreImage;

namespace CardScanner.Views
{

public partial class NewCardScanner : UIViewController, IVNDocumentCameraViewControllerDelegate
    {
        private VNDocumentCameraViewController cameraViewController;
        private VNRecognizeTextRequest textRecognitionRequest;

        public NewCardScanner(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            cameraViewController = new VNDocumentCameraViewController();
            cameraViewController.Delegate = this;
            textRecognitionRequest = new VNRecognizeTextRequest(OnTextRecognitionCompleted);
            textRecognitionRequest.RecognitionLevel = VNRequestTextRecognitionLevel.Accurate;
            textRecognitionRequest.UsesLanguageCorrection = true;
        }

        //partial void ScanButton_TouchUpInside(UIButton sender)
        //{
        //    PresentViewController(cameraViewController, true, null);
        //}

        [Export("documentCameraViewController:didFinishWithScan:")]
        public void DidFinish(VNDocumentCameraViewController controller, VNDocumentCameraScan scan)
        {
            for (nuint i = 0; i < scan.PageCount; i++)
            {
                using (var scannedImage = scan.GetImage(i))
                using (var ciImage = new CIImage(scannedImage))
                using (var imageRequestHandler = new VNImageRequestHandler(ciImage, options: new NSDictionary()))
                using (var textRequest = GetTextRequest())
                {
                    imageRequestHandler.Perform(new VNRequest[] { textRecognitionRequest }, out NSError error);
                }
            }

        }

        private VNRecognizeTextRequest GetTextRequest()
        {
            var completionHandler = new VNRequestCompletionHandler((request, error) =>
            {
                var texts = request.GetResults<VNRecognizedTextObservation>()?.SelectMany(x => x.TopCandidates(100))?.Where(x => x != null && !string.IsNullOrEmpty(x.String))?.Select(x => x.String);

                if (texts != null && texts.Any())
                {

                }
            });

            return new VNRecognizeTextRequest(completionHandler)
            {
                RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
                UsesLanguageCorrection = true
            };
        }

        private void OnTextRecognitionCompleted(VNRequest request, NSError error)
        {
            if (error != null)
            {
                Console.WriteLine($"Error in text recognition: {error.LocalizedDescription}");
                return;
            }

            var observations = request.GetResults<VNRecognizedTextObservation>();
            foreach (var observation in observations)
            {
                var text = observation.TopCandidates(1).FirstOrDefault()?.String;
                if (!string.IsNullOrEmpty(text))
                {
                    if (IsCreditCardNumber(text))
                    {
                        // Credit card number detected
                        Console.WriteLine($"Credit card number detected: {text}");
                    }
                }
            }
        }

        private bool IsCreditCardNumber(string text)
        {
            // TODO: Implement credit card number validation
            // You can use regular expressions to check if the string matches a valid credit card number pattern
            return false;
        }
    }

}

