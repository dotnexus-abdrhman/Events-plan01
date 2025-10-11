using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminsController : Controller
    {
        private readonly ICrudService<AdminDto, Guid> _admins;
        private readonly ILogger<AdminsController> _logger;

        public AdminsController(ICrudService<AdminDto, Guid> admins, ILogger<AdminsController> logger)
        {
            _admins = admins;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var admins = await _admins.ListAsync();
            return View(admins.OrderByDescending(a => a.CreatedAt));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _admins.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _admins.UpdateAsync(model);
                TempData["Success"] = "تم تحديث بيانات المسؤول";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحديث بيانات المسؤول {AdminId}", model.Id);
                TempData["Error"] = "حدث خطأ أثناء التحديث";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _admins.DeleteAsync(id);
                TempData["Success"] = "تم حذف المسؤول";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف المسؤول {AdminId}", id);
                TempData["Error"] = "تعذر حذف المسؤول";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CreateOrPromote()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNew([FromForm] string email, [FromForm] string? fullName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "البريد الإلكتروني مطلوب";
                return RedirectToAction(nameof(CreateOrPromote));
            }

            try
            {
                var dto = new AdminDto
                {
                    Id = Guid.NewGuid(),
                    Email = email.Trim(),
                    FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName!.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _admins.CreateAsync(dto);
                TempData["Success"] = "تم إنشاء المسؤول بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إنشاء مسؤول جديد");
                TempData["Error"] = "حدث خطأ أثناء إنشاء المسؤول";
            }
            return RedirectToAction(nameof(CreateOrPromote));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteUser([FromForm] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "البريد الإلكتروني للمستخدم مطلوب";
                return RedirectToAction(nameof(CreateOrPromote));
            }

            try
            {
                // لأغراض العرض فقط: نضيف بريده كمسؤول منصة إن لم يكن موجوداً
                var dto = new AdminDto
                {
                    Id = Guid.NewGuid(),
                    Email = email.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _admins.CreateAsync(dto);
                TempData["Success"] = "تمت ترقية المستخدم إلى مسؤول بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء ترقية مستخدم إلى مسؤول");
                TempData["Error"] = "حدث خطأ أثناء الترقية";
            }
            return RedirectToAction(nameof(CreateOrPromote));
        }
    }
}

