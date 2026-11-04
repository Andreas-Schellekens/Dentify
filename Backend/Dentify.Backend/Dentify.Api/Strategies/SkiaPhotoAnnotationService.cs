using SkiaSharp;

namespace Dentify.Api.Strategies
{
    // Draws the damage boxes on a copy of the photo, so the app can show what the model and the review found
    public class SkiaPhotoAnnotationService : IPhotoAnnotationService
    {
        private const int JpegQuality = 85;

        private static readonly SKColor DetectionColor = SKColor.Parse("#F59E0B"); // amber: proposed by the model
        private static readonly SKColor KeptColor = SKColor.Parse("#16A34A");      // green: kept as a damage
        private static readonly SKColor RejectedColor = SKColor.Parse("#DC2626");  // red: rejected by the AI check
        private static readonly SKColor UncertainColor = SKColor.Parse("#6B7280"); // grey: no AI verdict, too uncertain

        public byte[] DrawDetections(byte[] imageData, IEnumerable<DamagePrediction> damages)
        {
            return Draw(imageData, (canvas, size) =>
            {
                foreach (var damage in damages.OrderBy(d => d.Confidence))
                    DrawBox(canvas, size, damage, DetectionColor, $"{damage.Description} {damage.Confidence:P0}", dashed: false);
            });
        }

        public byte[] DrawReview(byte[] imageData, DamageReviewResult review)
        {
            return Draw(imageData, (canvas, size) =>
            {
                // Dropped damages first, so the kept boxes stay on top
                foreach (var damage in review.Uncertain)
                    DrawBox(canvas, size, damage, UncertainColor, $"{damage.Description} (uncertain)", dashed: true);

                foreach (var damage in review.Rejected)
                    DrawBox(canvas, size, damage, RejectedColor, $"{damage.Description} (rejected)", dashed: true);

                foreach (var damage in review.Kept)
                    DrawBox(canvas, size, damage, KeptColor, $"{damage.Description} {damage.Confidence:P0}", dashed: false);
            });
        }

        private static byte[] Draw(byte[] imageData, Action<SKCanvas, SKSizeI> drawDamages)
        {
            using var bitmap = SKBitmap.Decode(imageData)
                ?? throw new InvalidDataException("The file is not a readable image.");

            using (var canvas = new SKCanvas(bitmap))
                drawDamages(canvas, new SKSizeI(bitmap.Width, bitmap.Height));

            using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return data.ToArray();
        }

        private static void DrawBox(SKCanvas canvas, SKSizeI size, DamagePrediction damage, SKColor color, string label, bool dashed)
        {
            // A classification project has no box: nothing to draw
            if (damage.Box == null)
                return;

            // Line width and text size grow with the photo, so they look the same on small and large photos
            var shortSide = Math.Min(size.Width, size.Height);
            var lineWidth = Math.Max(3f, shortSide / 180f);
            var textSize = Math.Max(18f, shortSide / 24f);

            var rect = SKRect.Create(
                (float)(damage.Box.Left * size.Width),
                (float)(damage.Box.Top * size.Height),
                (float)(damage.Box.Width * size.Width),
                (float)(damage.Box.Height * size.Height));

            using var boxPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = dashed ? lineWidth * 0.7f : lineWidth,
                IsAntialias = true,
                PathEffect = dashed ? SKPathEffect.CreateDash(new[] { lineWidth * 4, lineWidth * 3 }, 0) : null
            };
            canvas.DrawRect(rect, boxPaint);

            // Label in a filled tag above the box, or inside it when the box touches the top of the photo
            using var font = new SKFont(SKTypeface.Default, textSize);
            var padding = textSize * 0.3f;
            var tagWidth = font.MeasureText(label) + padding * 2;
            var tagHeight = textSize + padding * 2;
            var tagTop = rect.Top - tagHeight >= 0 ? rect.Top - tagHeight : rect.Top;
            var tagLeft = Math.Min(rect.Left, size.Width - tagWidth);

            using var tagPaint = new SKPaint { Color = dashed ? color.WithAlpha(200) : color, IsAntialias = true };
            canvas.DrawRect(SKRect.Create(tagLeft, tagTop, tagWidth, tagHeight), tagPaint);

            using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawText(label, tagLeft + padding, tagTop + padding + textSize * 0.85f, SKTextAlign.Left, font, textPaint);
        }
    }
}
