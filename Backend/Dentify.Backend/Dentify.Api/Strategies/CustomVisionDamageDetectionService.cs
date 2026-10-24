using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Dentify.Api.Strategies
{
    // Calls the published Custom Vision model through the Prediction REST API v3.1.
    // Works with an object detection project (with locations) and a classification project (whole photo).
    public class CustomVisionDamageDetectionService : IDamageDetectionService
    {
        private HttpClient _httpClient;
        private ILogger<CustomVisionDamageDetectionService> _logger;
        private string _projectId;
        private string _publishedName;
        private bool _isObjectDetection;
        private double _minimumProbability;

        public CustomVisionDamageDetectionService(HttpClient httpClient, IConfiguration configuration,
            ILogger<CustomVisionDamageDetectionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(configuration["Azure:CustomVision:PredictionEndpoint"].TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Add("Prediction-Key", configuration["Azure:CustomVision:PredictionKey"]);

            _projectId = configuration["Azure:CustomVision:ProjectId"];
            _publishedName = configuration["Azure:CustomVision:PublishedName"];
            _isObjectDetection = !string.Equals(configuration["Azure:CustomVision:ProjectType"], "Classification",
                StringComparison.OrdinalIgnoreCase);
            _minimumProbability = configuration.GetValue("Azure:CustomVision:MinimumProbability", 0.5);
        }

        public async Task<IEnumerable<DamagePrediction>> DetectDamagesAsync(byte[] imageData)
        {
            var operation = _isObjectDetection ? "detect" : "classify";
            var url = $"customvision/v3.1/prediction/{_projectId}/{operation}/iterations/{_publishedName}/image/nostore";

            using var content = new ByteArrayContent(imageData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Custom Vision prediction failed ({(int)response.StatusCode}): {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<ImagePredictionResponse>();
            var predictions = result?.Predictions ?? new List<PredictionResponse>();

            _logger.LogInformation("Custom Vision returned {Predictions}",
                string.Join(", ", predictions.Select(p => $"{p.TagName} {p.Probability:P0}")));

            return predictions
                .Where(p => p.Probability >= _minimumProbability)
                .OrderByDescending(p => p.Probability)
                .Select(p => new DamagePrediction(
                    ToDescription(p.TagName),
                    ToSeverity(p.TagName),
                    p.BoundingBox == null ? "Whole photo" : ToLocation(p.BoundingBox),
                    Math.Round(p.Probability, 4)))
                .ToList();
        }

        private static string ToDescription(string tagName) => tagName switch
        {
            "scratch" => "Scratch",
            "dent" => "Dent",
            "crack" => "Crack",
            "broken-glass" => "Broken glass",
            "broken-lamp" => "Broken lamp",
            "flat-tire" => "Flat tire",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tagName.Replace('-', ' '))
        };

        private static string ToSeverity(string tagName) => tagName switch
        {
            "scratch" => "Minor",
            "dent" or "crack" => "Moderate",
            "broken-glass" or "broken-lamp" or "flat-tire" => "Severe",
            _ => "Moderate"
        };

        // Turns the centre of the bounding box (fractions 0..1) into an area of the photo, e.g. "Bottom left"
        private static string ToLocation(BoundingBoxResponse box)
        {
            var centerX = box.Left + box.Width / 2;
            var centerY = box.Top + box.Height / 2;

            var vertical = centerY < 1.0 / 3 ? "Top" : centerY < 2.0 / 3 ? "Middle" : "Bottom";
            var horizontal = centerX < 1.0 / 3 ? "left" : centerX < 2.0 / 3 ? "center" : "right";

            return vertical == "Middle" && horizontal == "center" ? "Center" : $"{vertical} {horizontal}";
        }

        // Response shapes of the Prediction API (only the fields we use)
        private class ImagePredictionResponse
        {
            public List<PredictionResponse> Predictions { get; set; }
        }

        private class PredictionResponse
        {
            public double Probability { get; set; }
            public string TagName { get; set; }
            public BoundingBoxResponse? BoundingBox { get; set; }
        }

        private class BoundingBoxResponse
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }
}
