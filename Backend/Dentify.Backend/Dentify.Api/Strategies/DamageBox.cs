namespace Dentify.Api.Strategies
{
    // Bounding box of a damage on the photo, as fractions (0..1) of the photo width and height
    public class DamageBox
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public DamageBox(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}
