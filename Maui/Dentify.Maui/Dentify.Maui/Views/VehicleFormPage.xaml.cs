using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class VehicleFormPage : ContentPage
    {
        public VehicleFormPage(VehicleFormViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
