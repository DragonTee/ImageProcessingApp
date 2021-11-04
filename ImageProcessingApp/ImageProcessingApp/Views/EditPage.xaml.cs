using ImageProcessingApp.Mobile.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ImageProcessingApp.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditPage : ContentPage
    {
        public EditPage()
        {
            InitializeComponent();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            Console.WriteLine("BindingContextChanged");
        }

        private void OnCanvasViewPaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs args)
        {            
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            EditViewModel model = (EditViewModel)this.BindingContext;
            
            args.Surface.Canvas.Clear();
            if (model.BitmapProcessed != null)
            {
                var bitmap = model.BitmapProcessed;
                var bitmapInfo = bitmap.Info;
                float c = bitmapInfo.Height * 1.0f / bitmapInfo.Width;
                var newHeight = (int)Math.Round(info.Width * c);

                if (newHeight > info.Height)
                {
                    info.Width = (int)Math.Round(info.Height / c);
                }
                else
                {
                    info.Height = newHeight;
                }

                canvas.Clear();
                canvas.DrawBitmap(model.BitmapProcessed, info.Rect);
            }
            Console.WriteLine("Validating canvas");
        }

        private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var model = BindingContext;
        }
    }
}