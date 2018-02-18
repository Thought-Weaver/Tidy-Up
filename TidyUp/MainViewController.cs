/*
 * 
 * Code based upon the example provided by the WritePadSDK for Xamarin iOS. Rights
 * for the tutorial code are given to WritePad. All other code was written by Logan
 * Apple and Anirudh Rangaswamy for Treehacks 2018.
 * 
 */

using System;
using System.Net.Http;
using System.Linq;
using CoreGraphics;
using Foundation;
using UIKit;
using System.Collections.Generic;
using BindingLibrary;

namespace TidyUp
{
	public class PickerModel : UIPickerViewModel
	{
		public IList<Object> values;

		public event EventHandler<PickerChangedEventArgs> PickerChanged;

		public PickerModel (IList<Object> values)
		{
			this.values = values;
		}

		public override nint GetComponentCount (UIPickerView picker)
		{
			return 1;
		}

		public override nint GetRowsInComponent (UIPickerView picker, nint component)
		{
			return values.Count;
		}

		public override string GetTitle (UIPickerView picker, nint row, nint component)
		{
			return values [(int)row].ToString ();
		}

		public override nfloat GetRowHeight (UIPickerView picker, nint component)
		{
			return 40f;
		}

		public override void Selected (UIPickerView picker, nint row, nint component)
		{
			if (this.PickerChanged != null)
			{
				this.PickerChanged (this, new PickerChangedEventArgs { SelectedValue = values [(int)row] });
			}
		}

		public class PickerChangedEventArgs : EventArgs
		{
			public object SelectedValue { get; set; }
		}
	}

	public partial class MainView : UIViewController
	{
		UIButton switchViewButton;
		UIButton clearButton;
		UIButton languageButton;
		UIButton translateButton;
		UIButton rewriteButton;
		UIButton printButton;
		InkView inkView;
		UITextView formattedText;

		private const float button_width = 100;
		private const float button_gap = 2;
		private const float button_height = 36;
		private const float bottom_gap = 40;
		private const float header_height = 6;
		private const float button_font_size = 14;

		private bool displayingFormatted = false;

		private String recognized_text = "";
		private String translated_text = "";
		private String from_language = "en";
		private String to_language = "en";

		static bool UserInterfaceIdiomIsPhone
		{
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public MainView ()
		{
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			UIInterfaceOrientation orientation = UIApplication.SharedApplication.StatusBarOrientation;
			WillRotate (orientation, 0.0);
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			UIInterfaceOrientation orientation = UIApplication.SharedApplication.StatusBarOrientation;
			float width = (float)View.Frame.Width;

			float height = (float)View.Frame.Height;
			if (orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight)
			{
				width = (float)View.Frame.Height;
				height = (float)View.Frame.Width;
			}

			WritePadAPI.setRecoFlag (WritePadAPI.recoGetFlags (), true, WritePadAPI.FLAG_USERDICT);
			WritePadAPI.setRecoFlag (WritePadAPI.recoGetFlags (), true, WritePadAPI.FLAG_ANALYZER);
			WritePadAPI.setRecoFlag (WritePadAPI.recoGetFlags (), true, WritePadAPI.FLAG_CORRECTOR);

			inkView = new InkView ();
			inkView.Frame = new CGRect (button_gap, header_height + button_gap, width - button_gap * 2, height);
			inkView.ContentMode = UIViewContentMode.Redraw;
			inkView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			View.Add (inkView);

			formattedText = new UITextView ();
			formattedText.Frame = new CGRect (0, UIScreen.MainScreen.Bounds.Height * 0.02, UIScreen.MainScreen.Bounds.Width, UIScreen.MainScreen.Bounds.Height);
			formattedText.TextColor = UIColor.Black;
			formattedText.Font = UIFont.FromName ("Baskerville", 28f);
			formattedText.Hidden = true;
			View.Add (formattedText);

			inkView.OnReturnGesture += () => recognized_text = inkView.Recognize (true);
			inkView.OnReturnGesture += () => inkView.cleanView (true);
			inkView.OnCutGesture += () => inkView.cleanView (true);
			inkView.OnUndoGesture += () => inkView.cleanView (true);

			float x = (width - (button_width * 5 + button_gap * 5)) / 2;
			switchViewButton = new UIButton (UIButtonType.Custom);
			switchViewButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			switchViewButton.SetTitle ("Switch View", UIControlState.Normal);
			switchViewButton.Font = UIFont.SystemFontOfSize (button_font_size);
			switchViewButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			switchViewButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			switchViewButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			switchViewButton.TouchUpInside += (object sender, EventArgs e) => {
				if (inkView.getNumPaths () > 0)
					recognized_text = inkView.Recognize (false);
				if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
				{
					formattedText.Text = recognized_text;
					formattedText.Text = FormatLatex (recognized_text);
					formattedText.Hidden = false;
					inkView.Hidden = true;
					displayingFormatted = true;
					printButton.SetTitle ("Print", UIControlState.Normal);
				}
				else if (displayingFormatted)
				{
					formattedText.Hidden = true;
					inkView.Hidden = false;
					displayingFormatted = false;
					printButton.SetTitle ("Save", UIControlState.Normal);
				}
			};
			View.Add (switchViewButton);
			x += (button_gap + button_width);
			clearButton = new UIButton (UIButtonType.Custom);
			clearButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			clearButton.SetTitle ("Clear", UIControlState.Normal);
			clearButton.Font = UIFont.SystemFontOfSize (button_font_size);
			clearButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			clearButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			clearButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			clearButton.TouchUpInside += (object sender, EventArgs e) => {
				inkView.cleanView (true);
			};
			View.Add (clearButton);

			x += (button_gap + button_width);
			languageButton = new UIButton (UIButtonType.Custom);
			languageButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			languageButton.SetTitle ("Language", UIControlState.Normal);
			languageButton.Font = UIFont.SystemFontOfSize (button_font_size);
			languageButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			languageButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			languageButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			languageButton.TouchUpInside += (object sender, EventArgs e) => {
				var actionSheet = new UIActionSheet ("Select Language:", (IUIActionSheetDelegate)null, "Cancel", null,
					new [] { "English", "English UK", "German", "French", "Spanish", "Portuguese", "Brazilian", "Dutch", "Italian", "Finnish", "Swedish", "Norwegian", "Danish", "Indonesian" });
				actionSheet.Clicked += delegate (object a, UIButtonEventArgs b) {
					switch (b.ButtonIndex)
					{
						case 0:
							WritePadAPI.language = WritePadAPI.LanguageType.en;
							from_language = "en";
							break;
						case 1:
							WritePadAPI.language = WritePadAPI.LanguageType.en_uk;
							from_language = "en";
							break;
						case 2:
							WritePadAPI.language = WritePadAPI.LanguageType.de;
							from_language = "de";
							break;
						case 3:
							WritePadAPI.language = WritePadAPI.LanguageType.fr;
							from_language = "fr";
							break;
						case 4:
							WritePadAPI.language = WritePadAPI.LanguageType.es;
							from_language = "es";
							break;
						case 5:
							WritePadAPI.language = WritePadAPI.LanguageType.pt_PT;
							from_language = "pt";
							break;
						case 6:
							WritePadAPI.language = WritePadAPI.LanguageType.pt_BR;
							from_language = "pt";
							break;
						case 7:
							WritePadAPI.language = WritePadAPI.LanguageType.nl;
							from_language = "nl";
							break;
						case 8:
							WritePadAPI.language = WritePadAPI.LanguageType.it;
							from_language = "it";
							break;
						case 9:
							WritePadAPI.language = WritePadAPI.LanguageType.fi;
							from_language = "fi";
							break;
						case 10:
							WritePadAPI.language = WritePadAPI.LanguageType.sv;
							from_language = "sv";
							break;
						case 11:
							WritePadAPI.language = WritePadAPI.LanguageType.nb;
							from_language = "nb";
							break;
						case 12:
							WritePadAPI.language = WritePadAPI.LanguageType.da;
							from_language = "da";
							break;
						case 13:
							WritePadAPI.language = WritePadAPI.LanguageType.id;
							from_language = "id";
							break;
					}
					WritePadAPI.recoFree ();
					WritePadAPI.recoInit ();
					WritePadAPI.initializeFlags ();
				};
				actionSheet.ShowInView (View);
			};
			View.Add (languageButton);

			x += (button_gap + button_width);
			translateButton = new UIButton (UIButtonType.Custom);
			translateButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			translateButton.SetTitle ("Translate", UIControlState.Normal);
			translateButton.Font = UIFont.SystemFontOfSize (button_font_size);
			translateButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			translateButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			translateButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			translateButton.TouchUpInside += (object sender, EventArgs e) => {
				var translateActionSheet = new UIActionSheet ("Select Language:", (IUIActionSheetDelegate)null, "Cancel", null,
					new [] { "English", "English UK", "German", "French", "Spanish", "Portuguese", "Brazilian", "Dutch", "Italian", "Finnish", "Swedish", "Norwegian", "Danish", "Indonesian" });
				translateActionSheet.Clicked += delegate (object a, UIButtonEventArgs b) {
					switch (b.ButtonIndex)
					{
						case 0:
							to_language = "en";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 1:
							to_language = "en";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 2:
							to_language = "de";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 3:
							to_language = "fr";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 4:
							to_language = "es";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 5:
							to_language = "pt";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 6:
							to_language = "pt";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 7:
							to_language = "nl";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 8:
							to_language = "it";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 9:
							to_language = "fi";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 10:
							to_language = "sv";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 11:
							to_language = "nb";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 12:
							to_language = "da";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
						case 13:
							to_language = "id";
							recognized_text = inkView.Recognize (false);
							if (!displayingFormatted && recognized_text != "" && recognized_text != "*Error*")
							{
								Translate (FormatLatex (recognized_text));
							}
							break;
					}
					WritePadAPI.recoFree ();
					WritePadAPI.recoInit ();
					WritePadAPI.initializeFlags ();
				};
				translateActionSheet.ShowInView (View);
			};
			View.Add (translateButton);

			x += (button_gap + button_width);
			printButton = new UIButton (UIButtonType.Custom);
			printButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			printButton.SetTitle ("Save", UIControlState.Normal);
			printButton.Font = UIFont.SystemFontOfSize (button_font_size);
			printButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			printButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			printButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			printButton.TouchUpInside += (object sender, EventArgs e) => {
				if (displayingFormatted && formattedText.Text != "")
				{
					var printInfo = UIPrintInfo.PrintInfo;

					printInfo.OutputType = UIPrintInfoOutputType.General;
					printInfo.JobName = "Printing Tidied-Up Text";

					UIPrintInteractionController printer = UIPrintInteractionController.SharedPrintController;

					printer.PrintInfo = printInfo;
					printer.PrintingItem = new NSString(formattedText.Text);
					printer.ShowsPageRange = true;

					printer.Present (true, PrintingCompleted);
				}
				else if (!displayingFormatted)
				{
					switchViewButton.Hidden = true;
					clearButton.Hidden = true;
					languageButton.Hidden = true;
					translateButton.Hidden = true;
					printButton.Hidden = true;
					UIImage data = UIScreen.MainScreen.Capture ();
					data.SaveToPhotosAlbum (null);
					switchViewButton.Hidden = false;
					clearButton.Hidden = false;
					languageButton.Hidden = false;
					translateButton.Hidden = false;
					printButton.Hidden = false;
				}
			};
			View.Add (printButton);

			/*x += (button_gap + button_width);
			rewriteButton = new UIButton (UIButtonType.Custom);
			rewriteButton.Frame = new CGRect (x, height - bottom_gap, button_width, button_height);
			rewriteButton.SetTitle ("Rewrite", UIControlState.Normal);
			rewriteButton.Font = UIFont.SystemFontOfSize (button_font_size);
			rewriteButton.SetTitleColor (UIColor.Blue, UIControlState.Normal);
			rewriteButton.SetTitleColor (UIColor.White, UIControlState.Highlighted);
			rewriteButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;
			rewriteButton.TouchUpInside += (object sender, EventArgs e) => {
				inkView.cleanView (true);
				if (translated_text != "")
				{
					formattedText.Hidden = true;
					inkView.Hidden = false;
					displayingFormatted = false;
					inkView.writeText (translated_text, (int)(width - button_gap * 2), (int)height);
				}
			};
			View.Add (rewriteButton);*/
		}

		async public void Translate(String input)
		{
			if (input != "")
			{
				string host = "https://api.microsofttranslator.com";
				string path = "/V2/Http.svc/Translate";
				string key = "abc8244eef3c480786165607b30b5ee4";

				HttpClient client = new HttpClient ();
				client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", key);

				string uri = host + path + "?from=" + from_language + "&to=" + to_language + "&text=" + System.Net.WebUtility.UrlEncode (input);

				translated_text = "Translating...";

				HttpResponseMessage response = await client.GetAsync (uri);

				translated_text = await response.Content.ReadAsStringAsync ();
				translated_text = translated_text.Replace ("<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">", "");
				translated_text = translated_text.Replace ("</string>", "");

				formattedText.Text = translated_text;
				formattedText.Hidden = false;
				inkView.Hidden = true;
				displayingFormatted = true;
			}
		}

		public String FormatLatex (String input)
		{
			List<String> words = input.Split (' ').ToList ();
			for (int i = 0; i < words.Count; i++)
			{
				if (words [i] == "$alpha")
					words [i] = "α";
				if (words [i] == "$Alpha")
					words [i] = "Α";
				if (words [i] == "$beta")
					words [i] = "β";
				if (words [i] == "$Beta")
					words [i] = "Β";
				if (words [i] == "$gamma")
					words [i] = "γ";
				if (words [i] == "$Gamma")
					words [i] = "Γ";
				if (words [i] == "$delta")
					words [i] = "δ";
				if (words [i] == "$Delta")
					words [i] = "Δ";
				if (words [i] == "$epsilon")
					words [i] = "ε";
				if (words [i] == "$Epsilon")
					words [i] = "Ε";
				if (words [i] == "$theta")
					words [i] = "θ";
				if (words [i] == "$Thetha")
					words [i] = "Θ";
				if (words [i] == "$lambda")
					words [i] = "λ";
				if (words [i] == "$Lambda")
					words [i] = "Λ";
				if (words [i] == "$mu")
					words [i] = "μ";
				if (words [i] == "$Mu")
					words [i] = "Μ";
				if (words [i] == "$pi")
					words [i] = "π";
				if (words [i] == "$Pi")
					words [i] = "Π";
				if (words [i] == "$rho")
					words [i] = "ρ";
				if (words [i] == "$Rho")
					words [i] = "Ρ";
				if (words [i] == "$sigma")
					words [i] = "σ";
				if (words [i] == "$Sigma")
					words [i] = "Σ";
				if (words [i] == "$phi")
					words [i] = "φ";
				if (words [i] == "$Phi")
					words [i] = "Φ";
				if (words [i] == "$psi")
					words [i] = "ψ";
				if (words [i] == "$Psi")
					words [i] = "Ψ";
				if (words [i] == "$omega")
					words [i] = "ω";
				if (words [i] == "$Omega")
					words [i] = "Ω";
				if (words [i] == "$!=")
					words [i] = "≠";
				if (words [i] == "$greater" || words [i] == "$Greater")
				    words [i] = ">";
				if (words [i] == "$less" || words [i] == "$Less")
					words [i] = "<";
				if (words [i] == "$greater-equal" || words [i] == "$Greater-equal" || (words [i] == "$Greater-Equal" || words [i] == "$greater-Equal"))
				    words [i] = "≥";
				if (words [i] == "$less-equal" || words [i] == "$Less-equal" || (words [i] == "$Less-Equal" || words [i] == "$less-Equal"))
				    words [i] = "≤";
				if (words [i] == "$plus-minus")
					words [i] = "±";
				if (words [i] == "$minus-plus")
					words [i] = "∓";
				if (words [i] == "$forward-slash")
					words [i] = "/";
				if (words [i] == "$back-slash")
					words [i] = "\\";
				if (words [i] == "$divide")
					words [i] = "÷";
				if (words [i] == "$percent")
					words [i] = "%";
				if (words [i] == "$degree")
					words [i] = "º";
				if (words [i] == "$triangle")
					words [i] = "Δ";
				if (words [i] == "$integral" || words [i] == "$Integral")
					words [i] = "∫";
				if ((words [i] == "$integral" || words [i] == "$Integral") && (words [i + 1] == "from" || words [i + 1] == "From"))
				{
					String s = "";
					s += words [i + 2];
					s += "∫";
					s += words [i + 4];
					words [i] = s;
					words.RemoveAt (i + 1);
					words.RemoveAt (i + 2);
					words.RemoveAt (i + 3);
					words.RemoveAt (i + 4);
				}
			}
			return string.Join (" ", words);
		}

		void PrintingCompleted (UIPrintInteractionController controller, bool completed, NSError error)
		{

		}
	}
}

