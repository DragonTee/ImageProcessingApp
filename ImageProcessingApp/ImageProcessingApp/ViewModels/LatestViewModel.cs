using ImageProcessingApp.Mobile.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace ImageProcessingApp.Mobile.ViewModels
{
    public class LatestViewModel : BaseViewModel
    {
        public LatestViewModel()
        {
            EditCommand = new Command(async () =>
            {
                await Application.Current.MainPage.Navigation.PopAsync(false);
            });
        }

        public ICommand EditCommand { get; }
    }
}
