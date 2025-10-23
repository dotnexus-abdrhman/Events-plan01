using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.ViewModels.Auth;
using EventPl.Services.Interface;
using EventPl.Dto;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Antiforgery;

using Microsoft.Extensions.Caching.Memory;

namespace RourtPPl01.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly ICrudService<UserDto, Guid> _users;
        private readonly ICrudService<OrganizationDto, Guid> _orgs;
        private readonly ICrudService<AdminDto, Guid> _admins;
        private readonly IAntiforgery _antiforgery;

        public AuthController(IAuthService authService,
                              ILogger<AuthController> logger,
                              ICrudService<UserDto, Guid> users,
                              ICrudService<OrganizationDto, Guid> orgs,
                              ICrudService<AdminDto, Guid> admins,
                              IAntiforgery antiforgery)
        {
            _authService = authService;
            _logger = logger;
            _users = users;
            _orgs = orgs;
            _admins = admins;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // إذا كان المستخدم مسجل دخول بالفعل، وجهه للصفحة المناسبة
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("PlatformAdmin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                else
                    return RedirectToAction("Index", "MyEvents", new { area = "UserPortal" });
            }

            // ضمان إصدار وحفظ Cookie رمز الحماية من التزوير (Anti-forgery) لطلب POST التالي
            _antiforgery.GetAndStoreTokens(HttpContext);
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
                var swTotal = System.Diagnostics.Stopwatch.StartNew();

                // Normalize identifier to avoid formatting issues (e.g., Arabic digits, spaces)
                var normalized = NormalizeIdentifier(model.Identifier);

                // Measure core login lookup time separately
                var swLookup = System.Diagnostics.Stopwatch.StartNew();
                var result = await _authService.LoginByIdentifierAsync(normalized);
                swLookup.Stop();

                if (result == null)
                {
                    _logger.LogInformation("Login failed for identifier {Identifier}. Lookup {LookupMs} ms", normalized, swLookup.ElapsedMilliseconds);
                    ModelState.AddModelError("", "البريد الإلكتروني أو رقم الهاتف غير صحيح");
                    return View(model);
                }

                // إنشاء Claims
                // لا تقم بتحميل جميع الجهات لأي سبب أثناء تسجيل الدخول؛ استخدم القيمة المتاحة فقط
                Guid orgIdForClaims = result.OrganizationId ?? Guid.Empty;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new Claim(ClaimTypes.Name, result.FullName),
                    new Claim(ClaimTypes.Email, !string.IsNullOrWhiteSpace(result.Email) ? result.Email : (result.Phone ?? string.Empty)),
                    new Claim(ClaimTypes.Role, result.RoleName),
                    new Claim("OrganizationId", orgIdForClaims.ToString())
                };

                // Give platform admins a distinct role to protect Admin Area
                if (string.Equals(result.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && (result.OrganizationId == null || result.OrganizationId == Guid.Empty))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "PlatformAdmin"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                var swSignIn = System.Diagnostics.Stopwatch.StartNew();
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                swSignIn.Stop();

                // Pre-warm validation cache used by OnValidatePrincipal to avoid first navigation DB hit
                try
                {
                    var cache = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                    var cacheKey = claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "PlatformAdmin")
                        ? $"admin-active-{result.UserId}"
                        : $"user-active-{result.UserId}";
                    cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
                }
                catch { /* non-fatal */ }

                swTotal.Stop();
                _logger.LogInformation("Login success for user {UserId}. Lookup {LookupMs} ms, SignIn {SignInMs} ms, Total {TotalMs} ms", result.UserId, swLookup.ElapsedMilliseconds, swSignIn.ElapsedMilliseconds, swTotal.ElapsedMilliseconds);

                // توجيه المستخدم: منصة الإدارة لمن لديهم صلاحية PlatformAdmin فقط
                if (string.Equals(result.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && (result.OrganizationId == null || result.OrganizationId == Guid.Empty))
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

        private static string NormalizeIdentifier(string? input)
        {
            var s = input?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // If it's an email, keep as-is
            if (s.Contains("@")) return s;
            // Normalize phone: convert Arabic digits to ASCII and strip non-digits
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch >= '0' && ch <= '9') { sb.Append(ch); continue; }
                // Arabic-Indic digits 0660..0669
                if (ch >= '\u0660' && ch <= '\u0669') { sb.Append((char)('0' + (ch - '\u0660'))); continue; }
                // Eastern Arabic-Indic digits 06F0..06F9
                if (ch >= '\u06F0' && ch <= '\u06F9') { sb.Append((char)('0' + (ch - '\u06F0'))); continue; }
                // ignore other characters (spaces, dashes, +)
            }
            return sb.ToString();
        }

    }
}

