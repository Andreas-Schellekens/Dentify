using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class VehicleListPage : ContentPage
    {
        public VehicleListPage(VehicleListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
