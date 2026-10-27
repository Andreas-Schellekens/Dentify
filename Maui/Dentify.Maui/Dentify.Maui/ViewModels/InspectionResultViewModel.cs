using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dentify.Contracts.Models;
using Dentify.Maui.Messages;
using Dentify.Maui.Models;
using Dentify.Maui.Services;

namespace Dentify.Maui.ViewModels
{
    public partial class InspectionResultViewModel : BaseViewModel,
        IRecipient<InspectionSavedMessage>, IRecipient<InspectionSelectedMessage>
    {
        private IApiService _apiService;
        private IPhotoLoaderService _photoLoaderService;
        private INavigationService _navigationService;
        private int? _inspectionId;
        private int _loadVersion;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasInspection), nameof(ShowLoading), nameof(Status), nameof(StatusTitle),
            nameof(StatusText), nameof(TypeText), nameof(HasSummary))]
        public partial InspectionDetailDto? Inspection { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPhotos))]
        public partial ObservableCollection<PhotoItem> Photos { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDamages))]
        public partial ObservableCollection<DamageListDto> Damages { get; set; }

        [ObservableProperty]
        public partial bool IsJustSaved { get; set; }

        public bool HasInspection => Inspection != null;
        public bool ShowLoading => Inspection == null && !HasError;
        public bool HasPhotos => Photos.Count > 0;
        public bool HasDamages => Damages.Count > 0;
        public bool HasSummary => !string.IsNullOrWhiteSpace(Inspection?.AiSummary);
        public string TypeText => Inspection?.Type == "Return" ? "Return" : "Departure";

        // "NoPhotos", "NoDamage", "Minor", "Moderate" or "Severe", the page picks the colors
        public string Status
        {
            get
            {
                if (Inspection == null || Inspection.Photos.Count == 0) return "NoPhotos";
                if (Inspection.Damages.Count == 0) return "NoDamage";
                return Inspection.Damages.MaxBy(damage => SeverityRank(damage.Severity))?.Severity ?? "Minor";
            }
        }

        public string StatusTitle => Status switch
        {
            "NoPhotos" => "No photos yet",
            "NoDamage" => "No damage found",
            _ => Inspection!.Damages.Count == 1 ? "1 damage found" : $"{Inspection!.Damages.Count} damages found"
        };

        public string StatusText => Status switch
        {
            "NoPhotos" => "This inspection has no photos, so nothing was checked.",
            "NoDamage" => "The AI model found no damage on the photos.",
            "Severe" => "At least one severe damage. Check the vehicle before it leaves again.",
            "Moderate" => "Worst damage is moderate.",
            _ => "Only minor damage."
        };

        public InspectionResultViewModel(IApiService apiService, IPhotoLoaderService photoLoaderService,
            INavigationService navigationService)
        {
            _apiService = apiService;
            _photoLoaderService = photoLoaderService;
            _navigationService = navigationService;
            Photos = new ObservableCollection<PhotoItem>();
            Damages = new ObservableCollection<DamageListDto>();

            WeakReferenceMessenger.Default.Register<InspectionSavedMessage>(this);
            WeakReferenceMessenger.Default.Register<InspectionSelectedMessage>(this);
        }

        // A new inspection: the upload already returned everything, no extra call needed
        public void Receive(InspectionSavedMessage message)
        {
            ++_loadVersion;
            _inspectionId = message.Value.Id;
            IsJustSaved = true;
            ErrorMessage = null;
            Show(message.Value);
        }

        // An inspection from the history: only the id is known
        public void Receive(InspectionSelectedMessage message)
        {
            _inspectionId = message.Value;
            IsJustSaved = false;
            Clear();
            _ = LoadInspectionAsync();
        }

        [RelayCommand]
        private async Task RetryAsync()
        {
            await LoadInspectionAsync();
        }

        [RelayCommand]
        private async Task DoneAsync()
        {
            await _navigationService.GoBackAsync();
        }

        private async Task LoadInspectionAsync()
        {
            if (_inspectionId == null) return;

            var version = ++_loadVersion;
            IsBusy = true;
            ErrorMessage = null;
            OnPropertyChanged(nameof(ShowLoading));

            try
            {
                var inspection = await _apiService.GetInspectionAsync(_inspectionId.Value);
                if (version == _loadVersion)
                    Show(inspection);
            }
            catch (ApiException exception)
            {
                if (version == _loadVersion)
                    ErrorMessage = exception.Message;
            }
            finally
            {
                if (version == _loadVersion)
                {
                    IsBusy = false;
                    OnPropertyChanged(nameof(ShowLoading));
                }
            }
        }

        private void Clear()
        {
            Inspection = null;
            Photos = new ObservableCollection<PhotoItem>();
            Damages = new ObservableCollection<DamageListDto>();
            ErrorMessage = null;
        }

        private void Show(InspectionDetailDto inspection)
        {
            Inspection = inspection;
            Damages = new ObservableCollection<DamageListDto>(inspection.Damages
                .OrderByDescending(damage => SeverityRank(damage.Severity))
                .ThenByDescending(damage => damage.Confidence));

            var photos = inspection.Photos
                .OrderBy(photo => photo.UploadedAt)
                .Select(photo => new PhotoItem(photo.Url))
                .ToList();
            Photos = new ObservableCollection<PhotoItem>(photos);
            OnPropertyChanged(nameof(ShowLoading));

            _ = Task.WhenAll(photos.Select(LoadPhotoAsync));
        }

        private async Task LoadPhotoAsync(PhotoItem photo)
        {
            var image = await _photoLoaderService.LoadAsync(photo.Url);
            MainThread.BeginInvokeOnMainThread(() => photo.Image = image);
        }

        private static int SeverityRank(string severity) => severity switch
        {
            "Severe" => 3,
            "Moderate" => 2,
            _ => 1
        };
    }
}
