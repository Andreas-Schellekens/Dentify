using Dentify.Maui.ViewModels;

namespace Dentify.Maui.Views
{
    public partial class PlateScanPage : ContentPage
    {
        public PlateScanPage(PlateScanViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
