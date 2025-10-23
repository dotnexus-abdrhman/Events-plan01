using System;

namespace RourtPPl01.Areas.Public.ViewModels
{
    public class VerifyViewModel
    {
        public bool IsFound { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public Guid VerificationId { get; set; }
        public Guid EventId { get; set; }
        public string PdfType { get; set; } = "Results";
        public DateTime ExportedAtUtc { get; set; }
        public string VerificationUrl { get; set; } = string.Empty;
    }
}

