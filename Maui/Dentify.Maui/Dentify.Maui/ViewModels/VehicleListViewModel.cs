using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dentify.Contracts.Models;
using Dentify.Maui.Messages;
using Dentify.Maui.Services;
using Dentify.Maui.Views;

namespace Dentify.Maui.ViewModels
{
    public partial class VehicleListViewModel : BaseViewModel, IRecipient<ApiSettingsChangedMessage>
    {
        private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(350);

        private IApiService _apiService;
        private INavigationService _navigationService;
        private CancellationTokenSource? _searchCancellation;
        private int _loadVersion;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
        public partial ObservableCollection<VehicleListDto> Vehicles { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyStateTitle))]
        public partial string SearchQuery { get; set; }

        [ObservableProperty]
        public partial bool IsRefreshing { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(ShowFirstLoad))]
        public partial bool HasLoaded { get; set; }

        public bool ShowFirstLoad => !HasLoaded;
        public bool ShowEmptyState => HasLoaded && !HasError && Vehicles.Count == 0;

        public string EmptyStateTitle => string.IsNullOrWhiteSpace(SearchQuery)
            ? "No vehicles yet"
            : $"No vehicle matches \"{SearchQuery.Trim()}\"";

        public VehicleListViewModel(IApiService apiService, INavigationService navigationService)
        {
            _apiService = apiService;
            _navigationService = navigationService;
            Vehicles = new ObservableCollection<VehicleListDto>();

            WeakReferenceMessenger.Default.Register<ApiSettingsChangedMessage>(this);

            // The page has no code-behind to trigger a load, so the first load starts here
            _ = LoadVehiclesAsync();
        }

        public void Receive(ApiSettingsChangedMessage message)
        {
            HasLoaded = false;
            _ = LoadVehiclesAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadVehiclesAsync();
        }

        [RelayCommand]
        private async Task SelectVehicleAsync(VehicleListDto vehicle)
        {
            if (vehicle == null) return;

            // The detail view model exists from startup, so it has the vehicle before the page appears
            WeakReferenceMessenger.Default.Send(new VehicleSelectedMessage(vehicle));
            await _navigationService.NavigateToAsync(nameof(VehicleDetailPage));
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            await _navigationService.NavigateToAsync(nameof(SettingsPage));
        }

        // Search as the user types, but wait until they pause
        partial void OnSearchQueryChanged(string value)
        {
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            _ = SearchAfterDelayAsync(_searchCancellation.Token);
        }

        private async Task SearchAfterDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(SearchDelay, cancellationToken);
                await LoadVehiclesAsync();
            }
            catch (TaskCanceledException)
            {
                // A newer search replaced this one
            }
        }

        private async Task LoadVehiclesAsync()
        {
            // Only the newest request may update the list, an older search can finish later
            var version = ++_loadVersion;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var vehicles = await _apiService.GetVehiclesAsync(SearchQuery);
                if (version == _loadVersion)
                    Vehicles = new ObservableCollection<VehicleListDto>(vehicles);
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
                }
            }
        }
    }
}
