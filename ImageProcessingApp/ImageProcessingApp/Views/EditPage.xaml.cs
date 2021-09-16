using ImageProcessingApp.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ImageProcessingApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditPage : ContentPage
    {
        public EditPage()
        {
            InitializeComponent();
        }

        private void OnCanvasViewPaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs args)
        {            
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;            

            EditViewModel model = (EditViewModel)this.BindingContext;
            
            args.Surface.Canvas.Clear();
            if (model.Bitmap != null)
            {
                var bitmap = model.Bitmap;
                var bitmapInfo = bitmap.Info;
                float c = bitmapInfo.Height * 1.0f / bitmapInfo.Width;
                info.Height = (int)Math.Round(info.Width * c);

                canvas.Clear();
                canvas.DrawBitmap(model.Bitmap, info.Rect);
            }
        }

        
    }
}