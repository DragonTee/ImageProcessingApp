using FFImageLoading;
using FFImageLoading.Transformations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var t = ImageService.Instance.LoadStream(async c => await photo.OpenReadAsync());

            if (Device.RuntimePlatform == Device.iOS)
                t.Transform(new RotateTransformation(90));

            var imageStream = await t.AsPNGStreamAsync();
            
            Image = ImageSource.FromStream(() => imageStream);
            Console.WriteLine("PhotoLoaded");
        }
    }
}