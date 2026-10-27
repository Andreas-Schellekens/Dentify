using CommunityToolkit.Mvvm.Messaging.Messages;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Messages
{
    // Sent when a scanned plate has no matching vehicle, so the vehicle list can search for it
    public class PlateScannedMessage : ValueChangedMessage<PlateScanResultDto>
    {
        public PlateScannedMessage(PlateScanResultDto result) : base(result)
        {
        }
    }
}
