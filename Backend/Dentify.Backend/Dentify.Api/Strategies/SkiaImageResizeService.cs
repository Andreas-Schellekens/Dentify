using SkiaSharp;

namespace Dentify.Api.Strategies
{
    // Shrinks phone photos (often 12+ MB) before they are stored and sent to Azure.
    // Custom Vision accepts at most 4 MB per prediction image; 2048 px JPEGs are well below that.
    public class SkiaImageResizeService : IImageResizeService
    {
        private const int JpegQuality = 85;

        private int _maxDimension;

        public SkiaImageResizeService(IConfiguration configuration)
        {
            _maxDimension = configuration.GetValue("Photos:MaxDimension", 2048);
        }

        public byte[] PrepareForAnalysis(byte[] imageData)
        {
            using var stream = new MemoryStream(imageData);
            using var codec = SKCodec.Create(stream)
                ?? throw new InvalidDataException("The file is not a readable image.");
            using var decoded = SKBitmap.Decode(codec)
                ?? throw new InvalidDataException("The file is not a readable image.");

            // Phones store the rotation in EXIF metadata; apply it so Azure sees the photo upright
            var origin = codec.EncodedOrigin;
            var isSideways = origin is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom
                or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;

            var uprightWidth = isSideways ? decoded.Height : decoded.Width;
            var uprightHeight = isSideways ? decoded.Width : decoded.Height;
            var scale = Math.Min(1.0, (double)_maxDimension / Math.Max(uprightWidth, uprightHeight));

            var targetWidth = Math.Max(1, (int)Math.Round(decoded.Width * scale));
            var targetHeight = Math.Max(1, (int)Math.Round(decoded.Height * scale));

            using var resized = scale < 1.0
                ? decoded.Resize(new SKImageInfo(targetWidth, targetHeight),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                : decoded.Copy();

            using var upright = Rotate(resized, origin);
            using var data = upright.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return data.ToArray();
        }

        private static SKBitmap Rotate(SKBitmap source, SKEncodedOrigin origin)
        {
            var degrees = origin switch
            {
                SKEncodedOrigin.BottomRight => 180,
                SKEncodedOrigin.RightTop => 90,
                SKEncodedOrigin.LeftBottom => 270,
                _ => 0
            };

            if (degrees == 0)
                return source.Copy();

            var swap = degrees != 180;
            var rotated = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
            using var canvas = new SKCanvas(rotated);
            canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), null);
            return rotated;
        }
    }
}
