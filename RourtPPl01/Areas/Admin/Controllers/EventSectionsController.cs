using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.Admin.ViewModels;
using System.Security.Claims;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventSectionsController : Controller
    {
        private readonly ISectionsService _sectionsService;
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<EventSectionsController> _logger;

        public EventSectionsController(
            ISectionsService sectionsService,
            IMinaEventsService eventsService,
            ILogger<EventSectionsController> logger)
        {
            _sectionsService = sectionsService;
            _eventsService = eventsService;
            _logger = logger;
        }

        // ============================================
        // GET: Admin/EventSections/Manage/eventId
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Manage(Guid eventId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!await CanAccessEvent(eventId))
                {
                    return Forbid();
                }

                var sections = await _sectionsService.GetEventSectionsAsync(eventId);

                var vm = new ManageSectionsViewModel
                {
                    EventId = eventId,
                    Sections = sections.Select(s => new SectionItemViewModel
                    {
                        SectionId = s.SectionId,
                        Title = s.Title,
                        Body = s.Body,
                        Order = s.Order,
                        DecisionsCount = s.Decisions?.Count ?? 0
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل البنود للحدث {EventId}", eventId);
                TempData["Error"] = "حدث خطأ أثناء تحميل البنود";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }
        }

        // ============================================
        // POST: Admin/EventSections/AddSection
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(AddSectionViewModel vm)
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

                var dto = new SectionDto
                {
                    EventId = vm.EventId,
                    Title = vm.Title.Trim(),
                    Body = vm.Body?.Trim() ?? string.Empty
                };

                var created = await _sectionsService.CreateSectionAsync(dto);

                return Json(new { success = true, message = "تم إضافة البند بنجاح", sectionId = created.SectionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة بند للحدث {EventId}", vm.EventId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة البند" });
            }
        }

        // ============================================
        // POST: Admin/EventSections/UpdateSection
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSection(UpdateSectionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                var section = await _sectionsService.GetSectionByIdAsync(vm.SectionId);
                if (section == null)
                {
                    return Json(new { success = false, message = "البند غير موجود" });
                }

                if (!await CanAccessEvent(section.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                section.Title = vm.Title.Trim();
                section.Body = vm.Body?.Trim() ?? string.Empty;

                var success = await _sectionsService.UpdateSectionAsync(section);

                if (success)
                {
                    return Json(new { success = true, message = "تم تحديث البند بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل تحديث البند" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث البند {SectionId}", vm.SectionId);
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث البند" });
            }
        }

        // ============================================
        // POST: Admin/EventSections/DeleteSection
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(Guid sectionId)
        {
            try
            {
                var section = await _sectionsService.GetSectionByIdAsync(sectionId);
                if (section == null)
                {
                    return Json(new { success = false, message = "البند غير موجود" });
                }

                if (!await CanAccessEvent(section.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var success = await _sectionsService.DeleteSectionAsync(sectionId);

                if (success)
                {
                    return Json(new { success = true, message = "تم حذف البند بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل حذف البند" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف البند {SectionId}", sectionId);
                return Json(new { success = false, message = "حدث خطأ أثناء حذف البند" });
            }
        }

        // ============================================
        // POST: Admin/EventSections/ReorderSections
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderSections(Guid eventId, [FromBody] List<Guid> sectionIds)
        {
            try
            {
                if (!await CanAccessEvent(eventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var success = await _sectionsService.ReorderSectionsAsync(eventId, sectionIds);

                if (success)
                {
                    return Json(new { success = true, message = "تم إعادة ترتيب البنود بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل إعادة ترتيب البنود" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إعادة ترتيب البنود للحدث {EventId}", eventId);
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة الترتيب" });
            }
        }

        // ============================================
        // POST: Admin/EventSections/AddDecision
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDecision(AddDecisionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                var section = await _sectionsService.GetSectionByIdAsync(vm.SectionId);
                if (section == null)
                {
                    return Json(new { success = false, message = "البند غير موجود" });
                }

                if (!await CanAccessEvent(section.EventId))
                {
                    return Json(new { success = false, message = "غير مصرح لك بهذا الإجراء" });
                }

                var dto = new DecisionDto
                {
                    SectionId = vm.SectionId,
                    Title = vm.Title.Trim()
                };

                var created = await _sectionsService.AddDecisionAsync(dto);

                return Json(new { success = true, message = "تم إضافة القرار بنجاح", decisionId = created.DecisionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة قرار للبند {SectionId}", vm.SectionId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة القرار" });
            }
        }

        // ============================================
        // POST: Admin/EventSections/AddDecisionItem
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDecisionItem(AddDecisionItemViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                var dto = new DecisionItemDto
                {
                    DecisionId = vm.DecisionId,
                    Text = vm.Text.Trim()
                };

                var created = await _sectionsService.AddDecisionItemAsync(dto);

                return Json(new { success = true, message = "تم إضافة عنصر القرار بنجاح", itemId = created.DecisionItemId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة عنصر للقرار {DecisionId}", vm.DecisionId);
                return Json(new { success = false, message = "حدث خطأ أثناء إضافة العنصر" });
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
            if (User.IsInRole("Admin")) return eventDto != null;
            return eventDto != null && eventDto.OrganizationId == orgId;
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }
}

