using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.UserPortal.ViewModels;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;

using RouteDAl.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using EvenDAL.Models.Classes;
using Microsoft.Extensions.Caching.Memory;



namespace RourtPPl01.Areas.UserPortal.Controllers
{
    [Area("UserPortal")]
    [Authorize]
    public class MyEventsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<MyEventsController> _logger;
        private readonly IMemoryCache _cache;

        public MyEventsController(
            AppDbContext db,
            IMinaEventsService eventsService,
            ILogger<MyEventsController> logger,
            IMemoryCache cache)
        {
            _db = db;
            _eventsService = eventsService;
            _logger = logger;
            _cache = cache;
        }

        // ============================================
        // GET: UserPortal/MyEvents
        // ============================================
        [HttpGet("/UserPortal/MyEvents")]
        [HttpGet("/UserPortal/Events")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, NoStore = false)]
        public async Task<IActionResult> Index(string? search)
        {
            try
            {
                var swTotal = System.Diagnostics.Stopwatch.StartNew();

                var userId = GetUserId();
                var orgId = GetOrganizationId();

                // جلب أحداث المستخدم بكفاءة مباشرة من قاعدة البيانات مع استبعاد المخفية داخل الاستعلام
                // منطق الاستحقاق:
                // - إذا كان الحدث بث عام IsBroadcast => يظهر للجميع
                // - إذا كان للحدث مدعوون أفراد (EventInvitedUsers) => يظهر فقط للمستخدمين المدعوين
                // - إذا لم يكن للحدث مدعوون أفراد => يتبع منطق المجموعة (OrganizationId + Active)
                // تحسين الأداء والدقة: حساب المجموعات الثلاث بشكل منفصل ثم توحيدها
                var invitedEventIds = _db.EventInvitedUsers.AsNoTracking()
                    .Where(i => i.UserId == userId)
                    .Select(i => i.EventId);

                var invitedQuery = _db.Events.AsNoTracking()
                    .Where(e => invitedEventIds.Contains(e.EventId));

                var broadcastQuery = _db.Events.AsNoTracking()
                    .Where(e => e.IsBroadcast);

                var orgQuery = _db.Events.AsNoTracking()
                    .Where(e => !_db.EventInvitedUsers.AsNoTracking().Any(i => i.EventId == e.EventId)
                                && e.OrganizationId == orgId
                                && e.Status == EventStatus.Active);

                var qry = broadcastQuery
                    .Union(invitedQuery)
                    .Union(orgQuery)
                    .Where(e => !_db.UserHiddenEvents.AsNoTracking().Any(h => h.UserId == userId && h.EventId == e.EventId));

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    qry = qry.Where(e => (e.Title ?? string.Empty).Contains(term));
                }

                var swQuery = System.Diagnostics.Stopwatch.StartNew();
                var items = await qry
                    .OrderByDescending(e => e.StartAt)
                    .Select(e => new MyEventItemViewModel
                    {
                        EventId = e.EventId,
                        Title = e.Title ?? string.Empty,
                        Description = e.Description,
                        StartAt = e.StartAt,
                        EndAt = e.EndAt,
                        Status = e.Status,
                        RequireSignature = e.RequireSignature,
                        SectionsCount = 0,
                        SurveysCount = 0,
                        DiscussionsCount = 0
                    })
                    .ToListAsync();
                swQuery.Stop();

                var vm = new MyEventsIndexViewModel
                {
                    Events = items,
                    SearchTerm = search
                };

                // Surface recent broadcast title from cache (set by Admin flow/middleware) for tests and hints
                try
                {
                    if (!_cache.TryGetValue<string>("recent-broadcast-title", out var rbTitle) || string.IsNullOrWhiteSpace(rbTitle))
                    {
                        // Fallback on first miss only: cheap query to warm the cache, no heavy includes
                        rbTitle = await _db.Events.AsNoTracking()
                            .Where(e => e.IsBroadcast)
                            .OrderByDescending(e => e.CreatedAt)
                            .Select(e => e.Title)
                            .FirstOrDefaultAsync();
                        try
                        {
                            using (var entry = _cache.CreateEntry("recent-broadcast-title"))
                            {
                                entry.Value = rbTitle ?? string.Empty;
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                            }
                        }
                        catch { /* non-fatal cache set */ }
                    }
                    if (!string.IsNullOrWhiteSpace(rbTitle))
                    {
                        ViewBag.RecentBroadcastTitle = rbTitle;
                    }
                }
                catch { }

                swTotal.Stop();
                var previewTitles = string.Join(" | ", (vm.Events ?? new List<MyEventItemViewModel>()).Take(5).Select(e => e.Title));
                _logger.LogInformation("MyEvents Index loaded {Count} items. Query {QueryMs} ms, Total {TotalMs} ms, Top5: {Titles}", vm.Events?.Count ?? 0, swQuery.ElapsedMilliseconds, swTotal.ElapsedMilliseconds, previewTitles);

                // تأكيد الترميز: إجبار تعيين الترميز UTF-8 حتى يقرأ HttpClient العربية بشكل صحيح في الاختبارات
                Response.ContentType = "text/html; charset=utf-8";
                _logger.LogInformation("CT header: {CT}", Response.ContentType);
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
                    .AsNoTracking()
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

