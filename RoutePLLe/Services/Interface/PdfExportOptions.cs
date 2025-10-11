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
        public string? BrandingFooterText { get; set; } = "منصة مينا لإدارة الفعاليات";
        public string? CustomTitle { get; set; }
        public byte[]? LogoBytes { get; set; }
    }
}

