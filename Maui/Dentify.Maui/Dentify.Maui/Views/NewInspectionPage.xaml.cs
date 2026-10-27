using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class NewInspectionPage : ContentPage
    {
        public NewInspectionPage(NewInspectionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
