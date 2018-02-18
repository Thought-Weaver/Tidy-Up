using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using BindingLibrary;

namespace TidyUp
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;
		MainView viewController;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			WritePadAPI.recoInit ();
            WritePadAPI.initializeFlags();

			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			viewController = new MainView ();
			window.RootViewController = viewController;
			window.MakeKeyAndVisible ();
			
			return true;
		}

		public override void WillTerminate(UIApplication app)
		{
			WritePadAPI.recoFree ();
		}
	}
}

