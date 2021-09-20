using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;

namespace ImageProcessingApp.Controls
{
    public class BitmapCanvas : SKCanvasView
    {
        public static readonly BindableProperty BitmapProperty = BindableProperty.Create("Bitmap", typeof(SKBitmap), typeof(BitmapCanvas), null, propertyChanged: OnBitmapChanged);

        public SKBitmap Bitmap
        {
            get { return (SKBitmap)GetValue(BitmapProperty); }
            set 
            { 
                SetValue(BitmapProperty, value);                
            }
        }

        static void OnBitmapChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
                (bindable as BitmapCanvas).InvalidateSurface();
        }

        public BitmapCanvas() : base()
        {
        }
    }
}