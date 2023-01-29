using System;
using UIKit;

namespace CardScanner.Views.Controls
{
    public class AlertView : UIAlertController
    {
        public Action OnDismiss { get; set; }

        public override UIAlertControllerStyle PreferredStyle => UIAlertControllerStyle.Alert;

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            OnDismiss?.Invoke();
        }
    }
}

