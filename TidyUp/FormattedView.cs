using System;
using CoreGraphics;
using Foundation;
using UIKit;
using System.Collections.Generic;
using BindingLibrary;

namespace TidyUp
{
	public partial class FormattedView : UIViewController
	{
		UILabel mainText = new UILabel ();

		public FormattedView (String text)
		{
			mainText.Text = text;
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

		void OnDone (object sender, EventArgs args)
		{
			base.NavigationController.DismissViewController (true, null);
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			base.NavigationItem.Title = "Formatted Text";

			mainText.Frame = new CGRect (0, 0, View.Frame.Width, View.Frame.Height);
			mainText.TextColor = UIColor.Black;
			mainText.Font = UIFont.FromName ("Latin Modern", 12f);
			mainText.AdjustsFontSizeToFitWidth = true;
			mainText.MinimumFontSize = 12f;
			mainText.LineBreakMode = UILineBreakMode.TailTruncation;
			mainText.Lines = 0;

			View.Add (mainText);
		}
	}
}
