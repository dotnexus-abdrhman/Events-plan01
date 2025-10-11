using System.ComponentModel.DataAnnotations;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    // ============================================
    // Manage Sections
    // ============================================
    public class ManageSectionsViewModel
    {
        public Guid EventId { get; set; }
        public List<SectionItemViewModel> Sections { get; set; } = new();
    }

    public class SectionItemViewModel
    {
        public Guid SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int Order { get; set; }
        public int DecisionsCount { get; set; }
    }

    // ============================================
    // Add Section
    // ============================================
    public class AddSectionViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "عنوان البند مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "النص يجب ألا يتجاوز 5000 حرف")]
        public string? Body { get; set; }
    }

    // ============================================
    // Update Section
    // ============================================
    public class UpdateSectionViewModel
    {
        [Required]
        public Guid SectionId { get; set; }

        [Required(ErrorMessage = "عنوان البند مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "النص يجب ألا يتجاوز 5000 حرف")]
        public string? Body { get; set; }
    }

    // ============================================
    // Add Decision
    // ============================================
    public class AddDecisionViewModel
    {
        [Required]
        public Guid SectionId { get; set; }

        [Required(ErrorMessage = "عنوان القرار مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;
    }

    // ============================================
    // Add Decision Item
    // ============================================
    public class AddDecisionItemViewModel
    {
        [Required]
        public Guid DecisionId { get; set; }

        [Required(ErrorMessage = "نص العنصر مطلوب")]
        [StringLength(500, ErrorMessage = "النص يجب ألا يتجاوز 500 حرف")]
        public string Text { get; set; } = string.Empty;
    }
}

