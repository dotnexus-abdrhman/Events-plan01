using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventPresentationlayer.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class GroupsController : Controller
    {
        private readonly ICrudService<OrganizationDto, Guid> _groups;
        private readonly ICrudService<UserDto, Guid> _users;
        private readonly ICrudService<EventDto, Guid> _events;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            ICrudService<OrganizationDto, Guid> groups,
            ICrudService<UserDto, Guid> users,
            ICrudService<EventDto, Guid> events,
            ILogger<GroupsController> logger)
        {
            _groups = groups;
            _users = users;
            _events = events;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromServices] ICrudService<OrganizationDto, Guid> orgsService)
        {
            var list = await orgsService.ListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new OrganizationFormVm();
            var users = await _users.ListAsync();

            // مصدر الواجهة الجديدة (قائمة قابلة للبحث)
            vm.AvailableUsers = users
                .Select(u => new UserLiteVm
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Phone = u.Phone ?? string.Empty,
                    IsActive = u.IsActive
                }).OrderBy(u => u.FullName).ToList();

            // إبقاء المصدر القديم للتوافق إن لزم
            vm.Users = users
                .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FullName} ({u.Email})" })
                .ToList();
            return View("CreateGroup", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrganizationFormVm vm)
        {
            if (!ModelState.IsValid)
            {
                var users = await _users.ListAsync();
                vm.Users = users
                    .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FullName} ({u.Email})" })
                    .ToList();
                return View("CreateGroup", vm);
            }

            try
            {
                var typeName = vm.Type switch
                {
                    1 => "Government",
                    2 => "Private",
                    3 => "NonProfit",
                    _ => "Other"
                };

                var dto = new OrganizationDto
                {
                    OrganizationId = Guid.NewGuid(),
                    Name = vm.Name?.Trim() ?? string.Empty,
                    NameEn = vm.NameEn?.Trim() ?? string.Empty,
                    TypeName = typeName,
                    Type = vm.Type,
                    LicenseExpiry = vm.LicenseExpiry,
                    IsActive = vm.IsActive,
                    PrimaryColor = string.IsNullOrWhiteSpace(vm.PrimaryColor) ? "#4A90E2" : vm.PrimaryColor,
                    SecondaryColor = string.IsNullOrWhiteSpace(vm.SecondaryColor) ? "#6C757D" : vm.SecondaryColor,
                    Logo = vm.Logo ?? string.Empty,
                    Settings = vm.Settings ?? "{}",
                    LicenseKey = string.IsNullOrWhiteSpace(vm.LicenseKey)
                        ? $"MINA-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
                        : vm.LicenseKey!.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _groups.CreateAsync(dto);

                // ربط المستخدمين المختارين بالمجموعة الجديدة
                if (vm.SelectedUserIds != null && vm.SelectedUserIds.Count > 0)
                {
                    foreach (var userId in vm.SelectedUserIds)
                    {
                        var user = await _users.GetByIdAsync(userId);
                        if (user != null)
                        {
                            user.OrganizationId = created.OrganizationId;
                            await _users.UpdateAsync(user);
                        }
                    }
                }

                TempData["Success"] = "تم إنشاء المجموعة بنجاح وربط المستخدمين المختارين";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إنشاء مجموعة");
                TempData["Error"] = "حدث خطأ أثناء حفظ المجموعة";
                return View("CreateGroup", vm);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var group = await _groups.GetByIdAsync(id);
            if (group == null) return NotFound();

            var users = (await _users.ListAsync()).Where(u => u.OrganizationId == id).ToList();
            var eventsList = (await _events.ListAsync()).Where(e => e.OrganizationId == id).ToList();

            var vm = new RourtPPl01.Areas.Admin.ViewModels.GroupDetailsVm
            {
                Group = group,
                Users = users,
                Events = eventsList
            };
            return View(vm);
        }



        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _groups.GetByIdAsync(id);
            if (dto == null) return NotFound();

            var vm = new OrganizationFormVm
            {
                OrganizationId = dto.OrganizationId,
                Name = dto.Name,
                NameEn = dto.NameEn,
                Type = dto.Type != 0 ? dto.Type : (dto.TypeName?.ToLower() switch
                {
                    "government" => 1,
                    "private" => 2,
                    "nonprofit" => 3,
                    _ => 4
                }),
                LicenseExpiry = dto.LicenseExpiry,
                IsActive = dto.IsActive,
                PrimaryColor = string.IsNullOrWhiteSpace(dto.PrimaryColor) ? "#4A90E2" : dto.PrimaryColor,
                SecondaryColor = string.IsNullOrWhiteSpace(dto.SecondaryColor) ? "#6C757D" : dto.SecondaryColor,
                Logo = dto.Logo,
                Settings = dto.Settings,
                LicenseKey = dto.LicenseKey
            };

            // 434846 4248272645 27442339362721 27442d27444a4a46 482744452a272d4a46 44442536274129
            var allUsers = await _users.ListAsync();
            vm.CurrentUsers = allUsers.Where(u => u.OrganizationId == id)
                .Select(u => new UserLiteVm { UserId = u.UserId, FullName = u.FullName ?? string.Empty, Email = u.Email ?? string.Empty, Phone = u.Phone ?? string.Empty, IsActive = u.IsActive })
                .OrderBy(u => u.FullName).ToList();

            vm.AvailableUsers = allUsers.Where(u => u.OrganizationId != id)
                .Select(u => new UserLiteVm { UserId = u.UserId, FullName = u.FullName ?? string.Empty, Email = u.Email ?? string.Empty, Phone = u.Phone ?? string.Empty, IsActive = u.IsActive })
                .OrderBy(u => u.FullName).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, OrganizationFormVm vm)
        {
            if (id == Guid.Empty || vm.OrganizationId != Guid.Empty && vm.OrganizationId != id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                vm.OrganizationId = id;
                return View(vm);
            }

            try
            {
                var existing = await _groups.GetByIdAsync(id);
                if (existing == null) return NotFound();

                var typeName = vm.Type switch
                {
                    1 => "Government",
                    2 => "Private",
                    3 => "NonProfit",
                    _ => "Other"
                };

                existing.Name = string.IsNullOrWhiteSpace(vm.Name) ? existing.Name : vm.Name.Trim();
                existing.NameEn = string.IsNullOrWhiteSpace(vm.NameEn) ? existing.NameEn : vm.NameEn.Trim();
                existing.TypeName = typeName;
                existing.Type = vm.Type;
                existing.LicenseExpiry = vm.LicenseExpiry;
                existing.IsActive = vm.IsActive;
                existing.PrimaryColor = string.IsNullOrWhiteSpace(vm.PrimaryColor) ? existing.PrimaryColor : vm.PrimaryColor;
                existing.SecondaryColor = string.IsNullOrWhiteSpace(vm.SecondaryColor) ? existing.SecondaryColor : vm.SecondaryColor;
                existing.Logo = vm.Logo ?? existing.Logo;
                existing.Settings = vm.Settings ?? existing.Settings;
                existing.LicenseKey = string.IsNullOrWhiteSpace(vm.LicenseKey) ? existing.LicenseKey : vm.LicenseKey.Trim();
                existing.CreatedAt = existing.CreatedAt == default ? DateTime.UtcNow : existing.CreatedAt;

                await _groups.UpdateAsync(existing);

                // 452d 27362f4a44 452c454839272a 2c2f4a2f29 25392f 272d4a2746
                if (vm.SelectedUserIds != null && vm.SelectedUserIds.Count > 0)
                {
                    foreach (var userId in vm.SelectedUserIds.Distinct())
                    {
                        var user = await _users.GetByIdAsync(userId);
                        if (user != null)
                        {
                            user.OrganizationId = id;
                            await _users.UpdateAsync(user);

                        }
                    }
                }

                TempData["Success"] = "تم تحديث بيانات المجموعة وتمت إضافة المستخدمين المختارين";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تعديل المجموعة {OrganizationId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحديث المجموعة";
                vm.OrganizationId = id;
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            try
            {
                var user = await _users.GetByIdAsync(userId);
                if (user == null)
                {
                    if (Request.Headers.ContainsKey("X-Requested-With")) return Json(new { ok = false, message = "المستخدم غير موجود" });
                    TempData["Error"] = "المستخدم غير موجود";
                    return RedirectToAction(nameof(Details), new { id });
                }

                user.OrganizationId = null;
                await _users.UpdateAsync(user);

                if (Request.Headers.ContainsKey("X-Requested-With")) return Json(new { ok = true });
                TempData["Success"] = "تم حذف العضو من المجموعة";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إزالة المستخدم {UserId} من المجموعة {OrganizationId}", userId, id);
                if (Request.Headers.ContainsKey("X-Requested-With")) return Json(new { ok = false, message = "تعذر إزالة العضو" });
                TempData["Error"] = "تعذر إزالة العضو";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _groups.DeleteAsync(id);
                TempData["Success"] = "تم حذف المجموعة";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف المجموعة {OrganizationId}", id);
                TempData["Error"] = "تعذر حذف المجموعة. تحقق من عدم وجود سجلات مرتبطة";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

