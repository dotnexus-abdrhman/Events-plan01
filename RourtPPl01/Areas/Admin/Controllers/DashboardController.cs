using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteDAl.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;
using EventPl.Services.Interface;
using EventPl.Dto;

using System.Linq;
namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "PlatformAdmin")]
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

                // احسب الإحصاءات بنفس نطاق الشاشات لضمان التطابق (أحداث المنظمة + البث)
                var eventsList = await _eventsService.GetOrganizationEventsAsync(orgId) ?? new List<EventPl.Dto.EventDto>();
                var totalEvents = eventsList.Select(e => e.EventId).Distinct().Count();
                var activeEvents = eventsList.Count(e => Enum.TryParse<EventStatus>(e.StatusName, true, out var s) && s == EventStatus.Active);

                // نفس مصادر القوائم لضمان التطابق
                var totalUsers = (await _usersService.ListAsync()).Count();
                var totalOrgs = (await _orgsService.ListAsync()).Count();

                var stats = new DashboardViewModel
                {
                    TotalEvents = totalEvents,
                    ActiveEvents = activeEvents,
                    TotalUsers = totalUsers,
                    TotalOrganizations = totalOrgs,

                    // بقية الإحصاءات (مقيدة بنطاق المنظمة)
                    TotalSurveys = await _db.Surveys.CountAsync(s => s.Event.OrganizationId == orgId),
                    TotalDiscussions = await _db.Discussions.CountAsync(d => d.Event.OrganizationId == orgId),
                    TotalTables = await _db.TableBlocks.CountAsync(t => t.Event.OrganizationId == orgId),
                    TotalAttachments = await _db.Attachments.CountAsync(a => a.Event.OrganizationId == orgId),
                    TotalSignatures = await _db.UserSignatures.CountAsync(s => s.Event.OrganizationId == orgId),

                    RecentEvents = eventsList
                        .OrderByDescending(e => e.CreatedAt)
                        .Take(5)
                        .Select(e => new RecentEventViewModel
                        {
                            EventId = e.EventId,
                            Title = e.Title,
                            StartAt = e.StartAt,
                            Status = e.StatusName ?? string.Empty
                        })
                        .ToList()
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

