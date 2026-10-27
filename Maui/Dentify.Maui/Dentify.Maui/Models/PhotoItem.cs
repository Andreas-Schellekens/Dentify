using CommunityToolkit.Mvvm.ComponentModel;

namespace Dentify.Maui.Models
{
    // A stored inspection photo, the image is loaded after the page is shown
    public partial class PhotoItem : ObservableObject
    {
        public string Url { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoaded))]
        public partial ImageSource? Image { get; set; }

        public bool IsLoaded => Image != null;

        public PhotoItem(string url)
        {
            Url = url;
        }
    }
}
