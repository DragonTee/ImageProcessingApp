
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ImageProcessingApp.Mobile.Infrastructure.DataAccess;
using ImageProcessingApp.Mobile.Services;
using ImageProcessingApp.Core.Interfaces.DataAccess;

[assembly: ExportFont("Quicksand-Light.ttf", Alias = "QuicksandLight")]
[assembly: ExportFont("Quicksand-Medium.ttf", Alias = "QuicksandMedium")]
[assembly: ExportFont("Quicksand-Regular.ttf", Alias = "Quicksand")]

namespace ImageProcessingApp.Mobile
{
    public partial class App : Application
    {
        public static double ScreenHeight;
        public static double ScreenWidth;
        public static string LocalImagesDirectory;

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            DependencyService.Register<IImageRepository, ImageRepository>();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
