using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EventPl.Services.ClassServices
{
    public class EventResultsService : IEventResultsService
    {
        private static readonly HashSet<string> _optionTypes = new(StringComparer.OrdinalIgnoreCase)
        { "SingleChoice", "MultipleChoice", "Rating" };

        private readonly AppDbContext _db;
        public EventResultsService(AppDbContext db) => _db = db;

        public Task<EventResultsDto> GetAdminResultsAsync(Guid eventId)
            => BuildCoreAsync(eventId, includeTextAnswers: true);

        public async Task<EventResultsDto> GetUserResultsAsync(Guid eventId)
        {
            var dto = await BuildCoreAsync(eventId, includeTextAnswers: false);
            foreach (var q in dto.SurveyResults) q.TextAnswers.Clear();
            return dto;
        }

        private async Task<EventResultsDto> BuildCoreAsync(Guid eventId, bool includeTextAnswers)
        {
            var ev = await _db.Events.AsNoTracking()
                      .FirstOrDefaultAsync(e => e.EventId == eventId)
                  ?? throw new Exception("الفعالية غير موجودة.");

            var sessions = await _db.VotingSessions
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .Include(s => s.VotingOptions)
                .ToListAsync();

            var votes = await _db.Votes
                .AsNoTracking()
                .Where(v => v.VotingSession.EventId == eventId)
                .ToListAsync();

            var result = new EventResultsDto
            {
                EventId = eventId,
                EventTitle = ev.Title
            };

            // ========================
            // 📌 1) نتائج الاستبيان
            // ========================
            foreach (var s in sessions)
            {
                var sr = new SurveyResultDto
                {
                    SessionId = s.VotingSessionId,
                    Question = s.Question,
                    TypeName = s.Type.ToString()
                };

                if (_optionTypes.Contains(sr.TypeName))
                {
                    var sessionVotes = votes.Where(v => v.VotingSessionId == s.VotingSessionId).ToList();

                    // هل هذه الجلسة متعددة الاختيارات؟
                    bool isMulti = false;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(s.Settings))
                        {
                            using var doc = JsonDocument.Parse(s.Settings);
                            if (doc.RootElement.TryGetProperty("isMultipleChoice", out var p))
                                isMulti = p.GetBoolean();
                        }
                    }
                    catch { }

                    var ordered = s.VotingOptions.OrderBy(o => o.OrderIndex).ToList();
                    var counts = ordered.ToDictionary(o => o.VotingOptionId, _ => 0);

                    // 1) أصوات مباشرة (SingleChoice)
                    foreach (var opt in ordered)
                    {
                        counts[opt.VotingOptionId] += sessionVotes.Count(v => v.VotingOptionId == opt.VotingOptionId);
                    }

                    // 2) أصوات مشفّرة في CustomAnswer كـ JSON Array (MultipleChoice)
                    foreach (var v in sessionVotes.Where(x => x.VotingOptionId == null && !string.IsNullOrWhiteSpace(x.CustomAnswer)))
                    {
                        try
                        {
                            using var arrDoc = JsonDocument.Parse(v.CustomAnswer!);
                            if (arrDoc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var el in arrDoc.RootElement.EnumerateArray())
                                {
                                    if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var gid))
                                    {
                                        if (counts.ContainsKey(gid)) counts[gid] += 1;
                                    }
                                }
                            }
                        }
                        catch { /* تجاهل أي تنسيق غير متوقع */ }
                    }

                    // إجمالي المستجيبين (مستخدِمون مميّزون) — مناسب للتقسيم على النسب
                    var totalUsers = sessionVotes.Select(v => v.UserId).Distinct().Count();
                    sr.TotalResponses = totalUsers;

                    foreach (var opt in ordered)
                    {
                        var cnt = counts[opt.VotingOptionId];
                        sr.Options.Add(new EventPl.Dto.OptionResultDto
                        {
                            OptionId = opt.VotingOptionId,
                            Text = opt.Text,
                            Count = cnt,
                            Percent = totalUsers == 0 ? 0 : Math.Round((double)cnt * 100 / totalUsers, 1)
                        });
                    }
                }
                else // OpenEnded
                {
                    var txts = votes
                        .Where(v => v.VotingSessionId == s.VotingSessionId && !string.IsNullOrWhiteSpace(v.CustomAnswer))
                        .OrderByDescending(v => v.VotedAt)
                        .Select(v => v.CustomAnswer!.Trim())
                        .ToList();

                    sr.TotalResponses = txts.Count;
                    if (includeTextAnswers)
                        sr.TextAnswers = txts;
                }

                result.SurveyResults.Add(sr);
            }

            // ========================
            // 📌 2) نقاش الفعالية
            // ========================
            var posts = await _db.DiscussionPosts
                .AsNoTracking()
                .Where(p => p.EventId == eventId)
                .Include(p => p.User)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            // إجمالي ردود النقاش (الردود فقط)
            result.DiscussionCount = posts.Count(p => p.ParentId != null);

            var dict = posts.ToDictionary(
                p => p.DiscussionPostId,
                p => new DiscussionPostDto
                {
                    Id = p.DiscussionPostId,
                    ParentId = p.ParentId,
                    Body = p.Body,
                    CreatedAt = p.CreatedAt,
                    UserName = p.User?.FullName ?? "مستخدم"
                });

            foreach (var d in dict.Values.Where(x => x.ParentId.HasValue))
            {
                if (dict.TryGetValue(d.ParentId.Value, out var parent))
                    parent.Replies.Add(d);
            }

            result.Discussion = dict.Values
                                    .Where(x => x.ParentId == null)
                                    .OrderBy(x => x.CreatedAt)
                                    .ToList();

            // ========================
            // 📌 3) إجابات حسب المستخدم + النقاش + التوقيع
            // ========================
            // مَن شارك؟ (تصويت أو نقاش)
            var voterUserIds = votes.Select(v => v.UserId).Distinct();
            var postUserIds = posts.Select(p => p.UserId).Distinct();
            var participantUserIds = voterUserIds.Union(postUserIds).Distinct().ToList();
            result.TotalVoters = participantUserIds.Count;

            // بيانات المستخدمين
            var users = await _db.Users.AsNoTracking()
                                       .Where(u => participantUserIds.Contains(u.UserId))
                                       .Select(u => new { u.UserId, u.FullName, u.Phone })
                                       .ToListAsync();
            var usersMap = users.ToDictionary(x => x.UserId, x => (Name: x.FullName ?? "مستخدم", Phone: x.Phone));

            // قاموس خيارات لكل جلسة
            var optionText = sessions.SelectMany(s => s.VotingOptions)
                                     .ToDictionary(o => o.VotingOptionId, o => (o.Text, o.VotingSessionId));

            // تحديد الجلسات المتعددة من Settings
            var multiSessions = new HashSet<Guid>();
            foreach (var s in sessions)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(s.Settings))
                    {
                        using var doc = JsonDocument.Parse(s.Settings);
                        if (doc.RootElement.TryGetProperty("isMultipleChoice", out var p) && p.GetBoolean())
                            multiSessions.Add(s.VotingSessionId);
                    }
                }
                catch { }
            }

            // وثائق التوقيع (صور/نصوص)
            var sigDocs = await _db.Documents.AsNoTracking()
                                .Where(d => d.EventId == eventId && participantUserIds.Contains(d.UploadedById) && d.FilePath.Contains("/signatures/"))
                                .OrderByDescending(d => d.UploadedAt)
                                .ToListAsync();

            foreach (var uid in participantUserIds)
            {
                var userName = usersMap.TryGetValue(uid, out var nm) ? nm.Name : "مستخدم";
                var phone = usersMap.TryGetValue(uid, out var ph) ? ph.Phone : null;

                var imgDoc = sigDocs.FirstOrDefault(d => d.UploadedById == uid && (d.FileType ?? "").StartsWith("image", StringComparison.OrdinalIgnoreCase));
                var txtDoc = sigDocs.FirstOrDefault(d => d.UploadedById == uid && string.Equals(d.FileType, "text/plain", StringComparison.OrdinalIgnoreCase));

                var resp = new UserSurveyResponseDto
                {
                    UserId = uid,
                    UserName = userName,
                    Phone = phone,
                    SignatureImageUrl = imgDoc?.FilePath,
                    SignatureText = txtDoc?.FilePath
                };

                // آخر نشاط
                DateTime lastVoteAt = votes.Where(v => v.UserId == uid)
                                           .Select(v => (DateTime?)v.VotedAt)
                                           .OrderByDescending(x => x)
                                           .FirstOrDefault() ?? DateTime.MinValue;
                DateTime lastPostAt = posts.Where(p => p.UserId == uid)
                                           .Select(p => (DateTime?)p.CreatedAt)
                                           .OrderByDescending(x => x)
                                           .FirstOrDefault() ?? DateTime.MinValue;
                resp.LastActivityAt = lastVoteAt > lastPostAt ? lastVoteAt : lastPostAt;

                // إجابات الاستبيان (أحدث تمثيل لكل جلسة)
                foreach (var s in sessions)
                {
                    var ua = new UserAnswerDto { SessionId = s.VotingSessionId, Question = s.Question };

                    var userVotesForSession = votes.Where(v => v.UserId == uid && v.VotingSessionId == s.VotingSessionId).ToList();

                    if (_optionTypes.Contains(s.Type.ToString()))
                    {
                        var texts = new List<string>();

                        var one = userVotesForSession.FirstOrDefault(v => v.VotingOptionId != null);
                        if (one?.VotingOptionId != null && optionText.TryGetValue(one.VotingOptionId.Value, out var t1))
                            texts.Add(t1.Text);

                        var multi = userVotesForSession.FirstOrDefault(v => v.VotingOptionId == null && !string.IsNullOrWhiteSpace(v.CustomAnswer));
                        if (multi != null)
                        {
                            try
                            {
                                using var arrDoc = JsonDocument.Parse(multi.CustomAnswer!);
                                if (arrDoc.RootElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var el in arrDoc.RootElement.EnumerateArray())
                                    {
                                        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var gid))
                                        {
                                            if (optionText.TryGetValue(gid, out var t)) texts.Add(t.Text);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        if (texts.Count > 0) ua.SelectedOptions = texts;
                    }
                    else
                    {
                        var text = userVotesForSession.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.CustomAnswer))?.CustomAnswer;
                        if (!string.IsNullOrWhiteSpace(text)) ua.TextAnswer = text!.Trim();
                    }

                    if ((ua.SelectedOptions?.Count ?? 0) > 0 || !string.IsNullOrWhiteSpace(ua.TextAnswer))
                        resp.Answers.Add(ua);
                }

                // ردود النقاش الخاصة بالمستخدم
                var userReplies = posts.Where(p => p.UserId == uid && p.ParentId != null).ToList();
                foreach (var rp in userReplies)
                {
                    var root = rp;
                    while (root?.ParentId != null)
                    {
                        root = posts.FirstOrDefault(x => x.DiscussionPostId == root.ParentId);
                    }
                    var rootTitle = root?.Body ?? "";

                    resp.Discussions.Add(new UserDiscussionReplyDto
                    {
                        RootPostId = root?.DiscussionPostId ?? Guid.Empty,
                        RootTitle = string.IsNullOrWhiteSpace(rootTitle) ? "" : (rootTitle.Length > 50 ? rootTitle.Substring(0, 50) + "..." : rootTitle),
                        ReplyBody = rp.Body,
                        CreatedAt = rp.CreatedAt
                    });
                }

                if (resp.Answers.Count > 0 || resp.Discussions.Count > 0 || resp.SignatureImageUrl != null)
                    result.UserResponses.Add(resp);
            }

            // ترتيب بحسب الأحدث
            result.UserResponses = result.UserResponses
                .OrderByDescending(u => u.LastActivityAt)
                .ToList();

            // ========================
            // 📌 4) اقتراحات الورشة
            // ========================
            var proposals = await _db.Proposals
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.UpvoteList)
                .Where(p => p.EventId == eventId)
                .OrderByDescending(p => p.Upvotes)
                .ToListAsync();

            result.Proposals = proposals.Select(p => new ProposalSummaryDto
            {
                ProposalId = p.ProposalId,
                Title = p.Title,
                Body = p.Body,
                Status = p.Status,
                Upvotes = p.Upvotes,
                CreatedAt = p.CreatedAt,
                UserName = p.User?.FullName ?? "مستخدم"
            }).ToList();

            return result;
        }
    }
}
