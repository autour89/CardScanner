using System;
using System.ComponentModel;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CardScanner.Views.Controls
{
    [Register(nameof(PartialTransparentView)), DesignTimeVisible(true)]
    public class PartialTransparentView : UIView
    {
        private CGRect _rect;

        public PartialTransparentView()
        {
        }

        public PartialTransparentView(CGRect cGRect) : base(cGRect)
        {
            _rect = cGRect;

            BackgroundColor = UIColor.Black.ColorWithAlpha(.6f);

            Opaque = false;
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            BackgroundColor.SetFill();

            UIKit.UIGraphics.RectFill(rect);

            var path = UIBezierPath.FromRoundedRect(_rect, 10);

            rect.Intersect(_rect);

            UIKit.UIGraphics.RectFill(_rect);

            UIColor.Clear.SetFill();

            UIGraphics.GetCurrentContext().SetBlendMode(CGBlendMode.Copy);

            path.Fill();
        }

    }

}


