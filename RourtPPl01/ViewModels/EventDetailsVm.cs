namespace EventPresentationlayer.ViewModels
{
    public class EventDetailsVm
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = "";
        public string TypeName { get; set; } = "";
        public string StatusName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }

        public bool IsSurvey { get; set; }
        public string Question { get; set; } = "";
        public bool IsMultipleChoice { get; set; }

        public List<OptionResultVm> Options { get; set; } = new();
        public List<UserAnswerVm> Participants { get; set; } = new();
    }

    public class OptionResultVm
    {
        public Guid OptionId { get; set; }
        public string Text { get; set; } = "";
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    public class UserAnswerVm
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public List<string> Answers { get; set; } = new();
        public List<string> TextAnswers { get; set; } = new();
        // إضافة عرض التوقيع إن وجد
        public string? SignatureText { get; set; }
        public string? SignatureImagePath { get; set; }
    }
}
