namespace Dentify.Maui.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);
        Task GoBackAsync();
        Task GoToRootAsync();
    }
}
