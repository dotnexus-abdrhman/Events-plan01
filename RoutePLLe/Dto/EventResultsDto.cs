using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class OptionResultDto
    {
        public Guid OptionId { get; set; }
        public string Text { get; set; } = "";
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    public class SurveyResultDto
    {
        public Guid SessionId { get; set; }
        public string Question { get; set; } = "";
        public string TypeName { get; set; } = "";   // SingleChoice / MultipleChoice / Rating / OpenEnded
        public int TotalResponses { get; set; }
        public List<OptionResultDto> Options { get; set; } = new();
        public List<string> TextAnswers { get; set; } = new(); // للأدمن فقط
    }

    public class EventResultsDto
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = "";

        // إجمالي المشاركين (تصويت أو نقاش)
        public int TotalVoters { get; set; }

        // إجمالي ردود النقاش
        public int DiscussionCount { get; set; }

        // ملخص الاستبيان حسب السؤال/الخيار
        public List<SurveyResultDto> SurveyResults { get; set; } = new();
        // (جاهزين للنقاش/الورش لاحقًا)
        public List<DiscussionPostDto> Discussion { get; set; } = new();
        public List<ProposalSummaryDto> Proposals { get; set; } = new();

        // تجميع إجابات حسب المستخدم
        public List<UserSurveyResponseDto> UserResponses { get; set; } = new();
    }

    public class UserSurveyResponseDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string? SignatureImageUrl { get; set; }
        public string? SignatureText { get; set; }
        public List<UserAnswerDto> Answers { get; set; } = new();
        public List<UserDiscussionReplyDto> Discussions { get; set; } = new();
    }

    public class UserAnswerDto
    {
        public Guid SessionId { get; set; }
        public string Question { get; set; } = "";
        public List<string> SelectedOptions { get; set; } = new();
        public string? TextAnswer { get; set; }
    }

    public class UserDiscussionReplyDto
    {
        public Guid RootPostId { get; set; }
        public string RootTitle { get; set; } = "";
        public string ReplyBody { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }


    // Placeholders مؤقتًا (هنفعّلهم لما نضيف الكيانات)
    public class DiscussionPostDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string UserName { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime CreatedAt { get; set; }


        public List<DiscussionPostDto> Replies { get; set; } = new();
    }

    public class ProposalSummaryDto
    {
        public Guid ProposalId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";            //


        public string Status { get; set; } = "Pending";
        public int Upvotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = "";
    }
}

