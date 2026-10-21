namespace Dentify.Contracts.Models
{
    public class InspectionPhotoListDto
    {
        public int Id { get; set; }

        // Relative URL, e.g. "/uploads/3f2a....jpg". Prefix it with the API base URL.
        public string Url { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
