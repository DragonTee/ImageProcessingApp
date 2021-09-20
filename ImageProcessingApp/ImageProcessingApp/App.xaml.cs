using ImageProcessingApp.Services;
using ImageProcessingApp.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ImageProcessingApp
{
    public partial class App : Application
    {
        public static double ScreenHeight;
        public static double ScreenWidth;
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
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
