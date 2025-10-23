using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using RourtPPl01.Areas.Public.ViewModels;

namespace RourtPPl01.Areas.Public.Controllers
{
    [Area("Public")]
    [AllowAnonymous]
    public class VerifyController : Controller
    {
        private readonly AppDbContext _db;
        public VerifyController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/verify/{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            try
            {
                var ver = await _db.PdfVerifications.AsNoTracking().FirstOrDefaultAsync(v => v.PdfVerificationId == id);
                if (ver == null)
                {
                    return View(new VerifyViewModel
                    {
                        IsFound = false,
                        ErrorMessage = "لم يتم العثور على سجل التحقق. يرجى التأكد من صحة الرابط أو إعادة تصدير الملف.",
                        VerificationId = id
                    });
                }

                var vm = new VerifyViewModel
                {
                    IsFound = true,
                    VerificationId = ver.PdfVerificationId,
                    EventId = ver.EventId,
                    PdfType = ver.PdfType,
                    ExportedAtUtc = ver.ExportedAtUtc,
                    VerificationUrl = ver.VerificationUrl
                };

                return View(vm);
            }
            catch (Exception)
            {
                // Likely a DB schema issue (e.g., missing PdfVerifications table) on legacy databases
                return View(new VerifyViewModel
                {
                    IsFound = false,
                    ErrorMessage = "التأكد غير متاح حالياً بسبب مشكلة في قاعدة البيانات. يرجى تحديث قاعدة البيانات ثم إعادة التصدير.",
                    VerificationId = id
                });
            }
        }
    }
}

