using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dentify.Contracts.Models;

namespace Dentify.Maui.Services
{
    // Typed HttpClient and the only class that talks to the Dentify API
    public class ApiService : IApiService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private HttpClient _httpClient;
        private ISettingsService _settingsService;

        public ApiService(HttpClient httpClient, ISettingsService settingsService)
        {
            _httpClient = httpClient;
            _settingsService = settingsService;
        }

        public async Task<IEnumerable<VehicleListDto>> GetVehiclesAsync(string? searchQuery = null)
        {
            var path = string.IsNullOrWhiteSpace(searchQuery)
                ? "api/vehicles"
                : $"api/vehicles?searchQuery={Uri.EscapeDataString(searchQuery.Trim())}";
            return await GetAsync<List<VehicleListDto>>(path);
        }

        public async Task<VehicleDetailDto> GetVehicleAsync(int id) =>
            await GetAsync<VehicleDetailDto>($"api/vehicles/{id}");

        public async Task<IEnumerable<InspectionListDto>> GetInspectionsForVehicleAsync(int vehicleId) =>
            await GetAsync<List<InspectionListDto>>($"api/vehicles/{vehicleId}/inspections");

        public async Task<InspectionDetailDto> GetInspectionAsync(int id) =>
            await GetAsync<InspectionDetailDto>($"api/inspections/{id}");

        public async Task<PlateScanResultDto> ScanPlateAsync(FileResult photo)
        {
            using var content = new MultipartFormDataContent();
            await AddPhotoAsync(content, "photo", photo);
            return await SendAsync<PlateScanResultDto>(HttpMethod.Post, "api/plates/scan", content);
        }

        public async Task<InspectionDetailDto> CreateInspectionAsync(InspectionForCreationDto inspection)
        {
            using var content = JsonContent.Create(inspection, options: JsonOptions);
            return await SendAsync<InspectionDetailDto>(HttpMethod.Post, "api/inspections", content);
        }

        public async Task<InspectionDetailDto> UploadPhotosAsync(int inspectionId, IEnumerable<FileResult> photos)
        {
            using var content = new MultipartFormDataContent();
            foreach (var photo in photos)
                await AddPhotoAsync(content, "photos", photo);

            return await SendAsync<InspectionDetailDto>(HttpMethod.Post, $"api/inspections/{inspectionId}/photos", content);
        }

        public async Task<byte[]> GetPhotoAsync(string relativeUrl)
        {
            using var response = await SendRequestAsync(HttpMethod.Get, BuildUri(relativeUrl.TrimStart('/')));
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<bool> CanConnectAsync(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
                return false;

            try
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var response = await _httpClient.GetAsync(new Uri(baseUri, "api/vehicles"), cancellation.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return false;
            }
        }

        private async Task<T> GetAsync<T>(string path) =>
            await SendAsync<T>(HttpMethod.Get, path, null);

        private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent? content)
        {
            using var response = await SendRequestAsync(method, BuildUri(path), content);
            try
            {
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
                    ?? throw new ApiException("The API returned an empty response.", response.StatusCode);
            }
            catch (JsonException exception)
            {
                // Usually the Ngrok warning or error page instead of JSON
                throw new ApiException("The API returned something unexpected. Check the API URL in Settings.",
                    response.StatusCode, exception);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, Uri uri, HttpContent? content = null)
        {
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(method, uri) { Content = content };
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException exception)
            {
                throw new ApiException(
                    $"Cannot reach the API at {_settingsService.ApiBaseUrl}. Check that the API and Ngrok are running and that the URL in Settings is correct.",
                    null, exception);
            }
            catch (TaskCanceledException exception)
            {
                throw new ApiException("The API took too long to respond. Try again.", null, exception);
            }

            if (response.IsSuccessStatusCode)
                return response;

            var message = await ReadErrorMessageAsync(response);
            response.Dispose();
            throw new ApiException(message, response.StatusCode);
        }

        private Uri BuildUri(string path)
        {
            if (!Uri.TryCreate(_settingsService.ApiBaseUrl + "/", UriKind.Absolute, out var baseUri))
                throw new ApiException("The API URL in Settings is not valid.");

            return new Uri(baseUri, path);
        }

        private static async Task AddPhotoAsync(MultipartFormDataContent content, string fieldName, FileResult photo)
        {
            // The API checks the extension, so make sure the file name has one
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var fileName = string.IsNullOrEmpty(extension) ? photo.FileName + ".jpg" : photo.FileName;

            var stream = await photo.OpenReadAsync();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(extension == ".png" ? "image/png" : "image/jpeg");
            content.Add(streamContent, fieldName, fileName);
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (mediaType.Contains("html"))
                return "The Ngrok tunnel is offline or the API URL in Settings is wrong.";

            if (mediaType.Contains("json") && TryReadProblemDetails(body, out var problem))
                return problem;

            if (!string.IsNullOrWhiteSpace(body) && body.Length < 500)
                return body.Trim('"');

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Not found.",
                HttpStatusCode.BadRequest => "The request was not valid.",
                HttpStatusCode.BadGateway => "An Azure AI service is not available right now. Try again later.",
                _ => $"The API returned an error ({(int)response.StatusCode})."
            };
        }

        // Validation errors come back as ProblemDetails with an "errors" object
        private static bool TryReadProblemDetails(string body, out string message)
        {
            message = string.Empty;
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(error => error.Value.EnumerateArray().Select(value => value.GetString()))
                        .Where(value => !string.IsNullOrWhiteSpace(value));
                    message = string.Join(Environment.NewLine, messages);
                }

                if (string.IsNullOrWhiteSpace(message) && root.TryGetProperty("title", out var title))
                    message = title.GetString() ?? string.Empty;

                return !string.IsNullOrWhiteSpace(message);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
