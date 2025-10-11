using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.ViewModels.Auth;
using EventPl.Services.Interface;
using EventPl.Dto;
using System.Security.Claims;

namespace RourtPPl01.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly ICrudService<UserDto, Guid> _users;
        private readonly ICrudService<OrganizationDto, Guid> _orgs;
        private readonly ICrudService<AdminDto, Guid> _admins;

        public AuthController(IAuthService authService,
                              ILogger<AuthController> logger,
                              ICrudService<UserDto, Guid> users,
                              ICrudService<OrganizationDto, Guid> orgs,
                              ICrudService<AdminDto, Guid> admins)
        {
            _authService = authService;
            _logger = logger;
            _users = users;
            _orgs = orgs;
            _admins = admins;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // إذا كان المستخدم مسجل دخول بالفعل، وجهه للصفحة المناسبة
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                else
                    return RedirectToAction("Index", "MyEvents", new { area = "UserPortal" });
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // محاولة تسجيل الدخول بالـ Identifier (Email أو Phone)
                var result = await _authService.LoginByIdentifierAsync(model.Identifier);

                if (result == null)
                {
                    ModelState.AddModelError("", "البريد الإلكتروني أو رقم الهاتف غير صحيح");
                    return View(model);
                }

                // إنشاء Claims
                // تأمين OrganizationId للمسؤول: إن لم يكن مرتبطًا بجهة، اربطه افتراضيًا بأول جهة متوفرة
                Guid orgIdForClaims = result.OrganizationId;
                if (string.Equals(result.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && orgIdForClaims == Guid.Empty)
                {
                    try
                    {
                        var orgsList = await _orgs.ListAsync();
                        var firstOrg = orgsList.FirstOrDefault();
                        if (firstOrg != null) orgIdForClaims = firstOrg.OrganizationId;
                    }
                    catch { /* ignore and keep Guid.Empty */ }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new Claim(ClaimTypes.Name, result.FullName),
                    new Claim(ClaimTypes.Email, result.Email),
                    new Claim(ClaimTypes.Role, result.RoleName),
                    new Claim("OrganizationId", orgIdForClaims.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // توجيه المستخدم حسب الدور
                if (result.RoleName == "Admin")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                else
                {
                    return RedirectToAction("Index", "MyEvents", new { area = "UserPortal" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تسجيل الدخول");
                ModelState.AddModelError("", "حدث خطأ أثناء تسجيل الدخول. يرجى المحاولة مرة أخرى.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Redirect explicitly to login path to ensure consistent Location header
            return Redirect("/Auth/Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Registration is disabled in this application
        [HttpGet]
        public IActionResult Register()
        {
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel vm)
        {
            return NotFound();
        }
    }
}

