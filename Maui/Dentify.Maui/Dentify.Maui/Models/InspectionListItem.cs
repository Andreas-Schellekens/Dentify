using CommunityToolkit.Mvvm.ComponentModel;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Models
{
    // An inspection in the history list, with its cover photo loaded separately
    public partial class InspectionListItem : ObservableObject
    {
        public InspectionListDto Inspection { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCoverPhoto))]
        public partial ImageSource? CoverPhoto { get; set; }

        public bool HasCoverPhoto => CoverPhoto != null;

        // "NoPhotos", "NoDamage" or "Damage", the page picks the colors
        public string Status => Inspection.PhotoUrl == null
            ? "NoPhotos"
            : Inspection.DamageCount > 0 ? "Damage" : "NoDamage";

        public string StatusText => Status switch
        {
            "NoPhotos" => "No photos",
            "NoDamage" => "No damage",
            _ => Inspection.DamageCount == 1 ? "1 damage" : $"{Inspection.DamageCount} damages"
        };

        public string TypeText => Inspection.Type == "Return" ? "Return" : "Departure";

        public InspectionListItem(InspectionListDto inspection)
        {
            Inspection = inspection;
        }
    }
}
