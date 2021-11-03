using FFImageLoading;
using FFImageLoading.Transformations;
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
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ImageProcessingApp.ViewModels
{
    public class EditViewModel : BaseViewModel
    {
        private static readonly object bitmapLock = new object();

        private bool sliderNeeded = false;
        public bool SliderNeeded
        {
            get => sliderNeeded;
            set => SetProperty(ref sliderNeeded, value);
        }

        private float sliderValue;
        public float SliderValue
        {
            get => sliderValue;
            set
            {
                MainThread.BeginInvokeOnMainThread(async () => await BinarizeBitmap());
                SetProperty(ref sliderValue, value);                
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

        private bool effectSelected = false;
        public bool EffectSelected
        {
            get => effectSelected;
            set => SetProperty(ref effectSelected, value);
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
            TakePhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await TakePhotoAsync()));
            PickPhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await PickPhotoAsync()));
            Pasterize = new Command(async () => await PasterizeBitmap());
            Grayscale = new Command(async () => await GrayscaleBitmap());
            Binarize = new Command(async () => await BinarizeBitmap());
            Invert = new Command(async () => await InvertBitmap());
            RestoreImage = new Command(() => RestoreBitmapImage());
            ResetImage = new Command(() => ResetBitmapImage());
        }

        public ICommand ResetImage { get; }
        public ICommand RestoreImage { get; }
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }
        public ICommand Pasterize { get; }
        public ICommand Grayscale { get; }
        public ICommand Invert { get; }
        public ICommand Binarize { get; }

        private void RestoreBitmapImage()
        {
            EffectSelected = false;
            BitmapProcessed = BitmapLoaded.Copy();
        }

        private void ResetBitmapImage()
        {
            EffectSelected = false;
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
            await InitializeGrayscaledBitmap();
            return;
        }

        private async Task PasterizeBitmap()
        {
            await Task.Run(() =>
            {
                lock (bitmapLock)
                {
                    SKBitmap bitmapNew = new SKBitmap(BitmapLoaded.Width, BitmapLoaded.Height);
                    unsafe
                    {
                        uint* ptrIn = (uint*)BitmapLoaded.GetPixels().ToPointer();
                        uint* ptrOut = (uint*)bitmapNew.GetPixels().ToPointer();
                        int pixelCount = BitmapLoaded.Width * BitmapLoaded.Height;

                        for (int i = 0; i < pixelCount; i++)
                        {
                            *ptrOut++ = *ptrIn++ & 0xE0E0E0FF;
                        }
                    }
                    bitmapProcessed.Dispose();
                    BitmapProcessed = bitmapNew;
                    EffectSelected = true;
                    SliderNeeded = false;
                }
            });
        }

        private async Task InitializeGrayscaledBitmap()
        {
            await Task.Run(() =>
            {
                lock (bitmapLock)
                {
                    SKBitmap bitmapNew = new SKBitmap(BitmapLoaded.Width, BitmapLoaded.Height);
                    unsafe
                    {
                        uint* ptrIn = (uint*)BitmapLoaded.GetPixels().ToPointer();
                        uint* ptrOut = (uint*)bitmapNew.GetPixels().ToPointer();
                        int pixelCount = BitmapLoaded.Width * BitmapLoaded.Height;

                        for (int i = 0; i < pixelCount; i++)
                        {
                            byte value = (byte)((*ptrIn & 255) * 3 / 10 + ((*ptrIn >> 8) & 255) * 59 / 100 + ((*ptrIn >> 16) & 255) * 11 / 100);
                            ptrIn++;
                            *ptrOut++ = MakePixel(value, value, value, 255);
                        }
                    }
                    if (bitmapGrayscaled != null)
                        bitmapGrayscaled.Dispose();
                    bitmapGrayscaled = bitmapNew;
                }
            });
        }

        private async Task BinarizeBitmap()
        {
            await Task.Run(() =>
            {
                lock (bitmapLock)
                {
                    SKBitmap bitmapNew = new SKBitmap(bitmapGrayscaled.Width, bitmapGrayscaled.Height);
                    unsafe
                    {
                        byte binarizationTheshold = (byte)Convert.ToInt32(Math.Max(0, 255 * sliderValue));
                        uint* ptrIn = (uint*)bitmapGrayscaled.GetPixels().ToPointer();
                        uint* ptrOut = (uint*)bitmapNew.GetPixels().ToPointer();
                        int pixelCount = bitmapGrayscaled.Width * bitmapGrayscaled.Height;

                        for (int i = 0; i < pixelCount; i++)
                        {
                            byte value = (byte)((*ptrIn & 255) > binarizationTheshold ? 255 : 0);
                            ptrIn++;
                            *ptrOut++ = MakePixel(value, value, value, 255);
                        }
                    }
                    BitmapProcessed.Dispose();
                    BitmapProcessed = bitmapNew;
                    SliderNeeded = true;
                }
            });
        }

        private async Task InvertBitmap()
        {
            await Task.Run(() =>
            {
                lock (bitmapLock)
                {
                    SKBitmap bitmapNew = new SKBitmap(bitmapLoaded.Width, bitmapLoaded.Height);
                    unsafe
                    {
                        uint* ptrIn = (uint*)bitmapLoaded.GetPixels().ToPointer();
                        uint* ptrOut = (uint*)bitmapNew.GetPixels().ToPointer();
                        int pixelCount = bitmapGrayscaled.Width * bitmapGrayscaled.Height;

                        for (int i = 0; i < pixelCount; i++)
                        {
                            byte r = (byte)(255 - (*ptrIn & 255));
                            byte g = (byte)(255 - ((*ptrIn >> 8) & 255));
                            byte b = (byte)(255 - ((*ptrIn >> 16) & 255));
                            ptrIn++;
                            *ptrOut++ = MakePixel(r, g, b, 255);
                        }
                    }
                    BitmapProcessed.Dispose();
                    BitmapProcessed = bitmapNew;
                    SliderNeeded = false;
                }
            });
        }

        private async Task GrayscaleBitmap()
        {
            await Task.Run(() =>
            {
                lock (bitmapLock)
                {
                    bitmapProcessed.Dispose();
                    BitmapProcessed = bitmapGrayscaled.Copy();
                    EffectSelected = true;
                    SliderNeeded = false;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
            (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);

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