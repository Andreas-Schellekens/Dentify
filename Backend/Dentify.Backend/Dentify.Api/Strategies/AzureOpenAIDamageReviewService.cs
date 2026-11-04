using System.ClientModel;
using System.Text;
using System.Text.Json;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // The Responses API is still marked experimental in the OpenAI SDK

namespace Dentify.Api.Strategies
{
    // Shows the photo and the damages proposed by Custom Vision to an Azure OpenAI vision model,
    // which confirms or rejects each one. Removes false alarms such as reflections seen as broken glass.
    public class AzureOpenAIDamageReviewService : IDamageReviewService
    {
        private const int MaxCandidates = 10;
        private const int MaxOutputTokens = 100;

        private const string Instructions =
            "You check the results of an image recognition model that looks for damage on vehicle photos. " +
            "You receive one photo and a numbered list of damages the model proposes, each with the area of the photo " +
            "where it was found. For each number, decide whether that damage is really visible on the vehicle in that area. " +
            "Reflections, dirt, shadows, stickers, background objects and normal car parts are not damage. " +
            "A tire only counts as flat when it is visibly deflated. When in doubt, reject. " +
            "Return the numbers of the confirmed damages only.";

        // Strict JSON schema, so the answer can always be parsed
        private static readonly BinaryData ResponseSchema = BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "confirmed": { "type": "array", "items": { "type": "integer" } }
              },
              "required": ["confirmed"],
              "additionalProperties": false
            }
            """);

        private ResponsesClient _client;
        private string _deploymentName;
        private FakeDamageReviewService _fallback;
        private ILogger<AzureOpenAIDamageReviewService> _logger;

        public AzureOpenAIDamageReviewService(IConfiguration configuration, ILogger<AzureOpenAIDamageReviewService> logger)
        {
            _client = new ResponsesClient(
                new ApiKeyCredential(configuration["Azure:OpenAI:Key"]),
                new ResponsesClientOptions
                {
                    Endpoint = new Uri(configuration["Azure:OpenAI:Endpoint"])
                });
            _deploymentName = configuration["Azure:OpenAI:DeploymentName"];
            _fallback = new FakeDamageReviewService(configuration);
            _logger = logger;
        }

        public async Task<IEnumerable<DamagePrediction>> ReviewDamagesAsync(byte[] imageData, IEnumerable<DamagePrediction> candidates)
        {
            var proposed = candidates
                .OrderByDescending(c => c.Confidence)
                .Take(MaxCandidates)
                .ToList();

            if (proposed.Count == 0)
                return proposed;

            var options = new CreateResponseOptions
            {
                // On Azure the model is the deployment name
                Model = _deploymentName,
                Instructions = Instructions,
                MaxOutputTokenCount = MaxOutputTokens,
                TextOptions = new ResponseTextOptions
                {
                    TextFormat = ResponseTextFormat.CreateJsonSchemaFormat("damage_review", ResponseSchema, jsonSchemaIsStrict: true)
                },
                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(new[]
                    {
                        ResponseContentPart.CreateInputTextPart(DescribeCandidates(proposed)),
                        // The photo is always a JPEG after IImageResizeService; the SDK reads the media type from BinaryData
                        ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(imageData, "image/jpeg"))
                    })
                }
            };

            try
            {
                ResponseResult response = await _client.CreateResponseAsync(options);
                var confirmed = ReadConfirmedNumbers(response.GetOutputText());

                var kept = proposed.Where((_, index) => confirmed.Contains(index + 1)).ToList();

                _logger.LogInformation("Damage review kept {Kept} and rejected {Rejected}",
                    Describe(kept), Describe(proposed.Except(kept)));

                return kept;
            }
            catch (Exception exception) when (exception is ClientResultException or HttpRequestException
                                                  or TaskCanceledException or JsonException)
            {
                // Without a review, only keep the damages the trained model is sure about
                _logger.LogWarning(exception, "Damage review failed, using the confidence threshold instead");
                return await _fallback.ReviewDamagesAsync(imageData, proposed);
            }
        }

        private static string DescribeCandidates(List<DamagePrediction> candidates)
        {
            var text = new StringBuilder("Proposed damages:");
            for (var i = 0; i < candidates.Count; i++)
                text.Append($"\n{i + 1}. {candidates[i].Description} in the {candidates[i].Location.ToLowerInvariant()} " +
                            $"of the photo ({candidates[i].Confidence:P0})");

            return text.ToString();
        }

        private static HashSet<int> ReadConfirmedNumbers(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.GetProperty("confirmed")
                .EnumerateArray()
                .Select(number => number.GetInt32())
                .ToHashSet();
        }

        private static string Describe(IEnumerable<DamagePrediction> damages)
        {
            var list = string.Join(", ", damages.Select(d => $"{d.Description} {d.Location} {d.Confidence:P0}"));
            return list.Length == 0 ? "nothing" : list;
        }
    }
}
