using System.ClientModel;
using System.Text;
using Dentify.Contracts.Models;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // The Responses API is still marked experimental in the OpenAI SDK

namespace Dentify.Api.Strategies
{
    // Writes the damage report with an Azure OpenAI deployment (Microsoft Foundry, v1 API, Responses API)
    public class AzureOpenAIReportService : IReportService
    {
        private const int MaxSummaryLength = 2000; // Inspection.AiSummary column size
        private const int MaxOutputTokens = 400;
        private const double LowConfidence = 0.6;

        private const string Instructions =
            "You write short damage reports for a vehicle rental company. " +
            "You receive the damages that an image recognition model detected on the photos of one inspection, " +
            "and sometimes the damages of the previous inspection of the same vehicle. " +
            "Write 2 to 4 plain English sentences for the employee who hands over or takes back the vehicle. " +
            "Mention the most severe damage first and where it is on the photo. " +
            "Each current damage is labeled NEW or ALSO AT PREVIOUS INSPECTION when there is a previous inspection. " +
            "Follow these labels exactly, never decide this yourself. Start with the NEW damages, " +
            "and say they are likely new: the labels compare photo areas, not exact positions on the car. " +
            "Only damages marked LOW CONFIDENCE need to be checked by eye; do not call other damages uncertain. " +
            "Only use the data you receive, never invent damages, costs or causes. " +
            "No headings, no lists, no markdown.";

        private ResponsesClient _client;
        private string _deploymentName;
        private FakeReportService _fallback;
        private ILogger<AzureOpenAIReportService> _logger;

        public AzureOpenAIReportService(IConfiguration configuration, ILogger<AzureOpenAIReportService> logger)
        {
            _client = new ResponsesClient(
                new ApiKeyCredential(configuration["Azure:OpenAI:Key"]),
                new ResponsesClientOptions
                {
                    Endpoint = new Uri(configuration["Azure:OpenAI:Endpoint"])
                });
            _deploymentName = configuration["Azure:OpenAI:DeploymentName"];
            _fallback = new FakeReportService();
            _logger = logger;
        }

        public async Task<string> WriteSummaryAsync(InspectionDetailDto inspection, InspectionDetailDto? previousInspection)
        {
            var options = new CreateResponseOptions
            {
                // On Azure the model is the deployment name
                Model = _deploymentName,
                Instructions = Instructions,
                MaxOutputTokenCount = MaxOutputTokens,
                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(DescribeInspections(inspection, previousInspection))
                }
            };

            try
            {
                ResponseResult response = await _client.CreateResponseAsync(options);
                var summary = response.GetOutputText()?.Trim();

                if (!string.IsNullOrEmpty(summary))
                    return summary.Length <= MaxSummaryLength ? summary : summary[..MaxSummaryLength];

                _logger.LogWarning("Azure OpenAI returned an empty report for inspection {InspectionId}", inspection.Id);
            }
            catch (Exception exception) when (exception is ClientResultException or HttpRequestException or TaskCanceledException)
            {
                // The photos and damages are already saved, so a failing report must not fail the upload
                _logger.LogWarning(exception, "Azure OpenAI failed for inspection {InspectionId}, using the template report", inspection.Id);
            }

            return await _fallback.WriteSummaryAsync(inspection, previousInspection);
        }

        private static string DescribeInspections(InspectionDetailDto inspection, InspectionDetailDto? previousInspection)
        {
            var text = new StringBuilder();
            AppendInspection(text, "Current inspection", inspection, previousInspection);

            text.AppendLine();
            if (previousInspection != null)
                AppendInspection(text, "Previous inspection", previousInspection, null);
            else
                text.AppendLine("There is no previous inspection of this vehicle.");

            return text.ToString();
        }

        // previousInspection is only passed for the current inspection, to label each damage as new or not
        private static void AppendInspection(StringBuilder text, string title, InspectionDetailDto inspection,
            InspectionDetailDto? previousInspection)
        {
            text.AppendLine($"{title}: {inspection.Type} inspection of vehicle {inspection.VehicleLicensePlate} " +
                            $"on {inspection.Date:yyyy-MM-dd HH:mm} by {inspection.InspectorName}, {inspection.Photos.Count} photo(s).");

            if (inspection.Damages.Count == 0)
            {
                text.AppendLine("Detected damages: none.");
                return;
            }

            text.AppendLine("Detected damages:");
            foreach (var damage in inspection.Damages.OrderByDescending(d => d.Confidence))
            {
                var line = $"- {damage.Description}, severity {damage.Severity}, photo area {damage.Location}, confidence {damage.Confidence:P0}";

                if (damage.Confidence < LowConfidence)
                    line += ", LOW CONFIDENCE";

                if (previousInspection != null)
                    line += IsAtPreviousInspection(damage, previousInspection) ? ", ALSO AT PREVIOUS INSPECTION" : ", NEW";

                text.AppendLine(line);
            }
        }

        // Same damage type in the same photo area. The photos can be taken from other angles, so this is an estimate.
        private static bool IsAtPreviousInspection(DamageListDto damage, InspectionDetailDto previousInspection) =>
            previousInspection.Damages.Any(previous =>
                string.Equals(previous.Description, damage.Description, StringComparison.OrdinalIgnoreCase)
                && string.Equals(previous.Location, damage.Location, StringComparison.OrdinalIgnoreCase));
    }
}
