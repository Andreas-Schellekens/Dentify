using CommunityToolkit.Mvvm.ComponentModel;

namespace Dentify.Maui.Models
{
    // A stored inspection photo, the images are loaded after the page is shown.
    // Shows either the boxes of the trained model or the result of the AI review.
    public partial class PhotoItem : ObservableObject
    {
        private ImageSource? _modelImage;
        private ImageSource? _reviewImage;

        // Photos without boxes (or from before the damage boxes) have no result copies: then the plain photo is used
        public string ModelUrl { get; }
        public string ReviewUrl { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoaded))]
        public partial ImageSource? Image { get; set; }

        public bool IsLoaded => Image != null;

        public PhotoItem(string url, string? modelResultUrl, string? reviewResultUrl)
        {
            ModelUrl = modelResultUrl ?? url;
            ReviewUrl = reviewResultUrl ?? url;
        }

        public void SetImages(ImageSource? modelImage, ImageSource? reviewImage, bool showReview)
        {
            _modelImage = modelImage;
            _reviewImage = reviewImage;
            Show(showReview);
        }

        public void Show(bool showReview)
        {
            Image = showReview ? _reviewImage : _modelImage;
        }
    }
}
