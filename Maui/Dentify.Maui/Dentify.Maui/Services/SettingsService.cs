namespace Dentify.Maui.Services
{
    // Stores the API base URL in Preferences, so a new Ngrok URL needs no rebuild
    public class SettingsService : ISettingsService
    {
        private const string ApiBaseUrlKey = "ApiBaseUrl";
        private const string InspectorNameKey = "InspectorName";

        // The Android emulator reaches the host PC at 10.0.2.2, other platforms use localhost
        public string DefaultApiBaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual
                ? "http://10.0.2.2:5152"
                : "http://localhost:5152";

        public string ApiBaseUrl
        {
            get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
            set => Preferences.Default.Set(ApiBaseUrlKey, value.Trim().TrimEnd('/'));
        }

        public string InspectorName
        {
            get => Preferences.Default.Get(InspectorNameKey, string.Empty);
            set => Preferences.Default.Set(InspectorNameKey, value.Trim());
        }
    }
}
