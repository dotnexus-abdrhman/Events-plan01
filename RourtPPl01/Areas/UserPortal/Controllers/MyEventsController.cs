using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.UserPortal.ViewModels;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;

using RouteDAl.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using EvenDAL.Models.Classes;

namespace RourtPPl01.Areas.UserPortal.Controllers
{
    [Area("UserPortal")]
    [Authorize(Roles = "Attendee,Organizer,Observer")]
    public class MyEventsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<MyEventsController> _logger;

        public MyEventsController(
            AppDbContext db,
            IMinaEventsService eventsService,
            ILogger<MyEventsController> logger)
        {
            _db = db;
            _eventsService = eventsService;
            _logger = logger;
        }

        // ============================================
        // GET: UserPortal/MyEvents
        // ============================================
        [HttpGet("/UserPortal/MyEvents")]
        [HttpGet("/UserPortal/Events")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetUserId();
                var orgId = GetOrganizationId();

                var events = await _eventsService.GetUserEventsAsync(userId, orgId);

                // استبعاد الأحداث المخفية من هذا المستخدم
                var hiddenIds = await _db.UserHiddenEvents
                    .Where(h => h.UserId == userId)
                    .Select(h => h.EventId)
                    .ToListAsync();
                events = events.Where(e => !hiddenIds.Contains(e.EventId)).ToList();

                var vm = new MyEventsIndexViewModel
                {
                    Events = events.Select(e => new MyEventItemViewModel
                    {
                        EventId = e.EventId,
                        Title = e.Title,
                        Description = e.Description,
                        StartAt = e.StartAt,
                        EndAt = e.EndAt,
                        Status = Enum.TryParse<EventStatus>(e.StatusName, out var status) ? status : EventStatus.Draft,
                        RequireSignature = e.RequireSignature,
                        SectionsCount = 0,
                        SurveysCount = 0,
                        DiscussionsCount = 0
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل قائمة الأحداث للمستخدم");
                TempData["Error"] = "حدث خطأ أثناء تحميل الأحداث";
                return View(new MyEventsIndexViewModel());
            }
        }

        // ============================================
        // POST: إخفاء حدث واحد للمستخدم الحالي فقط
        // ============================================
        [HttpPost("/UserPortal/MyEvents/Hide")]
        [HttpPost("/UserPortal/Events/Hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(Guid eventId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty) return Forbid();

                var exists = await _db.UserHiddenEvents.AnyAsync(h => h.UserId == userId && h.EventId == eventId);
                if (!exists)
                {
                    _db.UserHiddenEvents.Add(new UserHiddenEvent { UserId = userId, EventId = eventId, HiddenAt = DateTime.UtcNow });
                    await _db.SaveChangesAsync();
                }
                TempData["Success"] = "تم حذف الحدث بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إخفاء الحدث للمستخدم");
                TempData["Error"] = "تعذر حذف الحدث";
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================
        // POST: إخفاء جميع أحداث المستخدم الحالي فقط
        // ============================================
        [HttpPost("/UserPortal/MyEvents/HideAll")]
        [HttpPost("/UserPortal/Events/HideAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideAll()
        {
            try
            {
                var userId = GetUserId();
                var orgId = GetOrganizationId();
                if (userId == Guid.Empty) return Forbid();

                var events = await _eventsService.GetUserEventsAsync(userId, orgId);
                var allIds = events.Select(e => e.EventId).ToList();

                var existing = await _db.UserHiddenEvents
                    .Where(h => h.UserId == userId)
                    .Select(h => h.EventId)
                    .ToListAsync();

                var toInsert = allIds.Except(existing).Select(id => new UserHiddenEvent
                {
                    UserId = userId,
                    EventId = id,
                    HiddenAt = DateTime.UtcNow
                });

                if (toInsert.Any())
                {
                    await _db.UserHiddenEvents.AddRangeAsync(toInsert);
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = "تم حذف جميع الأحداث بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إخفاء جميع الأحداث للمستخدم");
                TempData["Error"] = "تعذر حذف جميع الأحداث";
            }
            return RedirectToAction(nameof(Index));
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

