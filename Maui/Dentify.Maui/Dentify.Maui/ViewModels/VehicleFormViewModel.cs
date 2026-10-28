using System.ComponentModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dentify.Contracts.Models;
using Dentify.Maui.Messages;
using Dentify.Maui.Services;
using Dentify.Maui.Views;

namespace Dentify.Maui.ViewModels
{
    // Adds a new vehicle or edits an existing one. Singleton: it receives the vehicle before the page appears.
    public partial class VehicleFormViewModel : BaseViewModel, IRecipient<VehicleFormRequestedMessage>
    {
        private const string CarType = "Car";
        private const string VanType = "Van";

        // Current Belgian format without separators, e.g. 1ABC123 becomes 1-ABC-123
        private static readonly Regex CurrentPlate = new("^([1-9])([A-Z]{3})([0-9]{3})$");

        private IApiService _apiService;
        private IPhotoService _photoService;
        private INavigationService _navigationService;

        // 0 for a new vehicle
        private int _vehicleId;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        public partial string LicensePlate { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        public partial string Brand { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        public partial string Model { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsVan))]
        public partial bool IsCar { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageTitle), nameof(SaveButtonText))]
        public partial bool IsEditing { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasScanMessage))]
        public partial string? ScanMessage { get; set; }

        [ObservableProperty]
        public partial bool IsScanning { get; set; }

        public bool IsVan => !IsCar;
        public bool HasScanMessage => !string.IsNullOrEmpty(ScanMessage);
        public bool IsCaptureSupported => _photoService.IsCaptureSupported;
        public string PageTitle => IsEditing ? "Edit vehicle" : "Add vehicle";
        public string SaveButtonText => IsEditing ? "Save changes" : "Save vehicle";

        public VehicleFormViewModel(IApiService apiService, IPhotoService photoService,
            INavigationService navigationService)
        {
            _apiService = apiService;
            _photoService = photoService;
            _navigationService = navigationService;
            LicensePlate = string.Empty;
            Brand = string.Empty;
            Model = string.Empty;
            IsCar = true;

            WeakReferenceMessenger.Default.Register<VehicleFormRequestedMessage>(this);
        }

        // Every request starts a fresh form, filled with the vehicle to edit or the scanned plate
        public void Receive(VehicleFormRequestedMessage message)
        {
            var vehicle = message.Value;
            _vehicleId = vehicle.Id;
            IsEditing = vehicle.Id != 0;
            LicensePlate = vehicle.LicensePlate ?? string.Empty;
            Brand = vehicle.Brand ?? string.Empty;
            Model = vehicle.Model ?? string.Empty;
            IsCar = vehicle.Type != VanType;
            ErrorMessage = null;
            ScanMessage = null;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsBusy))
                SaveCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void SelectCar() => IsCar = true;

        [RelayCommand]
        private void SelectVan() => IsCar = false;

        [RelayCommand]
        private async Task ScanPlateWithCameraAsync()
        {
            await ScanPlateAsync(() => _photoService.CapturePhotoAsync());
        }

        [RelayCommand]
        private async Task ScanPlateFromGalleryAsync()
        {
            await ScanPlateAsync(() => _photoService.PickPhotoAsync());
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            var vehicle = new VehicleListDto
            {
                Id = _vehicleId,
                LicensePlate = FormatPlate(LicensePlate),
                Brand = Brand.Trim(),
                Model = Model.Trim(),
                Type = IsCar ? CarType : VanType
            };

            try
            {
                if (IsEditing)
                {
                    await _apiService.UpdateVehicleAsync(_vehicleId, new VehicleForUpdateDto
                    {
                        LicensePlate = vehicle.LicensePlate,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Type = vehicle.Type
                    });
                }
                else
                {
                    var created = await _apiService.CreateVehicleAsync(new VehicleForCreationDto
                    {
                        LicensePlate = vehicle.LicensePlate,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Type = vehicle.Type
                    });
                    vehicle.Id = created.Id;
                }

                WeakReferenceMessenger.Default.Send(new VehicleSavedMessage(vehicle));
                // The detail page shows (or reloads) this vehicle
                WeakReferenceMessenger.Default.Send(new VehicleSelectedMessage(vehicle));

                if (IsEditing)
                    await _navigationService.GoBackAsync();
                else
                    // Replace this page with the new vehicle, so back goes to the vehicle list
                    await _navigationService.NavigateToAsync($"../{nameof(VehicleDetailPage)}");
            }
            catch (ApiException exception)
            {
                // A duplicate plate comes back as 409 with a readable message
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSave() =>
            !IsBusy
            && !string.IsNullOrWhiteSpace(LicensePlate)
            && !string.IsNullOrWhiteSpace(Brand)
            && !string.IsNullOrWhiteSpace(Model);

        // Reads the plate with the same OCR as the scan page, so it does not have to be typed
        private async Task ScanPlateAsync(Func<Task<FileResult?>> getPhoto)
        {
            if (IsBusy) return;
            ErrorMessage = null;
            ScanMessage = null;

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
            IsScanning = true;
            try
            {
                var result = await _apiService.ScanPlateAsync(photo);

                if (string.IsNullOrWhiteSpace(result.RecognizedPlate))
                    ErrorMessage = "No license plate found on this photo. Try again closer and straight on, or type it.";
                else if (result.Vehicle != null && result.Vehicle.Id != _vehicleId)
                    ErrorMessage = $"{result.RecognizedPlate} is already in the fleet ({result.Vehicle.Brand} {result.Vehicle.Model}).";
                else
                {
                    LicensePlate = result.RecognizedPlate;
                    ScanMessage = $"Plate read: {result.RecognizedPlate}. Check it before saving.";
                }
            }
            catch (ApiException exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsScanning = false;
                IsBusy = false;
            }
        }

        // Stores plates the way the plate scan returns them, so a later scan finds the vehicle
        private static string FormatPlate(string plate)
        {
            var compact = plate.Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty);
            var match = CurrentPlate.Match(compact);
            return match.Success
                ? $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}"
                : plate.Trim().ToUpperInvariant();
        }
    }
}
