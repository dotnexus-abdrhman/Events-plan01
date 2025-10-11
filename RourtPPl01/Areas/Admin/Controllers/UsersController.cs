using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RourtPPl01.Areas.Admin.ViewModels;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
                    OrganizationName = orgs.ContainsKey(u.OrganizationId) ? orgs[u.OrganizationId] : string.Empty,
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
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList()
                };
                return View(pageVm);
            }

            try
            {
                var dto = new UserDto
                {
                    UserId = Guid.NewGuid(),
                    OrganizationId = form.OrganizationId,
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
                    .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                    .ToList(),
                Roles = new List<string> { "Attendee", "Organizer", "Observer" }
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
                        .Select(o => new OrgItem { OrganizationId = o.OrganizationId, Name = o.Name ?? o.NameEn ?? "Organization" })
                        .ToList(),
                    Roles = new List<string> { "Attendee", "Organizer", "Observer" }
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
                existing.OrganizationId = form.OrganizationId;
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
                    Roles = new List<string> { "Attendee", "Organizer", "Observer" }
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

    }
}

