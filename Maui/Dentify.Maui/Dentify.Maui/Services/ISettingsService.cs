namespace Dentify.Maui.Services
{
    public interface ISettingsService
    {
        // Base URL of the Dentify API, e.g. "https://abcd-1234.ngrok-free.app"
        string ApiBaseUrl { get; set; }

        string DefaultApiBaseUrl { get; }

        // Name of the last inspector, so it does not have to be typed for every inspection
        string InspectorName { get; set; }
    }
}
