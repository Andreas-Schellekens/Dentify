using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dentify.Contracts.Models;
using Dentify.Maui.Messages;
using Dentify.Maui.Models;
using Dentify.Maui.Services;
using Dentify.Maui.Views;

namespace Dentify.Maui.ViewModels
{
    // Transient: every scan starts on a clean page
    public partial class PlateScanViewModel : BaseViewModel
    {
        private IApiService _apiService;
        private IPhotoService _photoService;
        private INavigationService _navigationService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPhoto), nameof(ShowIntro))]
        public partial ImageSource? Photo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasResult), nameof(IsMatch), nameof(IsUnknownPlate), nameof(IsNoPlate),
            nameof(ShowCaptureButtons), nameof(ShowIntro))]
        public partial PlateScanResultDto? Result { get; set; }

        public bool IsCaptureSupported => _photoService.IsCaptureSupported;

        public bool HasPhoto => Photo != null;
        public bool ShowIntro => Photo == null && Result == null;
        public bool HasResult => Result != null;
        public bool IsMatch => Result?.Vehicle != null;
        public bool IsUnknownPlate => Result != null && Result.Vehicle == null && !string.IsNullOrWhiteSpace(Result.RecognizedPlate);
        public bool IsNoPlate => Result != null && string.IsNullOrWhiteSpace(Result.RecognizedPlate);
        public bool ShowCaptureButtons => Result == null;

        public PlateScanViewModel(IApiService apiService, IPhotoService photoService,
            INavigationService navigationService)
        {
            _apiService = apiService;
            _photoService = photoService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task TakePhotoAsync()
        {
            await ScanAsync(() => _photoService.CapturePhotoAsync());
        }

        [RelayCommand]
        private async Task PickPhotoAsync()
        {
            await ScanAsync(() => _photoService.PickPhotoAsync());
        }

        [RelayCommand]
        private void ScanAgain()
        {
            Result = null;
            Photo = null;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task OpenVehicleAsync()
        {
            if (Result?.Vehicle == null) return;

            WeakReferenceMessenger.Default.Send(new VehicleSelectedMessage(Result.Vehicle));
            // Replace the scan page, so back goes to the vehicle list
            await _navigationService.NavigateToAsync($"../{nameof(VehicleDetailPage)}");
        }

        [RelayCommand]
        private async Task StartInspectionAsync()
        {
            if (Result?.Vehicle == null) return;

            WeakReferenceMessenger.Default.Send(new VehicleSelectedMessage(Result.Vehicle));
            // Vehicle detail goes under the new inspection, so after saving back leads to the vehicle
            await _navigationService.NavigateToAsync($"../{nameof(VehicleDetailPage)}/{nameof(NewInspectionPage)}");
        }

        [RelayCommand]
        private async Task AddScannedVehicleAsync()
        {
            if (Result == null) return;

            WeakReferenceMessenger.Default.Send(new VehicleFormRequestedMessage(
                new VehicleListDto { LicensePlate = Result.RecognizedPlate ?? string.Empty }));
            // Replace the scan page, so back goes to the vehicle list
            await _navigationService.NavigateToAsync($"../{nameof(VehicleFormPage)}");
        }

        [RelayCommand]
        private async Task ChooseFromListAsync()
        {
            if (Result != null)
                WeakReferenceMessenger.Default.Send(new PlateScannedMessage(Result));

            await _navigationService.GoBackAsync();
        }

        private async Task ScanAsync(Func<Task<FileResult?>> getPhoto)
        {
            if (IsBusy) return;
            ErrorMessage = null;

            FileResult? photo;
            try
            {
                photo = await getPhoto();
            }
            catch (PhotoException exception)
            {
                ErrorMessage = exception.Message;
                return;
            }

            // Cancelled by the user
            if (photo == null) return;

            IsBusy = true;
            try
            {
                Photo = (await SelectedPhoto.CreateAsync(photo)).Preview;
                Result = await _apiService.ScanPlateAsync(photo);
            }
            catch (ApiException exception)
            {
                ErrorMessage = exception.Message;
                Photo = null;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
