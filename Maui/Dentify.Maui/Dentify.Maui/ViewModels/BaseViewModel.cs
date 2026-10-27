using CommunityToolkit.Mvvm.ComponentModel;

namespace Dentify.Maui.ViewModels
{
    // Loading and error state shared by every page
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        public partial string? ErrorMessage { get; set; }

        public bool IsNotBusy => !IsBusy;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }
}
