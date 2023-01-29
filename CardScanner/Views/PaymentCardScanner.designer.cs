// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace CardScanner.Views
{
	[Register ("PaymentCardScanner")]
	partial class PaymentCardScanner
	{
		[Outlet]
		UIKit.UIView CameraView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint CardHeightConstraint { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint CardWidthConstraint { get; set; }

		[Outlet]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		UIKit.UIButton ConfirmButton { get; set; }

		[Outlet]
		UIKit.UILabel ExpirationDateLabel { get; set; }

		[Outlet]
		UIKit.UIButton FlashlightButton { get; set; }

		[Outlet]
		CardScanner.Views.Controls.PartialBlurredView GuideView { get; set; }

		[Outlet]
		UIKit.UILabel NameLabel { get; set; }

		[Outlet]
		UIKit.UILabel NumberLabel { get; set; }

		[Action ("OnCloseTap")]
		partial void OnCloseTap ();

		[Action ("OnConfirnTap")]
		partial void OnConfirnTap ();

		[Action ("OnFlashlightTap")]
		partial void OnFlashlightTap ();
		
		void ReleaseDesignerOutlets ()
		{
			if (CameraView != null) {
				CameraView.Dispose ();
				CameraView = null;
			}

			if (CardHeightConstraint != null) {
				CardHeightConstraint.Dispose ();
				CardHeightConstraint = null;
			}

			if (CardWidthConstraint != null) {
				CardWidthConstraint.Dispose ();
				CardWidthConstraint = null;
			}

			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (ConfirmButton != null) {
				ConfirmButton.Dispose ();
				ConfirmButton = null;
			}

			if (ExpirationDateLabel != null) {
				ExpirationDateLabel.Dispose ();
				ExpirationDateLabel = null;
			}

			if (FlashlightButton != null) {
				FlashlightButton.Dispose ();
				FlashlightButton = null;
			}

			if (GuideView != null) {
				GuideView.Dispose ();
				GuideView = null;
			}

			if (NameLabel != null) {
				NameLabel.Dispose ();
				NameLabel = null;
			}

			if (NumberLabel != null) {
				NumberLabel.Dispose ();
				NumberLabel = null;
			}
		}
	}
}
