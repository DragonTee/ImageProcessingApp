using ImageProcessingApp.ViewModels;
using ImageProcessingApp.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace ImageProcessingApp
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            DevExpress.XamarinForms.Charts.Initializer.Init();
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
            Routing.RegisterRoute(nameof(EditPage), typeof(EditPage));
        }

    }
}
