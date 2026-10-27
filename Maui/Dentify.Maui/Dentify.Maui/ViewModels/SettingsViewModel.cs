using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dentify.Maui.Messages;
using Dentify.Maui.Services;

namespace Dentify.Maui.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private IApiService _apiService;
        private ISettingsService _settingsService;
        private INavigationService _navigationService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSuccess))]
        public partial string ApiBaseUrl { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSuccess))]
        public partial string? SuccessMessage { get; set; }

        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public string DefaultApiBaseUrl => _settingsService.DefaultApiBaseUrl;

        public SettingsViewModel(IApiService apiService, ISettingsService settingsService,
            INavigationService navigationService)
        {
            _apiService = apiService;
            _settingsService = settingsService;
            _navigationService = navigationService;
            ApiBaseUrl = _settingsService.ApiBaseUrl;
        }

        partial void OnApiBaseUrlChanged(string value)
        {
            SuccessMessage = null;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (!TryGetValidUrl(out var url)) return;

            IsBusy = true;
            try
            {
                if (await _apiService.CanConnectAsync(url))
                    SuccessMessage = "Connected. The API answers on this URL.";
                else
                    ErrorMessage = "No answer from the API on this URL. Is the API running, and is the Ngrok URL still current?";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!TryGetValidUrl(out var url)) return;

            _settingsService.ApiBaseUrl = url;
            WeakReferenceMessenger.Default.Send(new ApiSettingsChangedMessage(_settingsService.ApiBaseUrl));
            await _navigationService.GoBackAsync();
        }

        [RelayCommand]
        private void ResetToDefault()
        {
            ApiBaseUrl = _settingsService.DefaultApiBaseUrl;
        }

        private bool TryGetValidUrl(out string url)
        {
            url = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            var isValid = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!isValid)
                ErrorMessage = "Enter a full URL, e.g. https://abcd-1234.ngrok-free.app";

            return isValid;
        }
    }
}
