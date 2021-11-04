using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Foundation;
using ImageProcessingApp.Mobile.Configurations;
using UIKit;

namespace ImageProcessingApp.Mobile.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            App.ScreenWidth = UIScreen.MainScreen.Bounds.Width;
            App.ScreenHeight = UIScreen.MainScreen.Bounds.Height;
            
            App.LocalImagesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", Constants.ImagesFolder);

            DevExpress.XamarinForms.Charts.iOS.Initializer.Init();

            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}
