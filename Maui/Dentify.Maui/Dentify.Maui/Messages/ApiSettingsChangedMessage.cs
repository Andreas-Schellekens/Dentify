using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Dentify.Maui.Messages
{
    // Sent by the settings page when the API base URL changed, so pages can reload their data
    public class ApiSettingsChangedMessage : ValueChangedMessage<string>
    {
        public ApiSettingsChangedMessage(string apiBaseUrl) : base(apiBaseUrl)
        {
        }
    }
}
