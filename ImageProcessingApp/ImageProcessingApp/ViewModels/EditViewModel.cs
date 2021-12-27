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

        public class ColorData
        {
            public int pixelCount;
            public int pixelIntensity;
            public ColorData(int count, int intensity)
            {
                pixelCount = count;
                pixelIntensity = intensity;
            }
        }

        private List<ColorData> imageDataRed = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataRed {
            get => imageDataRed;
            set => SetProperty(ref imageDataRed, value);
        }

        private List<ColorData> imageDataGreen = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataGreen
        {
            get => imageDataGreen;
            set => SetProperty(ref imageDataGreen, value);
        }

        private List<ColorData> imageDataBlue = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataBlue
        {
            get => imageDataBlue;
            set => SetProperty(ref imageDataBlue, value);
        }

        private List<ColorData> imageDataAll = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataAll
        {
            get => imageDataAll;
            set => SetProperty(ref imageDataAll, value);
        }

        private List<ColorData> imageDataSliceR = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataSliceR
        {
            get => imageDataSliceR;
            set => SetProperty(ref imageDataSliceR, value);
        }

        private List<ColorData> imageDataSliceG = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataSliceG
        {
            get => imageDataSliceG;
            set => SetProperty(ref imageDataSliceG, value);
        }

        private List<ColorData> imageDataSliceB = new List<ColorData>() { new ColorData(10, 1), new ColorData(20, 2) };
        public List<ColorData> ImageDataSliceB
        {
            get => imageDataSliceB;
            set => SetProperty(ref imageDataSliceB, value);
        }

        public bool SliderNeeded
        {
            get => sliderNeeded;
            set => SetProperty(ref sliderNeeded, value);
        }

        private bool colorPickerNeeded = false;
        public bool ColorPickerNeeded
        {
            get => colorPickerNeeded;
            set => SetProperty(ref colorPickerNeeded, value);
        }

        private bool secondcolorPickerNeeded = false;
        public bool SecondcolorPickerNeeded
        {
            get => secondcolorPickerNeeded;
            set => SetProperty(ref secondcolorPickerNeeded, value);
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
            }
        }

        private Xamarin.Forms.Color colorValue2;
        public Xamarin.Forms.Color ColorValue2
        {
            get => colorValue2;
            set
            {
                SetProperty(ref colorValue2, value);
            }
        }

        private ImageSource image;
        public ImageSource Image 
        { 
            get => image;
            set => SetProperty(ref image, value);
        }

        private bool showingStats = false;
        public bool ShowingStats
        {
            get => showingStats;
            set => SetProperty(ref showingStats, value);
        }

        private bool statsAvailable = false;
        public bool StatsAvailable
        {
            get => statsAvailable;
            set => SetProperty(ref statsAvailable, value);
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
            ApplyCommand = new Command(() =>
            {
                BitmapLoaded.Dispose();
                BitmapLoaded = BitmapProcessed.Copy();
                bitmapGrayscaled.Dispose();
                bitmapGrayscaled = BitmapLoaded.Copy();
                ImageProcessor.Grayscale(bitmapGrayscaled);
            });

            StatsCommand = new Command(() =>
            {
                var dataR = ImageProcessor.GetRedChannelStats(bitmapProcessed);
                var dataG = ImageProcessor.GetGreenChannelStats(bitmapProcessed);
                var dataB = ImageProcessor.GetBlueChannelStats(bitmapProcessed);
                var dataX = ImageProcessor.GetCombinedChannelStats(bitmapGrayscaled);
                var slice = ImageProcessor.GetMiddleSliceStats(bitmapProcessed);
                var newListR = new List<ColorData>();
                var newListG = new List<ColorData>();
                var newListB = new List<ColorData>();
                var newListX = new List<ColorData>();
                var newSliceR = new List<ColorData>();
                var newSliceG = new List<ColorData>();
                var newSliceB = new List<ColorData>();

                for (int i = 0; i < bitmapProcessed.Width; i++)
                {
                    newSliceR.Add(new ColorData(slice[i * 3], i));
                    newSliceG.Add(new ColorData(slice[i * 3 + 1], i));
                    newSliceB.Add(new ColorData(slice[i * 3 + 2], i));
                }                

                for (int i = 0; i < 256; i++)
                {
                    newListR.Add(new ColorData(dataR[i], i));
                    newListG.Add(new ColorData(dataG[i], i));
                    newListB.Add(new ColorData(dataB[i], i));
                    newListX.Add(new ColorData(dataX[i], i));
                }
                ShowingStats = true;
                StatsAvailable = false;
                ImageDataRed = newListR;
                ImageDataGreen = newListG;
                ImageDataBlue = newListB;
                ImageDataAll = newListX;
                ImageDataSliceR = newSliceR;
                ImageDataSliceG = newSliceG;
                ImageDataSliceB = newSliceB;
            });
            EditCommand = new Command(() =>
            {
                ShowingStats = false;
                StatsAvailable = true;
            });
            TakePhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await TakePhotoAsync()));
            PickPhoto = new Command(() => MainThread.BeginInvokeOnMainThread(async () => await PickPhotoAsync()));
            Pasterize = new Command(async () => 
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        ColorPickerNeeded = false;
                        SecondcolorPickerNeeded = false;
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
                        ColorPickerNeeded = false;
                        SecondcolorPickerNeeded = false;
                        SliderNeeded = false;
                        DisposeProcessedBitmap();
                        BitmapProcessed = bitmapGrayscaled.Copy();
                    }
                });
            });
            BrightnessCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = true;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    sliderChangedCommand = BrightnessCommand;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    int deltaValue = (int)Math.Floor(sliderValue * 511) - 255;
                    ImageProcessor.Brighten(bitmap, deltaValue);
                    BitmapProcessed = bitmap;
                }
            });
            ContrastCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = true;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    sliderChangedCommand = ContrastCommand;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    int multiplier = (int)Math.Floor(sliderValue * 20000);
                    int divisor = 10000;
                    ImageProcessor.Contrast(bitmap, multiplier, divisor);
                    BitmapProcessed = bitmap;
                }
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
                    ColorPickerNeeded = true;
                    SecondcolorPickerNeeded = true;
                    sliderChangedCommand = Binarize;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    byte treshold = (byte)Math.Max((int)Math.Floor(sliderValue * 255), 0);
                    ImageProcessor.BinarizeBitmap(bitmap, bitmapGrayscaled, treshold, color1, color2);
                    BitmapProcessed = bitmap;
                }
            });
            NoiseCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = true;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    sliderChangedCommand = NoiseCommand;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.ApplySaltPepperNoise(bitmap, sliderValue);
                    BitmapProcessed = bitmap;
                }
            });
            LinearFilterCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.LinearFilter(bitmap, BitmapLoaded);
                    BitmapProcessed = bitmap;
                }
            });
            MedianFilterCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.MedianFilter(bitmap, BitmapLoaded, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            StatEdgesCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.StatEdges(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            WallaceCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.Wallace(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            SobelCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.Sobel(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            RobertsCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.Roberts(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            LaplaceCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.Laplace(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            KirschCommand = new Command(async () =>
            {
                lock (bitmapLock)
                {
                    SliderNeeded = false;
                    ColorPickerNeeded = false;
                    SecondcolorPickerNeeded = false;
                    DisposeProcessedBitmap();
                    SKBitmap bitmap = BitmapLoaded.Copy();
                    ImageProcessor.Kirsch(bitmap, bitmapGrayscaled);
                    BitmapProcessed = bitmap;
                }
            });
            Invert = new Command(async () =>
            {
                await Task.Run(() =>
                {
                    lock (bitmapLock)
                    {
                        ColorPickerNeeded = false;
                        SecondcolorPickerNeeded = false;
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
                        ColorPickerNeeded = false;
                        SecondcolorPickerNeeded = false;
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

        public ICommand StatEdgesCommand { get; }
        public ICommand WallaceCommand { get; }
        public ICommand SobelCommand { get; }
        public ICommand RobertsCommand { get; }
        public ICommand LaplaceCommand { get; }
        public ICommand KirschCommand { get; }
        public ICommand LatestCommand { get; }
        public ICommand StatsCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand BrightnessCommand { get; }
        public ICommand ContrastCommand { get; }
        public ICommand ResetImage { get; }
        public ICommand RestoreImage { get; }
        public ICommand TakePhoto { get; }
        public ICommand PickPhoto { get; }
        public ICommand Pasterize { get; }
        public ICommand Grayscale { get; }
        public ICommand Invert { get; }
        public ICommand Binarize { get; }
        public ICommand NoiseCommand { get; }
        public ICommand LinearFilterCommand { get; }
        public ICommand MedianFilterCommand { get; }        

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
            ColorPickerNeeded = false;
            SecondcolorPickerNeeded = false;
            ShowingStats = false;
            StatsAvailable = false;
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
            StatsAvailable = true;
            ShowingStats = false;
            SliderNeeded = false;
            ColorPickerNeeded = false;
            SecondcolorPickerNeeded = false;
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