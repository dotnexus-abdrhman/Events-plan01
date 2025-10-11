using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.Admin.ViewModels;
using System.Security.Claims;
using System;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventComponentsController : Controller
    {
        private readonly ISurveysService _surveysService;
        private readonly IDiscussionsService _discussionsService;
        private readonly ITableBlocksService _tablesService;
        private readonly IAttachmentsService _attachmentsService;
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<EventComponentsController> _logger;

        public EventComponentsController(
            ISurveysService surveysService,
            IDiscussionsService discussionsService,
            ITableBlocksService tablesService,
            IAttachmentsService attachmentsService,
            IMinaEventsService eventsService,
            ILogger<EventComponentsController> logger)
        {
            _surveysService = surveysService;
            _discussionsService = discussionsService;
            _tablesService = tablesService;
            _attachmentsService = attachmentsService;
            _eventsService = eventsService;
            _logger = logger;
        }

        // ============================================
        // GET: Admin/EventComponents/Manage/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Manage(Guid eventId)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                var surveys = await _surveysService.GetEventSurveysAsync(eventId);
                var discussions = await _discussionsService.GetEventDiscussionsAsync(eventId);
                var tables = await _tablesService.GetEventTablesAsync(eventId);
                var attachments = await _attachmentsService.GetEventAttachmentsAsync(eventId);

                var vm = new ManageComponentsViewModel
                {
                    EventId = eventId,
                    SurveysCount = surveys.Count,
                    DiscussionsCount = discussions.Count,
                    TablesCount = tables.Count,
                    AttachmentsCount = attachments.Count
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل المكونات للحدث {EventId}", eventId);
                TempData["Error"] = "حدث خطأ أثناء تحميل المكونات";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }
        }

        // ============================================
        // POST: Admin/EventComponents/AddSurvey
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSurvey(AddSurveyViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if (!await CanAccessEvent(vm.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var dto = new SurveyDto
                {
                    EventId = vm.EventId,
                    SectionId = vm.SectionId,
                    Title = vm.Title.Trim(),
                    IsActive = true
                };

                var created = await _surveysService.CreateSurveyAsync(dto);

                return Json(new { success = true, message = "تم إضافة الاستبيان بنجاح", surveyId = created.SurveyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة استبيان للحدث {EventId}", vm.EventId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة الاستبيان" });
            }
        }

        // ============================================
        // POST: Admin/EventComponents/AddDiscussion
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscussion(AddDiscussionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if (!await CanAccessEvent(vm.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var dto = new DiscussionDto
                {
                    EventId = vm.EventId,
                    SectionId = vm.SectionId,
                    Title = vm.Title.Trim(),
                    Purpose = vm.Purpose?.Trim() ?? string.Empty,
                    IsActive = true
                };

                var created = await _discussionsService.CreateDiscussionAsync(dto);

                return Json(new { success = true, message = "تم إضافة النقاش بنجاح", discussionId = created.DiscussionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة نقاش للحدث {EventId}", vm.EventId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة النقاش" });
            }
        }

        // ============================================
        // POST: Admin/EventComponents/AddTable
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTable(AddTableViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if (!await CanAccessEvent(vm.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var dto = new TableBlockDto
                {
                    EventId = vm.EventId,
                    SectionId = vm.SectionId,
                    Title = vm.Title.Trim(),
                    TableData = new TableDataDto
                    {
                        Rows = new List<TableRowDto>()
                    }
                };

                var created = await _tablesService.CreateTableAsync(dto);

                return Json(new { success = true, message = "تم إضافة الجدول بنجاح", tableId = created.TableBlockId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة جدول للحدث {EventId}", vm.EventId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة الجدول" });
            }
        }

        // ============================================
        // POST: Admin/EventComponents/UploadAttachment
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment([FromForm] UploadAttachmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if (!await CanAccessEvent(vm.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                if (vm.File == null || vm.File.Length == 0)
                {
                    return Json(new { success = false, message = "الملف مطلوب" });
                }

                // Validation: File size (max 10MB)
                if (vm.File.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "حجم الملف يجب ألا يتجاوز 10 ميجابايت" });
                }

                // Validation: File type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var extension = Path.GetExtension(vm.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "نوع الملف غير مدعوم. الأنواع المسموحة: JPG, PNG, GIF, PDF" });
                }

                // Read file data
                byte[] fileData;
                using (var ms = new MemoryStream())
                {
                    await vm.File.CopyToAsync(ms);
                    fileData = ms.ToArray();
                }

                var request = new UploadAttachmentRequest
                {
                    EventId = vm.EventId,
                    SectionId = vm.SectionId,
                    FileName = vm.File.FileName,
                    Type = extension == ".pdf" ? "Pdf" : "Image",
                    FileData = fileData
                };

                var created = await _attachmentsService.UploadAttachmentAsync(request);

                return Json(new { success = true, message = "تم رفع الملف بنجاح", attachmentId = created.AttachmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في رفع ملف للحدث {EventId}", vm.EventId);
                return Json(new { success = false, message = "حدث خطأ أثناء رفع الملف" });
            }
        }

        // ============================================
        // POST: Admin/EventComponents/DeleteComponent
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComponent(string type, Guid id)
        {
            try
            {
                bool success = type switch
                {
                    "survey" => await _surveysService.DeleteSurveyAsync(id),
                    "discussion" => await _discussionsService.DeleteDiscussionAsync(id),
                    "table" => await _tablesService.DeleteTableAsync(id),
                    "attachment" => await _attachmentsService.DeleteAttachmentAsync(id),
                    _ => false
                };

                if (success)
                {
                    return Json(new { success = true, message = "تم الحذف بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل الحذف" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف المكون {Type} {Id}", type, id);
                return Json(new { success = false, message = "حدث خطأ أثناء الحذف" });
            }
        }

        // ============================================
        // Helper Methods
        // ============================================
        private async Task<bool> CanAccessEvent(Guid eventId)
        {
            var orgId = GetOrganizationId();
            var eventDto = await _eventsService.GetEventByIdAsync(eventId);
            // 2733452d 444445343141 (Admin) 2f2726454b27
            if (User.IsInRole("Admin")) return eventDto != null;
            return eventDto != null && eventDto.OrganizationId == orgId;
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }

        private bool IsAjaxRequest()
        {
            var xrw = Request?.Headers?["X-Requested-With"].ToString();
            var accept = Request?.Headers?["Accept"].ToString() ?? string.Empty;
            return string.Equals(xrw, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                   || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}

