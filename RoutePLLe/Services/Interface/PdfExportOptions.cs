using System;

namespace EventPl.Services.Interface
{
    public class PdfExportOptions
    {
        public bool IncludeEventDetails { get; set; } = true;
        public bool IncludeSurveyAndResponses { get; set; } = true;
        public bool IncludeDiscussions { get; set; } = true;
        public bool IncludeSignatures { get; set; } = true;
        public bool IncludeSections { get; set; } = false;
        public bool IncludeAttachments { get; set; } = false;

        // Branding / Header
        public string? BrandingFooterText { get; set; } = "منصة مينا لإدارة الفعاليات";
        public string? CustomTitle { get; set; }
        public byte[]? LogoBytes { get; set; }

        // Appearance
        public byte[]? BackgroundImageBytes { get; set; }
        public float BackgroundOpacity { get; set; } = 0.15f; // 0..1
        public string? FontColorHex { get; set; } = "#000000"; // applies to all text
        public string? FontFamily { get; set; } // e.g., Cairo, Tajawal, Arial
        public int BaseFontSize { get; set; } = 11;

        // Table header background color (hex). Defaults to Teal if null/empty
        public string? TableHeaderBackgroundColorHex { get; set; }

        // Verification
        public Guid? VerificationId { get; set; }
        public string? VerificationUrlBase { get; set; } // e.g., https://host[:port][/base]
        public string? VerificationType { get; set; } // e.g., CustomResults, CustomWithParticipants

        // QR Code customization options
        public int QrCodeSize { get; set; } = 45; // pixels (render size)
        public string QrCodePosition { get; set; } = "BottomLeft"; // BottomLeft, BottomRight, BottomCenter
        public bool ShowQrCode { get; set; } = true; // toggle QR image
        public bool ShowVerificationUrl { get; set; } = true; // toggle textual URL in footer
    }
}

