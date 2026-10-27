using System.Net;

namespace Dentify.Maui.Services
{
    // Thrown by the ApiService with a message that can be shown to the user as is
    public class ApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public ApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
