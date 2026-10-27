using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class VehicleDetailPage : ContentPage
    {
        public VehicleDetailPage(VehicleDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
