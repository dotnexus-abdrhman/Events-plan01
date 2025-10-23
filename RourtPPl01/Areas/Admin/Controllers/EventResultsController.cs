using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.Admin.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class EventResultsController : Controller
    {
        private readonly IMinaResultsService _resultsService;
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<EventResultsController> _logger;
        private readonly AppDbContext _db;
        private readonly IPdfExportService _pdf;
        private readonly IHostEnvironment _env;

        public EventResultsController(
            IMinaResultsService resultsService,
            IMinaEventsService eventsService,
            ILogger<EventResultsController> logger,
            AppDbContext db,
            IPdfExportService pdf,
            IHostEnvironment env)
        {
            _resultsService = resultsService;
            _eventsService = eventsService;
            _logger = logger;
            _db = db;
            _pdf = pdf;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Organizations()
        {
            var orgs = await _db.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
            return View(orgs);
        }

        [HttpGet]
        public async Task<IActionResult> OrganizationEvents(Guid id)
        {
            var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.OrganizationId == id);
            if (org == null) return NotFound();
            var events = await _db.Events.AsNoTracking().Where(e => e.OrganizationId == id).OrderByDescending(e => e.StartAt).ToListAsync();
            ViewBag.Organization = org;
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> ExportSummaryPdf(Guid eventId)
        {
            try
            {
                var bytes = await _pdf.ExportEventSummaryPdfAsync(eventId);
                var title = (await _eventsService.GetEventByIdAsync(eventId))?.Title ?? "event";
                return File(bytes, "application/pdf", $"Summary-{title}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تصدير ملف PDF الملخص للحدث {EventId}", eventId);
                TempData["Error"] = "تعذر تصدير الملف";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportDetailedPdf(Guid eventId)
        {
            try
            {
                var bytes = await _pdf.ExportEventDetailedPdfAsync(eventId);
                var title = (await _eventsService.GetEventByIdAsync(eventId))?.Title ?? "event";
                return File(bytes, "application/pdf", $"Details-{title}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تصدير ملف PDF التفصيلي للحدث {EventId}", eventId);
                TempData["Error"] = "تعذر تصدير الملف";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportCustomPdfResults(Guid eventId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                _logger.LogInformation("[EventResults] ExportCustomPdfResults for Event {EventId}", eventId);

                // حاول استخدام الملف المدمج الجاهز إن وجد
                var merged = await _db.Attachments.AsNoTracking()
                    .Where(a => a.EventId == eventId && a.Type == EvenDAL.Models.Shared.Enums.AttachmentType.CustomPdfMerged)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (merged != null && !string.IsNullOrWhiteSpace(merged.Path))
                {
                    var full = Path.Combine(_env.ContentRootPath, "wwwroot", merged.Path.TrimStart('/'));
                    _logger.LogInformation("[EventResults] Trying to serve pre-merged file at {Path}", full);
                    if (System.IO.File.Exists(full))
                    {
                        var readyBytes = await System.IO.File.ReadAllBytesAsync(full);
                        _logger.LogInformation("[EventResults] Serving pre-merged file. Size={Size}", readyBytes.Length);
                        var evt = await _eventsService.GetEventByIdAsync(eventId);
                        var title = evt?.Title ?? "event";
                        return File(readyBytes, "application/pdf", $"Custom-Results-{title}.pdf");
                    }
                    _logger.LogWarning("[EventResults] Pre-merged PDF record found but file missing: {Path}", full);
                }

                // تراجع إلى الدمج عند الطلب كحل احتياطي + إضافة QR افتراضي
                _logger.LogInformation("[EventResults] Fallback to on-demand merge for Event {EventId}", eventId);

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
                var fallbackOptions = new PdfExportOptions
                {
                    // Keep conservative contents similar to participants export
                    IncludeEventDetails = true,
                    IncludeSurveyAndResponses = false,
                    IncludeDiscussions = false,
                    IncludeSignatures = true,
                    IncludeSections = false,
                    IncludeAttachments = false,
                    BrandingFooterText = "منصة مينا لإدارة الفعاليات",

                    // QR defaults
                    QrCodeSize = 45,
                    QrCodePosition = "BottomLeft",
                    ShowQrCode = true,
                    ShowVerificationUrl = true,

                    VerificationUrlBase = baseUrl,
                    VerificationId = Guid.NewGuid(),
                    VerificationType = "CustomWithParticipants"
                };

                var bytes = await _pdf.ExportCustomMergedWithParticipantsPdfAsync(eventId, fallbackOptions);
                _logger.LogInformation("[EventResults] On-demand merged size = {Size}", bytes?.Length ?? 0);
                var evtFallback = await _eventsService.GetEventByIdAsync(eventId);
                var titleFallback = evtFallback?.Title ?? "event";
                return File(bytes, "application/pdf", $"Custom-Results-{titleFallback}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل تصدير PDF المخصص للحدث {EventId}", eventId);
                TempData["Error"] = "تعذر إنشاء ملف PDF المخصص.";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }


        // ============================================
        // GET: Admin/EventResults/ExportOptions?eventId=...
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ExportOptions(Guid eventId)
        {
            if (!await CanAccessEvent(eventId))
                return Forbid();

            var vm = new PdfExportOptionsVm
            {
                EventId = eventId,
                IncludeEventDetails = true,
                IncludeSurveyAndResponses = true,
                IncludeDiscussions = true,
                IncludeSignatures = true,
                IncludeSections = false,
                IncludeAttachments = false,
                UseOrganizationLogo = false,
                BrandingFooterText = "منصة مينا لإدارة الفعاليات"
            };
            return View(vm);
        }

        // ============================================
        // POST: Admin/EventResults/ExportResultsPdf
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportResultsPdf(PdfExportOptionsVm model)
        {
            if (!await CanAccessEvent(model.EventId))
                return Forbid();

            byte[]? logoBytes = null;
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.LogoFile.CopyToAsync(ms);
                logoBytes = ms.ToArray();
            }

            // Read optional background image
            byte[]? bgBytes = null;
            if (model.BackgroundImageFile != null && model.BackgroundImageFile.Length > 0)
            {
                using var bg = new MemoryStream();
                await model.BackgroundImageFile.CopyToAsync(bg);
                bgBytes = bg.ToArray();
            }

            var options = new PdfExportOptions
            {
                IncludeEventDetails = model.IncludeEventDetails,
                IncludeSurveyAndResponses = model.IncludeSurveyAndResponses,
                IncludeDiscussions = model.IncludeDiscussions,
                IncludeSignatures = model.IncludeSignatures,
                IncludeSections = model.IncludeSections,
                IncludeAttachments = model.IncludeAttachments,
                BrandingFooterText = string.IsNullOrWhiteSpace(model.BrandingFooterText)
                    ? "منصة مينا لإدارة الفعاليات"
                    : model.BrandingFooterText,
                LogoBytes = logoBytes,

                // Appearance
                BackgroundImageBytes = bgBytes,
                BackgroundOpacity = model.BackgroundOpacity,
                FontColorHex = string.IsNullOrWhiteSpace(model.FontColorHex) ? "#000000" : model.FontColorHex,
                FontFamily = string.IsNullOrWhiteSpace(model.FontFamily) ? null : model.FontFamily,
                BaseFontSize = model.BaseFontSize > 0 ? model.BaseFontSize : 11,
                TableHeaderBackgroundColorHex = string.IsNullOrWhiteSpace(model.TableHeaderBackgroundColorHex) ? null : model.TableHeaderBackgroundColorHex,

                // QR customization
                QrCodeSize = model.QrCodeSize > 0 ? model.QrCodeSize : 45,
                QrCodePosition = string.IsNullOrWhiteSpace(model.QrCodePosition) ? "BottomLeft" : model.QrCodePosition,
                ShowQrCode = model.ShowQrCode,
                ShowVerificationUrl = model.ShowVerificationUrl
            };

            // Prepare verification for this export
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
            options.VerificationUrlBase = baseUrl;
            options.VerificationId = Guid.NewGuid();
            options.VerificationType = "CustomResults";

            var evt = await _eventsService.GetEventByIdAsync(model.EventId);
            var fileName = $"Event_Results_{(evt?.Title ?? "").Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var bytes = await _pdf.ExportEventResultsPdfAsync(model.EventId, options);
            return File(bytes, "application/pdf", fileName);
        }

        // ============================================
        // GET: Admin/EventResults/ParticipantsTableOptions?eventId=...
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ParticipantsTableOptions(Guid eventId)
        {
            if (!await CanAccessEvent(eventId))
                return Forbid();

            var vm = new ParticipantsTableOptionsVm
            {
                EventId = eventId,
                FontFamily = null,
                BaseFontSize = 11,
                FontColorHex = "#000000",
                TableHeaderBackgroundColorHex = null
            };
            return View(vm);
        }

        // ============================================
        // POST: Admin/EventResults/ExportCustomPdfParticipants
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportCustomPdfParticipants(ParticipantsTableOptionsVm model)
        {
            if (!await CanAccessEvent(model.EventId))
                return Forbid();

            // Read optional background image (same approach as ExportResultsPdf)
            byte[]? bgBytes = null;
            if (model.BackgroundImageFile != null && model.BackgroundImageFile.Length > 0)
            {
                using var bg = new MemoryStream();
                await model.BackgroundImageFile.CopyToAsync(bg);
                bgBytes = bg.ToArray();
            }

            var options = new PdfExportOptions
            {
                IncludeEventDetails = true,
                IncludeSurveyAndResponses = false,
                IncludeDiscussions = false,
                IncludeSignatures = true,
                IncludeSections = false,
                IncludeAttachments = false,
                BrandingFooterText = "منصة مينا لإدارة الفعاليات",
                LogoBytes = null,

                // Appearance (copied from ExportResultsPdf)
                BackgroundImageBytes = bgBytes,
                BackgroundOpacity = model.BackgroundOpacity,
                FontFamily = string.IsNullOrWhiteSpace(model.FontFamily) ? null : model.FontFamily,
                BaseFontSize = model.BaseFontSize > 0 ? model.BaseFontSize : 11,
                FontColorHex = string.IsNullOrWhiteSpace(model.FontColorHex) ? "#000000" : model.FontColorHex,
                TableHeaderBackgroundColorHex = string.IsNullOrWhiteSpace(model.TableHeaderBackgroundColorHex) ? null : model.TableHeaderBackgroundColorHex,

                // QR customization
                QrCodeSize = model.QrCodeSize > 0 ? model.QrCodeSize : 45,
                QrCodePosition = string.IsNullOrWhiteSpace(model.QrCodePosition) ? "BottomLeft" : model.QrCodePosition,
                ShowQrCode = model.ShowQrCode,
                ShowVerificationUrl = model.ShowVerificationUrl
            };

            // Prepare verification for merged export
            var baseUrl2 = $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
            options.VerificationUrlBase = baseUrl2;
            options.VerificationId = Guid.NewGuid();
            options.VerificationType = "CustomWithParticipants";

            var bytes = await _pdf.ExportCustomMergedWithParticipantsPdfAsync(model.EventId, options);
            var evt = await _eventsService.GetEventByIdAsync(model.EventId);
            var title = evt?.Title ?? "event";
            return File(bytes, "application/pdf", $"Custom-Results-{title}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportUserPdf(Guid eventId, Guid userId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }
                var bytes = await _pdf.ExportUserResultPdfAsync(eventId, userId);
                var evt = await _eventsService.GetEventByIdAsync(eventId);
                var title = evt?.Title ?? "event";
                return File(bytes, "application/pdf", $"User-{userId}-Details-{title}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تصدير ملف PDF للمشارك {UserId} في الحدث {EventId}", userId, eventId);
                TempData["Error"] = "تعذر تصدير الملف";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }



        // ============================================
        // GET: Admin/EventResults/Summary/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Summary(Guid eventId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                var results = await _resultsService.GetEventResultsAsync(eventId);

                // Compute lightweight statistics inline to avoid DbContext concurrency issues
                var uniqueParticipants = await _db.SurveyAnswers
                    .Where(a => a.EventId == eventId)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();
                var totalSurveyAnswers = await _db.SurveyAnswers.CountAsync(a => a.EventId == eventId);
                var totalDiscussionReplies = await _db.DiscussionReplies.CountAsync(r => r.Discussion.EventId == eventId);
                var totalSignatures = await _db.UserSignatures.CountAsync(s => s.EventId == eventId);

                var eventDto = await _eventsService.GetEventByIdAsync(eventId);

                var hasCustomPdfs = await _db.Attachments.AsNoTracking().AnyAsync(a => a.EventId == eventId && a.Type == EvenDAL.Models.Shared.Enums.AttachmentType.CustomPdf);

                var vm = new ResultsSummaryViewModel
                {
                    EventId = eventId,
                    EventTitle = eventDto?.Title ?? "غير معروف",
                    Statistics = new EventStatisticsViewModel
                    {
                        UniqueParticipants = uniqueParticipants,
                        TotalSurveyAnswers = totalSurveyAnswers,
                        TotalDiscussionReplies = totalDiscussionReplies,
                        TotalSignatures = totalSignatures
                    },
                    SurveyResults = results.SurveyResults.Select(sr => new SurveyResultViewModel
                    {
                        SurveyTitle = sr.SurveyTitle,
                        Questions = sr.QuestionResults.Select(qr => new QuestionResultViewModel
                        {
                            QuestionText = qr.QuestionText,
                            QuestionType = qr.QuestionType,
                            TotalAnswers = qr.TotalAnswers,
                            Options = qr.OptionResults.Select(or => new OptionResultViewModel
                            {
                                OptionText = or.OptionText,
                                Count = or.Count,
                                Percentage = decimal.Parse(or.Percentage.TrimEnd('%'))
                            }).ToList()
                        }).ToList()
                    }).ToList(),
                    UserResponses = new List<UserResponseViewModel>(),
                    HasCustomPdfs = hasCustomPdfs
                };

                // Build detailed user responses
                var participantIds = await _db.SurveyAnswers.Where(a => a.EventId == eventId).Select(a => a.UserId).Distinct()
                    .Union(_db.DiscussionReplies.Where(r => r.Discussion.EventId == eventId).Select(r => r.UserId))
                    .Union(_db.UserSignatures.Where(s => s.EventId == eventId).Select(s => s.UserId))
                    .Distinct()
                    .ToListAsync();

                var guestUserIds = await _db.PublicEventGuests.AsNoTracking()
                    .Where(g => g.EventId == eventId)
                    .Select(g => g.UserId)
                    .ToListAsync();

                // Include guests even if they didn't submit survey/discussion/signature
                participantIds = participantIds
                    .Union(guestUserIds)
                    .Distinct()
                    .ToList();

                var users = await _db.Users.AsNoTracking().Where(u => participantIds.Contains(u.UserId)).ToListAsync();

                foreach (var uid in participantIds)
                {
                    var user = users.FirstOrDefault(u => u.UserId == uid);
                    var isGuest = guestUserIds.Contains(uid);
                    var answers = await _db.SurveyAnswers.AsNoTracking()
                        .Where(a => a.EventId == eventId && a.UserId == uid)
                        .Include(a => a.Question)
                        .Include(a => a.SelectedOptions)
                            .ThenInclude(so => so.Option)
                        .ToListAsync();

                    var replies = await _db.DiscussionReplies.AsNoTracking()
                        .Where(r => r.UserId == uid && r.Discussion.EventId == eventId)
                        .Include(r => r.Discussion)
                        .ToListAsync();

                    var signature = await _db.UserSignatures.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.EventId == eventId && s.UserId == uid);

                    var submittedAt = new DateTime[]
                    {
                        answers.OrderByDescending(a=>a.CreatedAt).FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
                        replies.OrderByDescending(r=>r.CreatedAt).FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
                        signature?.CreatedAt ?? DateTime.MinValue
                    }.Max();

                    vm.UserResponses.Add(new UserResponseViewModel
                    {
                        UserId = uid,
                        UserName = user?.FullName ?? "-",
                        SubmittedAt = submittedAt == DateTime.MinValue ? DateTime.UtcNow : submittedAt,
                        HasSignature = signature != null,
                        SignaturePath = signature?.ImagePath,
                        HasSurveyAnswers = answers.Any(),
                        HasDiscussionReplies = replies.Any(),
                        HasTableData = false,
                        IsGuest = isGuest,
                        SurveyAnswers = answers.Select(a => new Areas.Admin.ViewModels.SurveyAnswerViewModel
                        {
                            QuestionText = a.Question?.Text ?? string.Empty,
                            SelectedOptions = a.SelectedOptions.Select(so => so.Option.Text).ToList()
                        }).ToList(),
                        DiscussionReplies = replies.Select(r => new Areas.Admin.ViewModels.DiscussionReplyViewModel
                        {
                            DiscussionTitle = r.Discussion?.Title ?? string.Empty,
                            ReplyText = r.Body
                        }).ToList()
                    });
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل ملخص النتائج للحدث {EventId}", eventId);
                TempData["Error"] = "حدث خطأ أثناء تحميل النتائج";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }
        }

        // ============================================
        // GET: Admin/EventResults/Details/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid eventId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                var results = await _resultsService.GetEventResultsAsync(eventId);

                // EventResultsSummaryDto doesn't have UserResponses property
                // We'll create a simple view for now
                var vm = new ResultsDetailsViewModel
                {
                    EventId = eventId,
                    UserResponses = new List<UserResponseViewModel>()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تفاصيل النتائج للحدث {EventId}", eventId);
                TempData["Error"] = "حدث خطأ أثناء تحميل التفاصيل";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }

        // ============================================
        // GET: Admin/EventResults/ExportPDF/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ExportPDF(Guid eventId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                // TODO: Implement PDF export using a library like QuestPDF or iTextSharp
                // For now, return a simple message
                TempData["Info"] = "تصدير PDF سيتم تنفيذه في المرحلة القادمة";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير PDF للحدث {EventId}", eventId);
                TempData["Error"] = "حدث خطأ أثناء تصدير PDF";
                return RedirectToAction(nameof(Summary), new { eventId });
            }
        }

        // ============================================
        // Helper Methods
        // ============================================
        private async Task<bool> CanAccessEvent(Guid eventId)
        {
            var orgId = GetOrganizationId();
            var eventDto = await _eventsService.GetEventByIdAsync(eventId);
            // اسمح للمشرف (Admin) دائمًا
            if (User.IsInRole("PlatformAdmin")) return eventDto != null;
            return eventDto != null && eventDto.OrganizationId == orgId;
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }
}

