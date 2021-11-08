using FFImageLoading;
using FFImageLoading.Transformations;
using ImageProcessingApp.Mobile.Services.ImageProcessing;
using ImageProcessingApp.Mobile.Views;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ImageProcessingApp.Mobile.ViewModels
{
    public class EditViewModel : BaseViewModel
    {
        private static readonly object bitmapLock = new object();

        private bool sliderNeeded = false;

        private Thread sliderUpdateThread = null;

        public bool SliderNeeded
        {
            get => sliderNeeded;
            set => SetProperty(ref sliderNeeded, value);
        }

        private ICommand sliderChangedCommand = null;

        private float sliderValue;
        public float SliderValue
        {
            get => sliderValue;
            set
            {
                SetProperty(ref sliderValue, value);
                if (sliderUpdateThread != null)
                    sliderUpdateThread.Abort();
                sliderUpdateThread = new Thread(() =>
                {
                    sliderChangedCommand.Execute(this);
                });                
                sliderUpdateThread.Start();
                sliderUpdateThread.Join();
            }
        }

        private Xamarin.Forms.Color colorValue1;
        public Xamarin.Forms.Color ColorValue1
        {
            get => colorValue1;
            set
            {
                SetProperty(ref colorValue1, value);
                if (sliderUpdateThread != null)
                    sliderUpdateThread.Abort();
                sliderUpdateThread = new Thread(() =>
                {
                    sliderChangedCommand.Execute(this);
                });
                sliderUpdateThread.Start();
                sliderUpdateThread.Join();
            }
        }

        private Xamarin.Forms.Color colorValue2;
        public Xamarin.Forms.Color ColorValue2
        {
            get => colorValue2;
            set
            {
                SetProperty(ref colorValue2, value);
                if (sliderUpdateThread != null)
                    sliderUpdateThread.Abort();
                sliderUpdateThread = new Thread(() =>
                {
                    sliderChangedCommand.Execute(this);
                });
                sliderUpdateThread.Start();
                sliderUpdateThread.Join();
            }
        }

        private ImageSource image;
        public ImageSource Image 
        { 
            get => image;
            set => SetProperty(ref image, value);
        }

        private bool imageLoaded = false;
        public bool ImageLoaded
        {
            get => imageLoaded;
            set => SetProperty(ref imageLoaded, value);
        }

        private SKBitmap bitmapLoaded;
        public SKBitmap BitmapLoaded
        {
            get => bitmapLoaded;
            set => SetProperty(ref bitmapLoaded, value);
        }

        private SKBitmap bitmapProcessed;
        public SKBitmap BitmapProcessed
        {
            get => bitmapProcessed;
            set => SetProperty(ref bitmapProcessed, value);
        }

        private SKBitmap bitmapGrayscaled;

        public EditViewModel()
        {
            Title = "Edit Image";
            LatestCommand = new Command(async () =>
            {
                await Application.Current.MainPage.Navigation.PushAsync(new LatestPage(), false);
            });
            TakePhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await TakePhotoAsync()));
            PickPhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await PickPhotoAsync()));
            Pasterize = new Command(async () => 
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        SliderNeeded = false;
                        DisposeProcessedBitmap();
                        SKBitmap bitmap = BitmapLoaded.Copy();
                        ImageProcessor.PasterizeBitmap(bitmap);
                        BitmapProcessed = bitmap;
                    }
                });
            });
            Grayscale = new Command(async () =>
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        SliderNeeded = false;
                        DisposeProcessedBitmap();
                        BitmapProcessed = bitmapGrayscaled.Copy();
                    }
                });
            });
            Binarize = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    var r1 = (byte)Math.Max((int)Math.Floor(colorValue1.R * 255), 0);
                    var g1 = (byte)Math.Max((int)Math.Floor(colorValue1.G * 255), 0);
                    var b1 = (byte)Math.Max((int)Math.Floor(colorValue1.B * 255), 0);
                    var r2 = (byte)Math.Max((int)Math.Floor(colorValue2.R * 255), 0);
                    var g2 = (byte)Math.Max((int)Math.Floor(colorValue2.G * 255), 0);
                    var b2 = (byte)Math.Max((int)Math.Floor(colorValue2.B * 255), 0);
                    var color1 = System.Drawing.Color.FromArgb(255, r1, g1, b1);
                    var color2 = System.Drawing.Color.FromArgb(255, r2, g2, b2);
                    SliderNeeded = true;
                    sliderChangedCommand = Binarize;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    byte treshold = (byte)Math.Max((int)Math.Floor(sliderValue * 255), 0);
                    ImageProcessor.BinarizeBitmap(bitmap, bitmapGrayscaled, treshold, color1, color2);
                    BitmapProcessed = bitmap;
                }
            });
            Invert = new Command(async () =>
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        SliderNeeded = false;
                        DisposeProcessedBitmap();
                        SKBitmap bitmap = BitmapLoaded.Copy();
                        ImageProcessor.InvertBitmap(bitmap);
                        BitmapProcessed = bitmap;
                    }
                });
            });
            RestoreImage = new Command(async () =>
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        SliderNeeded = false;
                        RestoreBitmapImage();
                    }
                });
            });
            ResetImage = new Command(async () =>
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        ResetBitmapImage();
                    }
                });
            });
        }


        public ICommand LatestCommand { get; }
        public ICommand ResetImage { get; }
        public ICommand RestoreImage { get; }
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }
        public ICommand Pasterize { get; }
        public ICommand Grayscale { get; }
        public ICommand Invert { get; }
        public ICommand Binarize { get; }
        public ICommand BinarizeUpdate { get; }

        private void RestoreBitmapImage()
        {
            BitmapProcessed = BitmapLoaded.Copy();
        }

        private void DisposeProcessedBitmap()
        {
            BitmapProcessed.Dispose();
            BitmapProcessed = null;
        }

        private void ResetBitmapImage()
        {
            BitmapLoaded.Dispose();
            BitmapProcessed.Dispose();
            bitmapGrayscaled.Dispose();
            bitmapGrayscaled = null;
            BitmapLoaded = null;
            BitmapProcessed = null;
            ImageLoaded = false;
            SliderNeeded = false;
        }

        private async Task TakePhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                await LoadPhotoAsync(photo);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Console.WriteLine($"CapturePhotoAsync THREW FeatureNotSupportedException: {fnsEx.Message}");
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"CapturePhotoAsync THREW PermissionException: {pEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }

        private async Task PickPhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                await LoadPhotoAsync(photo);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Console.WriteLine($"PickPhotoAsync THREW FeatureNotSupportedException: {fnsEx.Message}");
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"PickPhotoAsync THREW PermissionException: {pEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PickPhotoAsync THREW: {ex.Message}");
            }
        }

        async Task LoadPhotoAsync(FileResult photo)
        {
            if (photo == null)
            {
                return;
            }
            SKManagedStream stream = new SKManagedStream(await photo.OpenReadAsync());
            SKCodec codec = SKCodec.Create(stream);
            SKBitmap bitmap = SKBitmap.Decode(await photo.OpenReadAsync());
            bitmap = AutoOrient(bitmap, codec.EncodedOrigin);
            BitmapLoaded = bitmap;
            BitmapProcessed = BitmapLoaded.Copy();
            ImageLoaded = true;
            SliderNeeded = false;
            bitmapGrayscaled = BitmapLoaded.Copy();
            ImageProcessor.Grayscale(bitmapGrayscaled);
        }

        private static SKBitmap AutoOrient(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            SKBitmap rotated;
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:
                    using (var surface = new SKCanvas(bitmap))
                    {
                        surface.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
                        surface.DrawBitmap(bitmap.Copy(), 0, 0);
                    }
                    return bitmap;
                case SKEncodedOrigin.RightTop:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(rotated.Width, 0);
                        surface.RotateDegrees(90);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;
                case SKEncodedOrigin.LeftBottom:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(0, rotated.Height);
                        surface.RotateDegrees(270);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;
                default:
                    return bitmap;
            }
        }
    }
}