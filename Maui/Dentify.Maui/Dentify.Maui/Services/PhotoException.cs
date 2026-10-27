namespace Dentify.Maui.Services
{
    // Thrown by the PhotoService with a message that can be shown to the user as is
    public class PhotoException : Exception
    {
        public PhotoException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
