using ImageProcessingApp.Mobile.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace ImageProcessingApp.Mobile.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}