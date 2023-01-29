using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreGraphics;
using UIKit;

namespace CardScanner.ViewModels
{
    public static class Extensions
    {
        public static bool IsNumeric(this string input) => long.TryParse(input, out long _);

        public static bool IsOnlyNumbers(this string input) => Regex.IsMatch(input.Trim().Replace(" ", string.Empty), @"^[0-9]+$");

        public static bool IsAllUpper(this string input) => input.Trim().Replace(" ", string.Empty).All(x => char.IsUpper(x));

        public static bool IsisOnlyAlpha(this string input) => Regex.IsMatch(input.Trim().Replace(" ", string.Empty), @"^[a-zA-Z]+$");

        public static bool IsExpirationDate(this string input)
        {
            return DateTime.TryParseExact(input.Trim().Replace(" ", string.Empty), "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
             DateTime.TryParseExact(input.Trim().Replace(" ", string.Empty), "MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        public static DateTime ExpirationDate(this string input)
        {
            DateTime result;
            if (!DateTime.TryParseExact(input.Trim().Replace(" ", string.Empty), "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                DateTime.TryParseExact(input.Trim().Replace(" ", string.Empty), "MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }

            return result;
        }

        public static void FillSuperview(this UIView view, UIView superView)
        {
            view.TranslatesAutoresizingMaskIntoConstraints = false;

            var constraints = new NSLayoutConstraint[]
            {
                view.LeadingAnchor.ConstraintEqualTo(superView.LeadingAnchor),
                view.TrailingAnchor.ConstraintEqualTo(superView.TrailingAnchor),
                view.TopAnchor.ConstraintEqualTo(superView.TopAnchor),
                view.BottomAnchor.ConstraintEqualTo(superView.BottomAnchor)
            };
            superView.AddSubview(view);
            superView.AddConstraints(constraints);
            NSLayoutConstraint.ActivateConstraints(constraints);
        }

        public static bool IsTouchInView(this UIView view, CGPoint touch) => touch.X >= 0 && touch.Y >= 0 && touch.X <= view.Frame.Width && touch.Y <= view.Frame.Height;
    }
}

