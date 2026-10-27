using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Dentify.Maui.Messages
{
    // Sent when the user opens an inspection from the history, the value is the inspection id
    public class InspectionSelectedMessage : ValueChangedMessage<int>
    {
        public InspectionSelectedMessage(int inspectionId) : base(inspectionId)
        {
        }
    }
}
