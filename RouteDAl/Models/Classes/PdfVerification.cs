using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    public class PdfVerification
    {
        [Key]
        public Guid PdfVerificationId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        [Required, MaxLength(50)]
        public string PdfType { get; set; } = "Results"; // e.g., CustomResults, CustomWithParticipants

        public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(300)]
        public string VerificationUrl { get; set; } = string.Empty;
    }
}
