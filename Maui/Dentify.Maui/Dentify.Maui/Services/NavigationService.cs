namespace Dentify.Maui.Services
{
    // The only class that touches Shell, so view models stay free of it
    public class NavigationService : INavigationService
    {
        public Task NavigateToAsync(string route) => Shell.Current.GoToAsync(route);

        public Task GoBackAsync() => Shell.Current.GoToAsync("..");

        public Task GoToRootAsync() => Shell.Current.Navigation.PopToRootAsync();
    }
}
