using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteDAl.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;
using EventPl.Services.Interface;
using EventPl.Dto;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DashboardController> _logger;
        private readonly ICrudService<UserDto, Guid> _usersService;
        private readonly ICrudService<OrganizationDto, Guid> _orgsService;
        private readonly IMinaEventsService _eventsService;

        public DashboardController(AppDbContext db,
            ILogger<DashboardController> logger,
            ICrudService<UserDto, Guid> usersService,
            ICrudService<OrganizationDto, Guid> orgsService,
            IMinaEventsService eventsService)
        {
            _db = db;
            _logger = logger;
            _usersService = usersService;
            _orgsService = orgsService;
            _eventsService = eventsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orgId = GetOrganizationId();

                // المدير العام يشاهد إحصاءات المنصة بالكامل بغض النظر عن OrganizationId
                var isPlatformScope = User.IsInRole("Admin") || orgId == Guid.Empty;

                // Counts computed directly from DB to avoid service-side filters and ensure accuracy
                var totalEvents = isPlatformScope
                    ? await _db.Events.CountAsync()
                    : await _db.Events.CountAsync(e => e.OrganizationId == orgId);

                var activeEvents = isPlatformScope
                    ? await _db.Events.CountAsync(e => e.Status == EventStatus.Active)
                    : await _db.Events.CountAsync(e => e.OrganizationId == orgId && e.Status == EventStatus.Active);

                var totalUsers = isPlatformScope
                    ? await _db.Users.CountAsync()
                    : await _db.Users.CountAsync(u => u.OrganizationId == orgId);

                var totalOrgs = await _db.Organizations.CountAsync();

                var stats = new DashboardViewModel
                {
                    TotalEvents = totalEvents,
                    ActiveEvents = activeEvents,
                    TotalUsers = totalUsers,
                    TotalOrganizations = totalOrgs,

                    // بقية الإحصاءات (ليست معروضة في الواجهة الآن، لكنها صحيحة)
                    TotalSurveys = isPlatformScope ? await _db.Surveys.CountAsync() : await _db.Surveys.CountAsync(s => s.Event.OrganizationId == orgId),
                    TotalDiscussions = isPlatformScope ? await _db.Discussions.CountAsync() : await _db.Discussions.CountAsync(d => d.Event.OrganizationId == orgId),
                    TotalTables = isPlatformScope ? await _db.TableBlocks.CountAsync() : await _db.TableBlocks.CountAsync(t => t.Event.OrganizationId == orgId),
                    TotalAttachments = isPlatformScope ? await _db.Attachments.CountAsync() : await _db.Attachments.CountAsync(a => a.Event.OrganizationId == orgId),
                    TotalSignatures = isPlatformScope ? await _db.UserSignatures.CountAsync() : await _db.UserSignatures.CountAsync(s => s.Event.OrganizationId == orgId),

                    RecentEvents = await _db.Events
                        .Where(e => isPlatformScope || e.OrganizationId == orgId)
                        .OrderByDescending(e => e.CreatedAt)
                        .Take(5)
                        .Select(e => new RecentEventViewModel
                        {
                            EventId = e.EventId,
                            Title = e.Title,
                            StartAt = e.StartAt,
                            Status = e.Status.ToString()
                        })
                        .ToListAsync()
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل لوحة التحكم");
                TempData["Error"] = "حدث خطأ أثناء تحميل لوحة التحكم";
                return View(new DashboardViewModel());
            }
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }

    public class DashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalSurveys { get; set; }
        public int TotalDiscussions { get; set; }
        public int TotalTables { get; set; }
        public int TotalAttachments { get; set; }
        public int TotalSignatures { get; set; }
        public List<RecentEventViewModel> RecentEvents { get; set; } = new();
    }

    public class RecentEventViewModel
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

