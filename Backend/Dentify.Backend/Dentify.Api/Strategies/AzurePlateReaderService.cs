using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace Dentify.Api.Strategies
{
    // Reads Belgian license plates with the OCR (Read) feature of Azure AI Vision Image Analysis 4.0
    public class AzurePlateReaderService : IPlateReaderService
    {
        // Current format 1-ABC-123, older formats ABC-123 and 123-ABC (without separators here)
        private static readonly Regex CurrentPlate = new("^[1-9][A-Z]{3}[0-9]{3}$");
        private static readonly Regex OldPlate = new("^([A-Z]{3}[0-9]{3}|[0-9]{3}[A-Z]{3})$");

        private ImageAnalysisClient _client;
        private ILogger<AzurePlateReaderService> _logger;

        public AzurePlateReaderService(IConfiguration configuration, ILogger<AzurePlateReaderService> logger)
        {
            _client = new ImageAnalysisClient(
                new Uri(configuration["Azure:Vision:Endpoint"]),
                new AzureKeyCredential(configuration["Azure:Vision:Key"]));
            _logger = logger;
        }

        public async Task<string?> ReadPlateAsync(byte[] imageData)
        {
            var response = await _client.AnalyzeAsync(BinaryData.FromBytes(imageData), VisualFeatures.Read);

            var lines = response.Value.Read.Blocks
                .SelectMany(block => block.Lines)
                .Select(line => line.Text)
                .ToList();

            _logger.LogInformation("OCR found {Count} text line(s): {Lines}", lines.Count, string.Join(" | ", lines));

            // A plate can be split over several lines (e.g. the "B" of the EU strip), so also try the whole text
            var candidates = lines.Append(string.Concat(lines));

            return candidates.Select(FindPlate).FirstOrDefault(plate => plate != null);
        }

        private static string? FindPlate(string text)
        {
            var cleaned = new string(text.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

            // Slide a window over the text so extra characters (like the "B" country code) are skipped
            for (var start = 0; start + 7 <= cleaned.Length; start++)
            {
                var candidate = FixConfusedCharacters(cleaned.Substring(start, 7), "DLLLDDD");
                if (CurrentPlate.IsMatch(candidate))
                    return $"{candidate[0]}-{candidate.Substring(1, 3)}-{candidate.Substring(4, 3)}";
            }

            for (var start = 0; start + 6 <= cleaned.Length; start++)
            {
                var window = cleaned.Substring(start, 6);
                foreach (var pattern in new[] { "LLLDDD", "DDDLLL" })
                {
                    var candidate = FixConfusedCharacters(window, pattern);
                    if (OldPlate.IsMatch(candidate))
                        return $"{candidate.Substring(0, 3)}-{candidate.Substring(3, 3)}";
                }
            }

            return null;
        }

        // OCR often mixes up look-alike characters. The pattern says per position: L = letter, D = digit.
        private static string FixConfusedCharacters(string text, string pattern)
        {
            var result = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                result.Append(pattern[i] == 'D'
                    ? c switch { 'O' => '0', 'Q' => '0', 'D' => '0', 'I' => '1', 'L' => '1', 'Z' => '2', 'S' => '5', 'G' => '6', 'B' => '8', _ => c }
                    : c switch { '0' => 'O', '1' => 'I', '2' => 'Z', '5' => 'S', '6' => 'G', '8' => 'B', _ => c });
            }
            return result.ToString();
        }
    }
}
