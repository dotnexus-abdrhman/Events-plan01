using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using System.Security.Claims;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Only admins can generate/toggle public links
    public class PublicLinksController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PublicLinksController> _logger;

        public PublicLinksController(AppDbContext db, ILogger<PublicLinksController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(Guid eventId)
        {
            try
            {
                var orgId = GetOrganizationId();
                var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
                if (ev == null) return NotFound(new { message = "الحدث غير موجود" });

                // Allow platform Admin to manage any event
                if (!User.IsInRole("Admin") && ev.OrganizationId != orgId)
                    return Forbid();

                var link = await _db.EventPublicLinks.FirstOrDefaultAsync(x => x.EventId == eventId);
                if (link == null)
                {
                    link = new EvenDAL.Models.Classes.EventPublicLink
                    {
                        EventPublicLinkId = Guid.NewGuid(),
                        EventId = eventId,
                        Token = Guid.NewGuid().ToString("N"),
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.EventPublicLinks.Add(link);
                }
                else
                {
                    if (!link.IsEnabled)
                        link.IsEnabled = true; // enable existing link
                }

                await _db.SaveChangesAsync();

                var baseUrl = string.Concat(Request.Scheme, "://", Request.Host.ToUriComponent());
                var publicUrl = $"{baseUrl}/Public/Event/{link.Token}";
                return Json(new { success = true, url = publicUrl, token = link.Token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate public link for event {EventId}", eventId);
                return Json(new { success = false, message = "تعذر إنشاء الرابط العام" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(Guid eventId, bool enabled)
        {
            try
            {
                var orgId = GetOrganizationId();
                var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
                if (ev == null) return NotFound(new { message = "الحدث غير موجود" });
                if (!User.IsInRole("Admin") && ev.OrganizationId != orgId) return Forbid();

                var link = await _db.EventPublicLinks.FirstOrDefaultAsync(x => x.EventId == eventId);
                if (link == null) return NotFound(new { message = "لا يوجد رابط عام لهذا الحدث" });
                link.IsEnabled = enabled;
                await _db.SaveChangesAsync();
                return Json(new { success = true, enabled });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle public link for event {EventId}", eventId);
                return Json(new { success = false, message = "تعذر تحديث حالة الرابط" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Status(Guid eventId)
        {
            try
            {
                var link = await _db.EventPublicLinks.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == eventId);
                if (link == null)
                    return Json(new { exists = false });

                var baseUrl = string.Concat(Request.Scheme, "://", Request.Host.ToUriComponent());
                var publicUrl = $"{baseUrl}/Public/Event/{link.Token}";
                return Json(new { exists = true, enabled = link.IsEnabled, expiresAt = link.ExpiresAt, url = publicUrl, token = link.Token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get public link status for event {EventId}", eventId);
                return Json(new { exists = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetExpiry(Guid eventId, DateTimeOffset? expiresAt)
        {
            try
            {
                var orgId = GetOrganizationId();
                var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
                if (ev == null) return NotFound(new { message = "الحدث غير موجود" });
                if (!User.IsInRole("Admin") && ev.OrganizationId != orgId) return Forbid();

                var link = await _db.EventPublicLinks.FirstOrDefaultAsync(x => x.EventId == eventId);
                if (link == null) return NotFound(new { message = "لا يوجد رابط عام لهذا الحدث" });
                link.ExpiresAt = expiresAt?.UtcDateTime;
                await _db.SaveChangesAsync();
                return Json(new { success = true, expiresAt = link.ExpiresAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set expiry for public link {EventId}", eventId);
                return Json(new { success = false, message = "تعذر تحديث تاريخ الانتهاء" });
            }
        }

        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }
}

