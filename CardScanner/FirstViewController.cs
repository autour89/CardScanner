using System;
using CoreImage;
using UIKit;
using CardScanner.Views;
using AVFoundation;

namespace CardScanner
{
    public partial class FirstViewController : UIViewController
    {
        public FirstViewController(IntPtr handle) : base(handle) { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialise();
        }

        private void Initialise()
        {

        }

        partial void OnScan(Foundation.NSObject sender)
        {
            //PresentModalViewController(PaymentCardScanner.GetScanner(), true);
            PresentModalViewController(new PaymentCardScanner() { ModalPresentationStyle = UIModalPresentationStyle.FullScreen }, true);
        }

    }
}
