namespace Dentify.Contracts.Models
{
    public class InspectionPhotoListDto
    {
        public int Id { get; set; }

        // Relative URL, e.g. "/uploads/3f2a....jpg". Prefix it with the API base URL.
        public string Url { get; set; }

        // Same photo with the boxes of the trained model. Null when there is no box to draw, and for older photos
        public string? ModelResultUrl { get; set; }

        // Same photo after the AI review (kept boxes, dropped ones dashed). Null in the same cases
        public string? ReviewResultUrl { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
