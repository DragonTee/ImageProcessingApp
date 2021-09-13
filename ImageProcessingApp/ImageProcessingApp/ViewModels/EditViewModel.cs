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
        private ImageSource image;
        public ImageSource Image { 
            get => image;
            set => SetProperty(ref image, value);
        }

        public EditViewModel()
        {
            Title = "Edit Image";
            TakePhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async() => await TakePhotoAsync()));
            PickPhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async() => await PickPhotoAsync()));
        }

        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }

        async Task TakePhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                await LoadPhotoAsync(photo);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature is not supported on the device
            }
            catch (PermissionException pEx)
            {
                // Permissions not granted
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }

        async Task PickPhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                await LoadPhotoAsync(photo);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature is not supported on the device
            }
            catch (PermissionException pEx)
            {
                // Permissions not granted
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }

        async Task LoadPhotoAsync(FileResult photo)
        {
            if (photo == null)
            {
                return;
            }
            /*var t = ImageService.Instance.LoadStream(async c => await photo.OpenReadAsync());

            if (Device.RuntimePlatform == Device.iOS)
                t.Transform(new RotateTransformation(90));
            var imageStream = await t.AsPNGStreamAsync();*/

            Console.WriteLine("1----------");
            SKBitmap bitmap = SKBitmap.Decode(await photo.OpenReadAsync());
            Console.WriteLine("2----------");

            IntPtr pixelsAddr = bitmap.GetPixels();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();

            unsafe
            {
                uint* ptr = (uint*)bitmap.GetPixels().ToPointer();
                int pixelCount = bitmap.Width * bitmap.Height;

                for (int i = 0; i < pixelCount; i++)
                {
                    *ptr++ &= 0xE0E0E0FF;
                }
            }
            /*
            unsafe
            {
                byte* ptr = (byte*)pixelsAddr.ToPointer();

                for (int row = 0; row < bitmap.Height; row++)
                    for (int col = 0; col < bitmap.Width; col++)
                    {
                        var r = *ptr;
                        var g = *(ptr+1);
                        var b = *(ptr+2);
                        var brightest = r > g ? r : g;
                        brightest = brightest > b ? brightest : b;
                        *ptr++ = brightest;
                        *ptr++ = brightest;
                        *ptr++ = brightest;
                        *ptr++ = 0xFF;
                        //*ptr
                        //*ptr++ = MakePixel((byte)(col%255), 0, (byte)(row%255), 0xFF);
                    }
            }*/
            Console.WriteLine($"Photo filtering time: {stopwatch.ElapsedMilliseconds} ms");

            var image = SKImage.FromBitmap(bitmap);
            var data = image.Encode();
            var stream = data.AsStream();

            Image = ImageSource.FromStream(() => stream);
            Console.WriteLine("PhotoLoaded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
            (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
    }
}