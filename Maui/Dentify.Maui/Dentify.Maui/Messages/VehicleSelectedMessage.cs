using CommunityToolkit.Mvvm.Messaging.Messages;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Messages
{
    // Sent when the user picks a vehicle (from the list or a plate scan), the detail page shows it
    public class VehicleSelectedMessage : ValueChangedMessage<VehicleListDto>
    {
        public VehicleSelectedMessage(VehicleListDto vehicle) : base(vehicle)
        {
        }
    }
}
