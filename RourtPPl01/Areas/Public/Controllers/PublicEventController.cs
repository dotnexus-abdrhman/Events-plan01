using EventPl.Dto;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using RourtPPl01.Areas.UserPortal.ViewModels;
using System;

namespace RourtPPl01.Areas.Public.Controllers
{
    [Area("Public")]
    public class PublicEventController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMinaEventsService _eventsService;
        private readonly ISectionsService _sectionsService;
        private readonly ISurveysService _surveysService;
        private readonly IDiscussionsService _discussionsService;
        private readonly ITableBlocksService _tablesService;
        private readonly IAttachmentsService _attachmentsService;
        private readonly ISignaturesService _signaturesService;
        private readonly ICrudService<UserDto, Guid> _usersService;
        private readonly ILogger<PublicEventController> _logger;

        public PublicEventController(AppDbContext db,
            IMinaEventsService eventsService,
            ISectionsService sectionsService,
            ISurveysService surveysService,
            IDiscussionsService discussionsService,
            ITableBlocksService tablesService,
            IAttachmentsService attachmentsService,
            ISignaturesService signaturesService,
            ICrudService<UserDto, Guid> usersService,
            ILogger<PublicEventController> logger)
        {
            _db = db;
            _eventsService = eventsService;
            _sectionsService = sectionsService;
            _surveysService = surveysService;
            _discussionsService = discussionsService;
            _tablesService = tablesService;
            _attachmentsService = attachmentsService;
            _signaturesService = signaturesService;
            _usersService = usersService;
            _logger = logger;
        }

        [HttpGet("/Public/Event/{token}")]
        public async Task<IActionResult> Event(string token)
        {
            _logger.LogInformation("Public Event GET start. token={Token}", token);
            var (ev, reason) = await ValidateTokenAsync(token);
            if (ev == null)
            {
                _logger.LogWarning("Public Event GET invalid link. token={Token} reason={Reason}", token, reason);
                ViewBag.Error = reason ?? "الرابط غير صالح";
                return View("InvalidLink");
            }

            // Try get guest user id from cookie
            var cookieKey = $"PublicGuest_{token}";
            if (Request.Cookies.TryGetValue(cookieKey, out var userIdStr) && Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation("Public Event GET cookie found -> Details. token={Token} userId={UserId} eventId={EventId}", token, userId, ev.EventId);
                return await DetailsInternal(token, ev.EventId, userId);
            }

            // no cookie => ask for name
            _logger.LogInformation("Public Event GET no cookie -> EnterName. token={Token} eventId={EventId}", token, ev.EventId);
            ViewBag.Token = token;
            ViewBag.EventTitle = ev.Title;
            return View("EnterName");
        }

        [HttpPost("/Public/Event/{token}/EnterName")]
        public async Task<IActionResult> EnterName(string token, string fullName, string? email, string? phone)
        {
            var (ev, reason) = await ValidateTokenAsync(token);
            if (ev == null)
            {
                TempData["Error"] = reason ?? "الرابط غير صالح";
                return RedirectToAction(nameof(Event), new { token });
            }

            fullName = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "الاسم الكامل مطلوب";
                return RedirectToAction(nameof(Event), new { token });
            }

            // Ensure a valid email to satisfy User entity requirements
            var syntheticUserId = Guid.NewGuid();
            var normalizedEmail = string.IsNullOrWhiteSpace(email)
                ? $"guest+{syntheticUserId:N}@public.local"
                : email.Trim();

            // Create synthetic user
            var userDto = new UserDto
            {
                UserId = syntheticUserId,
                OrganizationId = ev.OrganizationId,
                FullName = fullName,
                Email = normalizedEmail,
                Phone = string.IsNullOrWhiteSpace(phone) ? string.Empty : phone.Trim(),
                RoleName = "Attendee",
                ProfilePicture = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            Guid finalUserId = userDto.UserId;
            try
            {
                await _usersService.CreateAsync(userDto);
            }
            catch (Exception)
            {
                // Likely duplicate email or other constraint; clear pending added entities
                try { _db.ChangeTracker.Clear(); } catch { }

                // Try reuse existing user by email
                var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
                if (existingUser != null)
                {
                    finalUserId = existingUser.UserId;
                }
                else
                {
                    // Fallback: use a unique synthetic email and create again
                    normalizedEmail = $"guest+{syntheticUserId:N}@public.local";
                    userDto.Email = normalizedEmail;
                    try { await _usersService.CreateAsync(userDto); } catch { try { _db.ChangeTracker.Clear(); } catch { } }
                }
            }

            // Persist guest record
            _db.PublicEventGuests.Add(new EvenDAL.Models.Classes.PublicEventGuest
            {
                GuestId = Guid.NewGuid(),
                EventId = ev.EventId,
                FullName = fullName,
                Email = normalizedEmail,
                Phone = phone,
                UniqueToken = token,
                IsGuest = true,
                UserId = finalUserId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Set cookie for subsequent interactions
            var cookieKey = $"PublicGuest_{token}";
            Response.Cookies.Append(cookieKey, finalUserId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return await DetailsInternal(token, ev.EventId, finalUserId);
        }

        [HttpPost("/Public/Event/{token}/Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string token, SubmitResponseViewModel vm)
        {
            var (ev, reason) = await ValidateTokenAsync(token);
            if (ev == null)
            {
                TempData["Error"] = reason ?? "الرابط غير صالح";
                return RedirectToAction(nameof(Event), new { token });
            }

            var cookieKey = $"PublicGuest_{token}";
            if (!Request.Cookies.TryGetValue(cookieKey, out var userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                TempData["Error"] = "يرجى إدخال الاسم أولاً";
                return RedirectToAction(nameof(Event), new { token });
            }

            if (vm.EventId != ev.EventId)
            {
                return BadRequest();
            }

            try
            {
                // Prevent duplicate participation
                var already = (await _surveysService.HasUserAnsweredAsync(vm.EventId, userId))
                    || (await _signaturesService.HasUserSignedAsync(vm.EventId, userId));
                if (already)
                {
                    TempData["Error"] = "لقد سبق أن شاركت في هذا الحدث";
                    return RedirectToAction(nameof(Event), new { token });
                }

                // Validate signature if required
                if (ev.RequireSignature && string.IsNullOrWhiteSpace(vm.SignatureData))
                {
                    TempData["Error"] = "التوقيع مطلوب لإرسال الردود";
                    return RedirectToAction(nameof(Event), new { token });
                }

                // Parse survey answers
                var questionAnswers = new List<QuestionAnswerDto>();
                foreach (var key in Request.Form.Keys)
                {
                    if (!key.StartsWith("SurveyAnswers[")) continue;
                    var start = key.IndexOf('[') + 1;
                    var end = key.IndexOf(']');
                    if (start <= 0 || end <= start) continue;
                    var qidStr = key.Substring(start, end - start);
                    if (!Guid.TryParse(qidStr, out var questionId)) continue;
                    var values = Request.Form[key];
                    var selected = values.Where(v => v != null && Guid.TryParse(v, out _)).Select(v => Guid.Parse(v!)).ToList();
                    if (selected.Count > 0)
                    {
                        questionAnswers.Add(new QuestionAnswerDto
                        {
                            QuestionId = questionId,
                            SelectedOptionIds = selected
                        });
                    }
                }
                if (questionAnswers.Count > 0)
                {
                    var saveAnswers = new SaveSurveyAnswersRequest
                    {
                        EventId = vm.EventId,
                        UserId = userId,
                        Answers = questionAnswers
                    };
                    await _surveysService.SaveUserAnswersAsync(saveAnswers);
                }

                // Parse discussion replies
                foreach (var key in Request.Form.Keys)
                {
                    if (!key.StartsWith("DiscussionReplies[")) continue;
                    var start = key.IndexOf('[') + 1;
                    var end = key.IndexOf(']');
                    if (start <= 0 || end <= start) continue;
                    var didStr = key.Substring(start, end - start);
                    if (!Guid.TryParse(didStr, out var discussionId)) continue;
                    var body = (Request.Form[key].ToString() ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        await _discussionsService.AddReplyAsync(new AddDiscussionReplyRequest
                        {
                            DiscussionId = discussionId,
                            UserId = userId,
                            Body = body
                        });
                    }
                }

                // Save signature
                if (ev.RequireSignature && !string.IsNullOrWhiteSpace(vm.SignatureData))
                {
                    await _signaturesService.SaveSignatureAsync(new SaveSignatureRequest
                    {
                        EventId = vm.EventId,
                        UserId = userId,
                        SignatureData = vm.SignatureData
                    });
                }

                // Regenerate merged Custom PDF (if any) so participants table reflects latest participation
                await _attachmentsService.RegenerateMergedCustomPdfIfAnyAsync(vm.EventId);

                TempData["PublicSuccess"] = "تم ارسال ردودك بنجاح";
                TempData["JustSubmitted"] = "1";
                return RedirectToAction(nameof(Confirmation), new { token, eventId = vm.EventId, submitted = 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال الردود عبر الرابط العام للحدث {EventId}", vm.EventId);
                TempData["Error"] = "حدث خطأ أثناء إرسال الردود";
                return RedirectToAction(nameof(Event), new { token });
            }
        }

        [HttpGet("/Public/Event/{token}/Confirmation")]
        public async Task<IActionResult> Confirmation(string token, Guid eventId)
        {
            _logger.LogInformation("Public Confirmation GET start. token={Token} eventIdParam={EventIdParam}", token, eventId);
            var (ev, reason) = await ValidateTokenAsync(token);
            if (ev == null)
            {
                _logger.LogWarning("Public Confirmation invalid link. token={Token} reason={Reason}", token, reason);
                ViewBag.Error = reason ?? "الرابط غير صالح";
                return View("InvalidLink");
            }

            // Must have the guest cookie for this token
            var cookieKey = $"PublicGuest_{token}";
            if (!Request.Cookies.TryGetValue(cookieKey, out var userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation("Public Confirmation no cookie -> redirect Event. token={Token}", token);
                return RedirectToAction(nameof(Event), new { token });
            }

            // Event id must match the token's event
            if (eventId != ev.EventId)
            {
                _logger.LogInformation("Public Confirmation eventId mismatch -> redirect Event. token={Token} eventIdParam={EventIdParam} expected={Expected}", token, eventId, ev.EventId);
                return RedirectToAction(nameof(Event), new { token });
            }

            // Only allow direct access if this request came right after a valid submission
            var submittedFlag = (TempData.ContainsKey("JustSubmitted") || string.Equals(HttpContext.Request.Query["submitted"], "1", StringComparison.Ordinal));
            if (!submittedFlag)
            {
                _logger.LogInformation("Public Confirmation accessed directly -> redirect Event. token={Token}", token);
                return RedirectToAction(nameof(Event), new { token });
            }

            // Only show confirmation if there is evidence of participation
            var hasAnswered = await _surveysService.HasUserAnsweredAsync(eventId, userId);
            var hasSigned = await _signaturesService.HasUserSignedAsync(eventId, userId);
            if (!(hasAnswered || hasSigned))
            {
                _logger.LogInformation("Public Confirmation no participation -> redirect Event. token={Token} userId={UserId}", token, userId);
                return RedirectToAction(nameof(Event), new { token });
            }

            _logger.LogInformation("Public Confirmation allowed. token={Token} userId={UserId} eventId={EventId}", token, userId, eventId);
            // Pass success message to the view (scoped key to avoid leaking to Admin)
            ViewBag.Success = TempData.ContainsKey("PublicSuccess") ? TempData["PublicSuccess"] : null;

            return View("Confirmation");
        }

        private async Task<(EventDto? ev, string? reason)> ValidateTokenAsync(string token)
        {
            token = (token ?? string.Empty).Trim();
            var link = await _db.EventPublicLinks.AsNoTracking().FirstOrDefaultAsync(x => x.Token == token);
            var now = DateTime.UtcNow;
            if (link == null)
            {
                _logger.LogWarning("Public link validation failed: token not found. token={Token}", token);
                return (null, "الرابط غير صالح");
            }
            if (!link.IsEnabled)
            {
                _logger.LogWarning("Public link disabled by admin. token={Token}", token);
                return (null, "تم تعطيل الرابط من قبل المشرف");
            }
            if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < now)
            {
                _logger.LogWarning("Public link expired. token={Token} exp={Exp} nowUtc={Now}", token, link.ExpiresAt, now);
                return (null, "انتهت صلاحية الرابط");
            }

            var ev = await _eventsService.GetEventByIdAsync(link.EventId);
            if (ev == null)
            {
                _logger.LogWarning("Public link points to missing event. token={Token} eventId={EventId}", token, link.EventId);
                return (null, "الحدث غير موجود");
            }

            // Be permissive with status: only block if it is explicitly inactive/closed.
            var status = (ev.StatusName ?? string.Empty).Trim();
            var inactiveKeywords = new[] { "inactive", "archived", "closed", "cancelled", "canceled", "disabled", "منتهي", "مغلق", "مؤرشف", "معطل" };
            if (!string.IsNullOrEmpty(status) && inactiveKeywords.Any(k => status.Equals(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Event status not allowed for public link. token={Token} status={Status}", token, ev.StatusName);
                return (null, "الحدث غير متاح حالياً");
            }
            _logger.LogInformation("Public link validated. token={Token} eventId={EventId}", token, ev.EventId);
            return (ev, null);
        }

        private async Task<IActionResult> DetailsInternal(string token, Guid eventId, Guid userId)
        {
            ViewBag.Token = token;
            // Load components
            var eventDto = await _eventsService.GetEventByIdAsync(eventId);
            var versionTicks = (eventDto.UpdatedAt ?? eventDto.CreatedAt).Ticks;
            var sections = await _sectionsService.GetEventSectionsAsync(eventId, versionTicks);
            var surveys = await _surveysService.GetEventSurveysAsync(eventId, versionTicks);
            var discussions = await _discussionsService.GetEventDiscussionsAsync(eventId, versionTicks);
            var tables = await _tablesService.GetEventTablesAsync(eventId, versionTicks);
            var attachments = await _attachmentsService.GetEventAttachmentsAsync(eventId, versionTicks);
            // Hide merged Custom PDF from Public area (admins only)
            attachments = attachments
                .Where(a => !string.Equals(a.Type, "CustomPdfMerged", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var hasSignature = await _signaturesService.HasUserSignedAsync(eventId, userId);
            var hasUserParticipated = (await _surveysService.HasUserAnsweredAsync(eventId, userId))
                                      || hasSignature;

            var vm = new EventDetailsViewModel
            {
                EventId = eventDto!.EventId,
                EventTitle = eventDto.Title,
                EventDescription = eventDto.Description ?? string.Empty,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt,
                RequireSignature = eventDto.RequireSignature,
                HasUserSigned = hasSignature,
                HasUserParticipated = hasUserParticipated,
                Sections = sections.OrderBy(s => s.Order).Select(s => new SectionViewModel
                {
                    SectionId = s.SectionId,
                    Title = s.Title,
                    Body = s.Body,
                    Order = s.Order,
                    Decisions = (s.Decisions ?? new()).Select(d => new DecisionViewModel
                    {
                        Title = d.Title,
                        Order = d.Order,
                        Items = (d.Items ?? new()).Select(i => new DecisionItemViewModel { Text = i.Text, Order = i.Order }).ToList()
                    }).ToList(),
                    Surveys = surveys.Where(x => x.SectionId == s.SectionId).Select(ss => new SurveyViewModel
                    {
                        SurveyId = ss.SurveyId, Title = ss.Title, Description = null,
                        Questions = (ss.Questions ?? new()).Select(q => new QuestionViewModel
                        {
                            SurveyQuestionId = q.SurveyQuestionId,
                            Text = q.Text,
                            Type = Enum.TryParse<EvenDAL.Models.Shared.Enums.SurveyQuestionType>(q.Type, out var qType) ? qType : EvenDAL.Models.Shared.Enums.SurveyQuestionType.Single,
                            IsRequired = q.IsRequired,
                            Options = (q.Options ?? new()).Select(o => new OptionViewModel { SurveyOptionId = o.SurveyOptionId, Text = o.Text }).ToList()
                        }).ToList()
                    }).ToList(),
                    Discussions = discussions.Where(x => x.SectionId == s.SectionId).Select(dd => new DiscussionViewModel { DiscussionId = dd.DiscussionId, Title = dd.Title, Purpose = dd.Purpose }).ToList(),
                    Tables = tables.Where(x => x.SectionId == s.SectionId).Select(tt => new TableViewModel
                    {
                        Title = tt.Title, HasHeader = tt.HasHeader,
                        Rows = tt.TableData?.Rows?.Select(row => row.Cells.Select(cell => new TableCellViewModel { Value = cell }).ToList()).ToList()
                    }).ToList(),
                    Attachments = attachments.Where(x => x.SectionId == s.SectionId).Select(aa => new AttachmentViewModel
                    {
                        AttachmentId = aa.AttachmentId,
                        FileName = aa.FileName,
                        Path = aa.Path,
                        Type = Enum.TryParse<EvenDAL.Models.Shared.Enums.AttachmentType>(aa.Type, out var at) ? at : EvenDAL.Models.Shared.Enums.AttachmentType.Image,
                        Order = aa.Order
                    }).ToList()
                }).ToList(),
                Surveys = surveys.Where(s => s.SectionId == null).Select(s => new SurveyViewModel
                {
                    SurveyId = s.SurveyId, Title = s.Title, Description = null,
                    Questions = (s.Questions ?? new()).Select(q => new QuestionViewModel
                    {
                        SurveyQuestionId = q.SurveyQuestionId,
                        Text = q.Text,
                        Type = Enum.TryParse<EvenDAL.Models.Shared.Enums.SurveyQuestionType>(q.Type, out var qType) ? qType : EvenDAL.Models.Shared.Enums.SurveyQuestionType.Single,
                        IsRequired = q.IsRequired,
                        Options = (q.Options ?? new()).Select(o => new OptionViewModel { SurveyOptionId = o.SurveyOptionId, Text = o.Text }).ToList()
                    }).ToList()
                }).ToList(),
                Discussions = discussions.Where(d => d.SectionId == null).Select(d => new DiscussionViewModel { DiscussionId = d.DiscussionId, Title = d.Title, Purpose = d.Purpose }).ToList(),
                Tables = tables.Where(t => t.SectionId == null).Select(t => new TableViewModel
                {
                    Title = t.Title, HasHeader = t.HasHeader,
                    Rows = t.TableData?.Rows?.Select(row => row.Cells.Select(cell => new TableCellViewModel { Value = cell }).ToList()).ToList()
                }).ToList(),
                Attachments = attachments.Where(a => a.SectionId == null).Select(a => new AttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    Path = a.Path,
                    Type = Enum.TryParse<EvenDAL.Models.Shared.Enums.AttachmentType>(a.Type, out var at2) ? at2 : EvenDAL.Models.Shared.Enums.AttachmentType.Image,
                    Order = a.Order
                }).ToList()
            };

            // Render the Public wrapper view which reuses the UserPortal Details markup
            return View("~/Areas/Public/Views/PublicEvent/Details.cshtml", vm);
        }
    }
}

