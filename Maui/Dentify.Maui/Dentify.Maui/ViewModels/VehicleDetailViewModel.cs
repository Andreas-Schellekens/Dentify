using System.Collections.ObjectModel;
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
    public partial class VehicleDetailViewModel : BaseViewModel,
        IRecipient<VehicleSelectedMessage>, IRecipient<InspectionSavedMessage>
    {
        private IApiService _apiService;
        private IPhotoLoaderService _photoLoaderService;
        private INavigationService _navigationService;
        private int _loadVersion;

        [ObservableProperty]
        public partial VehicleListDto? Vehicle { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(InspectionCountText), nameof(LastInspectionText))]
        public partial ObservableCollection<InspectionListItem> Inspections { get; set; }

        [ObservableProperty]
        public partial bool IsRefreshing { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(ShowFirstLoad))]
        public partial bool HasLoaded { get; set; }

        public bool ShowFirstLoad => !HasLoaded && !HasError;
        public bool ShowEmptyState => HasLoaded && !HasError && Inspections.Count == 0;

        public string InspectionCountText => Inspections.Count == 1 ? "1 inspection" : $"{Inspections.Count} inspections";

        public string LastInspectionText => Inspections.Count == 0
            ? "Never inspected"
            : $"Last on {Inspections[0].Inspection.Date:dd MMM yyyy}";

        public VehicleDetailViewModel(IApiService apiService, IPhotoLoaderService photoLoaderService,
            INavigationService navigationService)
        {
            _apiService = apiService;
            _photoLoaderService = photoLoaderService;
            _navigationService = navigationService;
            Inspections = new ObservableCollection<InspectionListItem>();

            WeakReferenceMessenger.Default.Register<VehicleSelectedMessage>(this);
            WeakReferenceMessenger.Default.Register<InspectionSavedMessage>(this);
        }

        public void Receive(VehicleSelectedMessage message)
        {
            // Same vehicle (e.g. this page starts a new inspection): refresh quietly, keep the list on screen
            if (Vehicle?.Id == message.Value.Id && HasLoaded)
            {
                _ = LoadInspectionsAsync();
                return;
            }

            // Show the header right away and clear the previous vehicle before loading
            Vehicle = message.Value;
            Inspections = new ObservableCollection<InspectionListItem>();
            HasLoaded = false;
            ErrorMessage = null;
            _ = LoadInspectionsAsync();
        }

        // A new inspection of this vehicle was saved, so the history is out of date
        public void Receive(InspectionSavedMessage message)
        {
            if (Vehicle?.Id == message.Value.VehicleId)
                _ = LoadInspectionsAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadInspectionsAsync();
        }

        [RelayCommand]
        private async Task StartInspectionAsync()
        {
            if (Vehicle == null) return;

            WeakReferenceMessenger.Default.Send(new VehicleSelectedMessage(Vehicle));
            await _navigationService.NavigateToAsync(nameof(NewInspectionPage));
        }

        [RelayCommand]
        private async Task OpenInspectionAsync(InspectionListItem item)
        {
            if (item == null) return;

            WeakReferenceMessenger.Default.Send(new InspectionSelectedMessage(item.Inspection.Id));
            await _navigationService.NavigateToAsync(nameof(InspectionResultPage));
        }

        private async Task LoadInspectionsAsync()
        {
            if (Vehicle == null) return;

            var version = ++_loadVersion;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var vehicle = await _apiService.GetVehicleAsync(Vehicle.Id);
                if (version != _loadVersion) return;

                Vehicle = new VehicleListDto
                {
                    Id = vehicle.Id,
                    LicensePlate = vehicle.LicensePlate,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Type = vehicle.Type
                };

                var items = vehicle.Inspections
                    .OrderByDescending(inspection => inspection.Date)
                    .Select(inspection => new InspectionListItem(inspection))
                    .ToList();
                Inspections = new ObservableCollection<InspectionListItem>(items);

                // Cover photos come in one by one after the list is shown
                _ = Task.WhenAll(items.Select(LoadCoverPhotoAsync));
            }
            catch (ApiException exception)
            {
                if (version == _loadVersion)
                    ErrorMessage = exception.Message;
            }
            finally
            {
                if (version == _loadVersion)
                {
                    IsBusy = false;
                    IsRefreshing = false;
                    HasLoaded = true;
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowFirstLoad));
                }
            }
        }

        private async Task LoadCoverPhotoAsync(InspectionListItem item)
        {
            var photo = await _photoLoaderService.LoadAsync(item.Inspection.PhotoUrl);
            MainThread.BeginInvokeOnMainThread(() => item.CoverPhoto = photo);
        }
    }
}
