using ImageProcessingApp.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace ImageProcessingApp.Views
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