using System;
using System.ComponentModel;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CardScanner.Views.Controls
{
    [Register(nameof(PartialBlurredView)), DesignTimeVisible(true)]
    public partial class PartialBlurredView : UIView
    {
        private const int CornerRadius = 20;
        private const int BorderWidth = 2;

        public UIView Card => CardView;

        public PartialBlurredView(IntPtr handle) : base(handle)
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(.6f);
            
            Opaque = false;
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            BackgroundColor.SetFill();

            UIKit.UIGraphics.RectFill(rect);

            var path = UIBezierPath.FromRoundedRect(CardView.Frame, CornerRadius);

            rect.Intersect(CardView.Frame);

            UIKit.UIGraphics.RectFill(CardView.Frame);

            UIColor.Clear.SetFill();

            UIGraphics.GetCurrentContext().SetBlendMode(CGBlendMode.Copy);

            path.Fill();
        }

        public void AddBorder()
        {
            Card.Layer.BorderColor = UIColor.White.CGColor;
            Card.Layer.BorderWidth = BorderWidth;
            Card.Layer.CornerRadius = CornerRadius;
        }
    }
}


