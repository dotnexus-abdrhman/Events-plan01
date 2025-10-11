using EventPl.Dto.Mina;
using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.UserPortal.ViewModels;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;

namespace RourtPPl01.Areas.UserPortal.Controllers
{
    [Area("UserPortal")]
    [Authorize(Roles = "Attendee,Organizer,Observer")]
    public class EventParticipationController : Controller
    {
        private readonly IMinaEventsService _eventsService;
        private readonly ISectionsService _sectionsService;
        private readonly ISurveysService _surveysService;
        private readonly IDiscussionsService _discussionsService;
        private readonly ITableBlocksService _tablesService;
        private readonly IAttachmentsService _attachmentsService;
        private readonly ISignaturesService _signaturesService;
        private readonly ICrudService<UserDto, Guid> _usersService;
        private readonly ILogger<EventParticipationController> _logger;

        public EventParticipationController(
            IMinaEventsService eventsService,
            ISectionsService sectionsService,
            ISurveysService surveysService,
            IDiscussionsService discussionsService,
            ITableBlocksService tablesService,
            IAttachmentsService attachmentsService,
            ISignaturesService signaturesService,
            ICrudService<UserDto, Guid> usersService,
            ILogger<EventParticipationController> logger)
        {
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

        // ============================================
        // GET: UserPortal/EventParticipation/Details/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var userId = GetUserId();
                // Use OrganizationId from claims for authorization comparisons
                var orgId = GetOrganizationId();

                var eventDto = await _eventsService.GetEventByIdAsync(id);
                if (eventDto == null)
                {
                    TempData["Error"] = "الحدث غير موجود";
                    return RedirectToAction("Index", "MyEvents");
                }

                // التحقق من الصلاحية
                if (eventDto.OrganizationId != orgId)
                {
                    return Forbid();
                }

                // تحميل جميع المكونات
                var sections = await _sectionsService.GetEventSectionsAsync(id);
                var surveys = await _surveysService.GetEventSurveysAsync(id);
                var discussions = await _discussionsService.GetEventDiscussionsAsync(id);
                var tables = await _tablesService.GetEventTablesAsync(id);
                var attachments = await _attachmentsService.GetEventAttachmentsAsync(id);

                // التحقق من التوقيع
                var hasSignature = await _signaturesService.HasUserSignedAsync(id, userId);

                // التحقق من المشاركة السابقة (مرة واحدة فقط)
                var hasUserParticipated = (await _surveysService.HasUserAnsweredAsync(id, userId))
                    || (await _signaturesService.HasUserSignedAsync(id, userId));

                var vm = new EventDetailsViewModel
                {
                    EventId = eventDto.EventId,
                    EventTitle = eventDto.Title,
                    EventDescription = eventDto.Description ?? string.Empty,
                    StartAt = eventDto.StartAt,
                    EndAt = eventDto.EndAt,
                    RequireSignature = eventDto.RequireSignature,
                    HasUserSigned = hasSignature,
                    HasUserParticipated = hasUserParticipated,
                    Sections = sections
                        .OrderBy(s => s.Order)
                        .Select(s => new SectionViewModel
                        {
                            SectionId = s.SectionId,
                            Title = s.Title,
                            Body = s.Body,
                            Order = s.Order,
                            Decisions = s.Decisions?.Select(d => new DecisionViewModel
                            {
                                Title = d.Title,
                                Order = d.Order,
                                Items = d.Items?.Select(i => new DecisionItemViewModel
                                {
                                    Text = i.Text,
                                    Order = i.Order
                                }).ToList() ?? new()
                            }).ToList() ?? new(),
                            // مكونات البند
                            Surveys = surveys.Where(x => x.SectionId == s.SectionId).Select(ss => new SurveyViewModel
                            {
                                SurveyId = ss.SurveyId,
                                Title = ss.Title,
                                Description = null,
                                Questions = ss.Questions?.Select(q => new QuestionViewModel
                                {
                                    SurveyQuestionId = q.SurveyQuestionId,
                                    Text = q.Text,
                                    Type = Enum.TryParse<SurveyQuestionType>(q.Type, out var qType) ? qType : SurveyQuestionType.Single,
                                    IsRequired = q.IsRequired,
                                    Options = q.Options?.Select(o => new OptionViewModel
                                    {
                                        SurveyOptionId = o.SurveyOptionId,
                                        Text = o.Text
                                    }).ToList() ?? new()
                                }).ToList() ?? new()
                            }).ToList(),
                            Discussions = discussions.Where(x => x.SectionId == s.SectionId).Select(dd => new DiscussionViewModel
                            {
                                DiscussionId = dd.DiscussionId,
                                Title = dd.Title,
                                Purpose = dd.Purpose
                            }).ToList(),
                            Tables = tables.Where(x => x.SectionId == s.SectionId).Select(tt => new TableViewModel
                            {
                                Title = tt.Title,
                                HasHeader = tt.HasHeader,
                                Rows = tt.TableData?.Rows?.Select(row =>
                                    row.Cells.Select(cell => new TableCellViewModel { Value = cell }).ToList()
                                ).ToList()
                            }).ToList(),
                            Attachments = attachments.Where(x => x.SectionId == s.SectionId).Select(aa => new AttachmentViewModel
                            {
                                AttachmentId = aa.AttachmentId,
                                FileName = aa.FileName,
                                Path = aa.Path,
                                Type = Enum.TryParse<AttachmentType>(aa.Type, out var aTypeS) ? aTypeS : AttachmentType.Image,
                                Order = aa.Order
                            }).ToList()
                        }).ToList(),
                    // المكونات العامة بعد جميع البنود
                    Surveys = surveys.Where(s => s.SectionId == null).Select(s => new SurveyViewModel
                    {
                        SurveyId = s.SurveyId,
                        Title = s.Title,
                        Description = null,
                        Questions = s.Questions?.Select(q => new QuestionViewModel
                        {
                            SurveyQuestionId = q.SurveyQuestionId,
                            Text = q.Text,
                            Type = Enum.TryParse<SurveyQuestionType>(q.Type, out var qType) ? qType : SurveyQuestionType.Single,
                            IsRequired = q.IsRequired,
                            Options = q.Options?.Select(o => new OptionViewModel
                            {
                                SurveyOptionId = o.SurveyOptionId,
                                Text = o.Text
                            }).ToList() ?? new()
                        }).ToList() ?? new()
                    }).ToList(),
                    Discussions = discussions.Where(d => d.SectionId == null).Select(d => new DiscussionViewModel
                    {
                        DiscussionId = d.DiscussionId,
                        Title = d.Title,
                        Purpose = d.Purpose
                    }).ToList(),
                    Tables = tables.Where(t => t.SectionId == null).Select(t => new TableViewModel
                    {
                        Title = t.Title,
                        HasHeader = t.HasHeader,
                        Rows = t.TableData?.Rows?.Select(row =>
                            row.Cells.Select(cell => new TableCellViewModel { Value = cell }).ToList()
                        ).ToList()
                    }).ToList(),
                    Attachments = attachments.Where(a => a.SectionId == null).Select(a => new AttachmentViewModel
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        Path = a.Path,
                        Type = Enum.TryParse<AttachmentType>(a.Type, out var aType) ? aType : AttachmentType.Image,
                        Order = a.Order
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تفاصيل الحدث {EventId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل تفاصيل الحدث";
                return RedirectToAction("Index", "MyEvents");
            }
        }

        // ============================================
        // POST: UserPortal/EventParticipation/Submit
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitResponseViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "البيانات غير صحيحة";
                return RedirectToAction(nameof(Details), new { id = vm.EventId });
            }

            try
            {
                var userId = GetUserId();
                // Use OrganizationId from claims for authorization comparisons
                var orgId = GetOrganizationId();

                var eventDto = await _eventsService.GetEventByIdAsync(vm.EventId);
                if (eventDto == null)
                {
                    TempData["Error"] = "الحدث غير موجود";
                    return RedirectToAction("Index", "MyEvents");
                }

                if (eventDto.OrganizationId != orgId)
                {
                    return Forbid();
                }

                // منع الإرسال المتكرر
                var alreadyParticipated = (await _surveysService.HasUserAnsweredAsync(vm.EventId, userId))
                    || (await _signaturesService.HasUserSignedAsync(vm.EventId, userId));
                if (alreadyParticipated)
                {
                    TempData["Error"] = "لقد سبق أن شاركت في هذا الحدث";
                    return RedirectToAction(nameof(Details), new { id = vm.EventId });
                }

                // Validation: التوقيع إذا كان مطلوباً
                if (eventDto.RequireSignature && string.IsNullOrWhiteSpace(vm.SignatureData))
                {
                    TempData["Error"] = "التوقيع مطلوب لإرسال الردود";
                    return RedirectToAction(nameof(Details), new { id = vm.EventId });
                }

                // Parse survey answers from Request.Form (supports radio and checkbox naming patterns)
                var questionAnswers = new List<QuestionAnswerDto>();
                foreach (var key in Request.Form.Keys)
                {
                    if (!key.StartsWith("SurveyAnswers[")) continue;
                    // key can be: SurveyAnswers[QUESTION_GUID] or SurveyAnswers[QUESTION_GUID][]
                    var start = key.IndexOf('[') + 1;
                    var end = key.IndexOf(']');
                    if (start <= 0 || end <= start) continue;
                    var qidStr = key.Substring(start, end - start);
                    if (!Guid.TryParse(qidStr, out var questionId)) continue;
                    var values = Request.Form[key];
                    // values can be one or many
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
                if (eventDto.RequireSignature && !string.IsNullOrWhiteSpace(vm.SignatureData))
                {
                    await _signaturesService.SaveSignatureAsync(new SaveSignatureRequest
                    {
                        EventId = vm.EventId,
                        UserId = userId,
                        SignatureData = vm.SignatureData
                    });
                }

                TempData["Success"] = "تم إرسال ردك بنجاح";
                return RedirectToAction(nameof(Confirmation), new { eventId = vm.EventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال الردود للحدث {EventId}", vm.EventId);
                TempData["Error"] = "حدث خطأ أثناء إرسال الردود";
                return RedirectToAction(nameof(Details), new { id = vm.EventId });
            }
        }

        // ============================================
        // GET: UserPortal/EventParticipation/Confirmation/eventId
        // ============================================
        [HttpGet]
        public IActionResult Confirmation(Guid eventId)
        {
            var vm = new ConfirmationViewModel
            {
                EventId = eventId,
                Message = "تم إرسال ردك بنجاح. شكراً لمشاركتك!"
            };

            return View(vm);
        }

        // ============================================
        // Helper Methods
        // ============================================
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }
}

