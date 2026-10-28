using CommunityToolkit.Mvvm.Messaging.Messages;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Messages
{
    // Sent before opening the vehicle form. Id 0 means a new vehicle (the plate may already be filled in),
    // any other id edits that vehicle.
    public class VehicleFormRequestedMessage : ValueChangedMessage<VehicleListDto>
    {
        public VehicleFormRequestedMessage(VehicleListDto vehicle) : base(vehicle)
        {
        }
    }
}
