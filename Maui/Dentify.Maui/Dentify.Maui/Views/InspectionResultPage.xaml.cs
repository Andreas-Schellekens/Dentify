using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class InspectionResultPage : ContentPage
    {
        public InspectionResultPage(InspectionResultViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
