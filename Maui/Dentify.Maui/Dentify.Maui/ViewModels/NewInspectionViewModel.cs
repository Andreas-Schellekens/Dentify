using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public partial class NewInspectionViewModel : BaseViewModel, IRecipient<VehicleSelectedMessage>
    {
        public const int MaximumPhotoCount = 8;

        private IApiService _apiService;
        private IPhotoService _photoService;
        private ISettingsService _settingsService;
        private INavigationService _navigationService;

        // Set once the inspection exists, so a failed photo upload can be retried without a second inspection
        private int? _createdInspectionId;
        private bool _typeChosenByUser;

        [ObservableProperty]
        public partial VehicleListDto? Vehicle { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsReturn), nameof(TypeHint))]
        public partial bool IsDeparture { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        public partial string InspectorName { get; set; }

        [ObservableProperty]
        public partial string? ProgressText { get; set; }

        public ObservableCollection<SelectedPhoto> Photos { get; } = new();

        public bool IsReturn => !IsDeparture;
        public bool IsCaptureSupported => _photoService.IsCaptureSupported;
        public bool HasPhotos => Photos.Count > 0;
        public bool CanAddPhotos => Photos.Count < MaximumPhotoCount && !IsBusy;
        public string PhotoCountText => $"{Photos.Count} of {MaximumPhotoCount} photos";

        public string TypeHint => IsDeparture
            ? "The vehicle leaves: record its condition before handing it over."
            : "The vehicle comes back: Dentify compares it with the previous inspection.";

        public NewInspectionViewModel(IApiService apiService, IPhotoService photoService,
            ISettingsService settingsService, INavigationService navigationService)
        {
            _apiService = apiService;
            _photoService = photoService;
            _settingsService = settingsService;
            _navigationService = navigationService;
            InspectorName = _settingsService.InspectorName;
            IsDeparture = true;

            Photos.CollectionChanged += OnPhotosChanged;
            WeakReferenceMessenger.Default.Register<VehicleSelectedMessage>(this);
        }

        // Every selected vehicle gets a fresh form
        public void Receive(VehicleSelectedMessage message)
        {
            Vehicle = message.Value;
            Photos.Clear();
            ErrorMessage = null;
            ProgressText = null;
            _createdInspectionId = null;
            _typeChosenByUser = false;
            InspectorName = _settingsService.InspectorName;
            IsDeparture = true;
            _ = SuggestTypeAsync(message.Value.Id);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(CanAddPhotos));
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private void SelectDeparture()
        {
            _typeChosenByUser = true;
            IsDeparture = true;
        }

        [RelayCommand]
        private void SelectReturn()
        {
            _typeChosenByUser = true;
            IsDeparture = false;
        }

        [RelayCommand]
        private async Task TakePhotoAsync()
        {
            if (!CanAddPhotos) return;
            ErrorMessage = null;

            try
            {
                var photo = await _photoService.CapturePhotoAsync();
                if (photo != null)
                    Photos.Add(await SelectedPhoto.CreateAsync(photo));
            }
            catch (PhotoException exception)
            {
                ErrorMessage = exception.Message;
            }
        }

        [RelayCommand]
        private async Task PickPhotosAsync()
        {
            if (!CanAddPhotos) return;
            ErrorMessage = null;

            try
            {
                var photos = await _photoService.PickPhotosAsync(MaximumPhotoCount - Photos.Count);
                foreach (var photo in photos)
                    Photos.Add(await SelectedPhoto.CreateAsync(photo));
            }
            catch (PhotoException exception)
            {
                ErrorMessage = exception.Message;
            }
        }

        [RelayCommand]
        private void RemovePhoto(SelectedPhoto photo)
        {
            if (!IsBusy && photo != null)
                Photos.Remove(photo);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (Vehicle == null) return;

            IsBusy = true;
            ErrorMessage = null;
            _settingsService.InspectorName = InspectorName;

            try
            {
                if (_createdInspectionId == null)
                {
                    ProgressText = "Creating the inspection…";
                    var created = await _apiService.CreateInspectionAsync(new InspectionForCreationDto
                    {
                        VehicleId = Vehicle.Id,
                        Type = IsDeparture ? "Departure" : "Return",
                        InspectorName = InspectorName.Trim()
                    });
                    _createdInspectionId = created.Id;
                }

                ProgressText = Photos.Count == 1
                    ? "Checking the photo for damage…"
                    : $"Checking {Photos.Count} photos for damage…";
                var inspection = await _apiService.UploadPhotosAsync(_createdInspectionId.Value,
                    Photos.Select(photo => photo.File).ToList());

                WeakReferenceMessenger.Default.Send(new InspectionSavedMessage(inspection));
                // Replace this page with the result, so back goes to the vehicle
                await _navigationService.NavigateToAsync($"../{nameof(InspectionResultPage)}");
            }
            catch (ApiException exception)
            {
                ErrorMessage = _createdInspectionId == null
                    ? exception.Message
                    : $"{exception.Message} The inspection is saved, tap Save to upload the photos again.";
            }
            finally
            {
                ProgressText = null;
                IsBusy = false;
            }
        }

        private bool CanSave() =>
            !IsBusy && Photos.Count > 0 && !string.IsNullOrWhiteSpace(InspectorName);

        private void OnPhotosChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasPhotos));
            OnPropertyChanged(nameof(CanAddPhotos));
            OnPropertyChanged(nameof(PhotoCountText));
            SaveCommand.NotifyCanExecuteChanged();
        }

        // A vehicle that left last time is most likely coming back now
        private async Task SuggestTypeAsync(int vehicleId)
        {
            try
            {
                var inspections = await _apiService.GetInspectionsForVehicleAsync(vehicleId);
                var latest = inspections.OrderByDescending(inspection => inspection.Date).FirstOrDefault();
                if (Vehicle?.Id == vehicleId && latest != null && !_typeChosenByUser)
                    IsDeparture = latest.Type == "Return";
            }
            catch (ApiException)
            {
                // Only a suggestion, the default stays Departure
            }
        }
    }
}
