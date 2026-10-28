using CommunityToolkit.Mvvm.Messaging.Messages;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Messages
{
    // Sent after a vehicle is added or changed, so the vehicle list reloads
    public class VehicleSavedMessage : ValueChangedMessage<VehicleListDto>
    {
        public VehicleSavedMessage(VehicleListDto vehicle) : base(vehicle)
        {
        }
    }
}
