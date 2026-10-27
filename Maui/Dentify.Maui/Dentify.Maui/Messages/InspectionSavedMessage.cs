using CommunityToolkit.Mvvm.Messaging.Messages;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Messages
{
    // Sent after a new inspection and its photos are saved: the result page shows it, the history refreshes
    public class InspectionSavedMessage : ValueChangedMessage<InspectionDetailDto>
    {
        public InspectionSavedMessage(InspectionDetailDto inspection) : base(inspection)
        {
        }
    }
}
