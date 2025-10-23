using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RourtPPl01.Areas.Admin.ViewModels;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class UsersController : Controller
    {
        private readonly ICrudService<UserDto, Guid> _users;
        private readonly ICrudService<OrganizationDto, Guid> _orgs;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ICrudService<UserDto, Guid> users,
            ICrudService<OrganizationDto, Guid> orgs,
            ILogger<UsersController> logger)
        {
            _users = users;
            _orgs = orgs;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _users.ListAsync();
            var orgs = (await _orgs.ListAsync()).ToDictionary(o => o.OrganizationId, o => o.Name ?? o.NameEn ?? "");

            var vm = new UsersIndexViewModel
            {
                Users = users.Select(u => new UsersIndexItem
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone ?? string.Empty,
                    OrganizationName = (u.OrganizationId.HasValue && orgs.ContainsKey(u.OrganizationId.Value)) ? orgs[u.OrganizationId.Value] : "بدون مجموعة",
                    RoleName = u.RoleName,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                }).OrderByDescending(x => x.CreatedAt).ToList()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new UsersCreatePageViewModel
            {
                Organizations = (await _orgs.ListAsync())
                    .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.Name ?? o.NameEn ?? "Organization")
                    .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var pageVm = new UsersCreatePageViewModel
                {
                    Form = form,
                    Organizations = (await _orgs.ListAsync())
                        .OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Name ?? o.NameEn ?? "Organization")
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList()
                };
                return View(pageVm);
            }

            try
            {
                Guid? orgId = (form.OrganizationId.HasValue && form.OrganizationId.Value != Guid.Empty)
                    ? form.OrganizationId.Value
                    : (Guid?)null;

                var dto = new UserDto
                {
                    UserId = Guid.NewGuid(),
                    OrganizationId = orgId,
                    FullName = form.FullName.Trim(),
                    Email = form.Email.Trim(),
                    Phone = form.Phone?.Trim() ?? string.Empty,
                    RoleName = form.RoleName,
                    IsActive = form.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _users.CreateAsync(dto);
                TempData["Success"] = "تم إنشاء المستخدم بنجاح";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إنشاء مستخدم جديد");
                TempData["Error"] = "حدث خطأ أثناء إنشاء المستخدم";
                var pageVm = new UsersCreatePageViewModel
                {
                    Form = form,
                    Organizations = (await _orgs.ListAsync())
                        .OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Name ?? o.NameEn ?? "Organization")
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList()
                };
                return View(pageVm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _users.GetByIdAsync(id);
            if (dto == null) return NotFound();
            var vm = new UsersCreatePageViewModel
            {
                Form = new CreateUserViewModel
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    OrganizationId = dto.OrganizationId,
                    RoleName = dto.RoleName,
                    IsActive = dto.IsActive
                },
                Organizations = (await _orgs.ListAsync())
                    .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.Name ?? o.NameEn ?? "Organization")
                    .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                    .ToList(),
                Roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Admin", Text = "المدير التنفيذي" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Organizer", Text = "عضو مجلس إدارة" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Attendee", Text = "عضو" }
                }
            };
            ViewBag.UserId = id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CreateUserViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var pageVm = new UsersCreatePageViewModel
                {
                    Form = form,
                    Organizations = (await _orgs.ListAsync())
                        .OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Name ?? o.NameEn ?? "Organization")
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList(),
                    Roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Admin", Text = "المدير التنفيذي" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Organizer", Text = "عضو مجلس إدارة" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Attendee", Text = "عضو" }
                }
                };
                ViewBag.UserId = id;
                return View(pageVm);
            }

            try
            {
                var existing = await _users.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.FullName = form.FullName.Trim();
                existing.Email = form.Email.Trim();
                existing.Phone = form.Phone?.Trim() ?? string.Empty;
                existing.OrganizationId = (form.OrganizationId.HasValue && form.OrganizationId.Value != Guid.Empty)
                    ? form.OrganizationId.Value
                    : null;
                existing.RoleName = form.RoleName;
                existing.IsActive = form.IsActive;

                await _users.UpdateAsync(existing);
                TempData["Success"] = "تم تحديث بيانات المستخدم";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "اخطاء اثناء تحديث المستخدم {UserId}", id);
                TempData["Error"] = "حدث خطأ أثناء التحديث";
                var pageVm = new UsersCreatePageViewModel
                {
                    Form = form,
                    Organizations = (await _orgs.ListAsync())
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList(),
                    Roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Admin", Text = "المدير التنفيذي" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Organizer", Text = "عضو مجلس إدارة" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Attendee", Text = "عضو" }
                }
                };
                ViewBag.UserId = id;
                return View(pageVm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            // نحاول الحذف الفعلي أولاً. إذا فشل بسبب علاقات/قيود قاعدة البيانات،
            // ننتقل لتعطيل المستخدم (Soft Delete) مع إبقاء البيانات المرتبطة سليمة.
            try
            {
                var hardDeleted = await _users.DeleteAsync(id);
                if (hardDeleted)
                {
                    TempData["Success"] = "تم حذف المستخدم";
                    return RedirectToAction(nameof(Index));
                }

                // لم يتم العثور على الكيان أو لم يحدث حذف فعلي، نرجع بدون خطأ
                TempData["Success"] = "تم حذف المستخدم";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // غالباً سبب الفشل: قيود علاقات (FK) لوجود بيانات مرتبطة بالمستخدم
                try
                {
                    var dto = await _users.GetByIdAsync(id);
                    if (dto == null)
                    {
                        TempData["Error"] = "المستخدم غير موجود";
                        return RedirectToAction(nameof(Index));
                    }

                    if (!dto.IsActive)
                    {
                        TempData["Warning"] = "تعذر حذف المستخدم لوجود بيانات مرتبطة. المستخدم معطل مسبقاً وتم الاحتفاظ ببياناته.";
                        return RedirectToAction(nameof(Index));
                    }

                    dto.IsActive = false; // Soft delete
                    await _users.UpdateAsync(dto);

                    _logger.LogWarning(ex, "Falling back to soft delete for user {UserId} due to related data", id);
                    TempData["Success"] = "تم تعطيل المستخدم بدلاً من الحذف بسبب وجود بيانات مرتبطة به.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "فشل الحذف والتعطيل للمستخدم {UserId}", id);
                    TempData["Error"] = "تعذر حذف أو تعطيل المستخدم. تحقق من البيانات المرتبطة (استبيانات، نقاشات، توقيعات، حضور، إلخ).";
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        private async Task<Guid> GetDefaultGroupId()
        {
            try
            {
                var orgs = await _orgs.ListAsync();
                var existing = orgs.FirstOrDefault(o => string.Equals((o.Name ?? string.Empty).Trim(), "بدون مجموعة", StringComparison.Ordinal))
                               ?? orgs.FirstOrDefault(o => string.Equals((o.NameEn ?? string.Empty).Trim(), "Ungrouped", StringComparison.OrdinalIgnoreCase));
                if (existing != null) return existing.OrganizationId;

                var dto = new OrganizationDto
                {
                    OrganizationId = Guid.NewGuid(),
                    Name = "بدون مجموعة",
                    NameEn = "Ungrouped",
                    TypeName = "Other",
                    Type = 4,
                    LicenseExpiry = null,
                    IsActive = true,
                    PrimaryColor = "#4A90E2",
                    SecondaryColor = "#6C757D",
                    Logo = string.Empty,
                    Settings = "{}",
                    LicenseKey = $"GROUP-NONE-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    CreatedAt = DateTime.UtcNow
                };
                var created = await _orgs.CreateAsync(dto);
                return created.OrganizationId;
            }
            catch
            {
                return Guid.Empty;
            }
        }


    }
}

