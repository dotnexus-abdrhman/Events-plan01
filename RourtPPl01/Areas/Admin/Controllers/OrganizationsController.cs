using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventPresentationlayer.ViewModels;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrganizationsController : Controller
    {
        private readonly ICrudService<OrganizationDto, Guid> _organizations;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(ICrudService<OrganizationDto, Guid> organizations, ILogger<OrganizationsController> logger)
        {
            _organizations = organizations;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> Index([FromServices] ICrudService<OrganizationDto, Guid> orgsService)
        {
            // Using the registered service instead of direct Db access
            var list = await orgsService.ListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new OrganizationFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrganizationFormVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
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
                    // Ensure required non-null DB columns have values
                    LicenseKey = string.IsNullOrWhiteSpace(vm.LicenseKey)
                        ? $"MINA-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
                        : vm.LicenseKey!.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _organizations.CreateAsync(dto);
                TempData["Success"] = "\u062a\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u062c\u0647\u0629 \u0628\u0646\u062c\u0627\u062d";
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "\u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0625\u0646\u0634\u0627\u0621 \u062c\u0647\u0629");
                TempData["Error"] = "\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u062d\u0641\u0638 \u0627\u0644\u062c\u0647\u0629";
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _organizations.GetByIdAsync(id);
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
                var existing = await _organizations.GetByIdAsync(id);
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

                await _organizations.UpdateAsync(existing);
                TempData["Success"] = "تم تحديث بيانات الجهة";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تعديل الجهة {OrganizationId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحديث الجهة";
                vm.OrganizationId = id;
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _organizations.DeleteAsync(id);
                TempData["Success"] = "تم حذف الجهة";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف الجهة {OrganizationId}", id);
                TempData["Error"] = "تعذر حذف الجهة. تحقق من عدم وجود سجلات مرتبطة";
            }
            return RedirectToAction(nameof(Index));
        }

    }
}

