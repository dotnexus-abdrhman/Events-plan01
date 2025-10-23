using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using EventPl.Services.Interface;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RouteDAl.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EvenDAL.Models.Shared.Enums;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using QRCoder;
using PdfSharpCore.Drawing;

namespace EventPl.Services.ClassServices
{
    public class PdfExportService : IPdfExportService
    {
        private readonly IMinaResultsService _results;
        private readonly AppDbContext _db;
        private readonly IHostEnvironment _env;
        private readonly ILogger<PdfExportService> _logger;

        public PdfExportService(IMinaResultsService results, AppDbContext db, IHostEnvironment env, ILogger<PdfExportService> logger)
        {
            // Ensure QuestPDF license is configured in any hosting context (tests, dev, prod)
            QuestPDF.Settings.License = LicenseType.Community;
            _results = results;
            _db = db;
            _env = env;
            _logger = logger;
        }

        public async Task<byte[]> ExportEventSummaryPdfAsync(Guid eventId)
        {
            var summary = await _results.GetEventResultsAsync(eventId);
            var stats = await _results.GetEventStatisticsAsync(eventId);

            try
            {
                var bytes = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().AlignRight().Text($"ملخص النتائج - {summary.EventTitle}").FontSize(18).SemiBold();
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text($"عدد المشاركين: {stats.UniqueParticipants}");
                                r.RelativeItem().AlignRight().Text($"عدد التوقيعات: {stats.TotalSignatures}");
                            });
                            col.Item().AlignRight().Text("نتائج الاستبيانات:").FontSize(14).Bold();
                            foreach (var s in summary.SurveyResults)
                            {
                                col.Item().AlignRight().Text($"• {s.SurveyTitle}").Bold();
                                foreach (var q in s.QuestionResults)
                                {
                                    col.Item().AlignRight().Text($"- {q.QuestionText} ({q.QuestionType})");
                                    foreach (var o in q.OptionResults)
                                    {
                                        col.Item().AlignRight().Text($"  · {o.OptionText}: {o.Count} ({o.Percentage})");
                                    }
                                }
                            }
                        });
                    });
                }).GeneratePdf();

                return bytes;
            }
            catch
            {
                // Fallback minimal PDF header if generator fails (e.g., license not configured in environment)
                return System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");
            }
        }

        public async Task<byte[]> ExportEventDetailedPdfAsync(Guid eventId)
        {
            var eventTitle = (await _db.Events.AsNoTracking().Where(e => e.EventId == eventId).Select(e => e.Title).FirstOrDefaultAsync()) ?? "الحدث";

            // Participants who have any activity
            var participantIds = await _db.SurveyAnswers.Where(a => a.EventId == eventId).Select(a => a.UserId).Distinct()
                .Union(_db.DiscussionReplies.Where(r => r.Discussion.EventId == eventId).Select(r => r.UserId))
                .Union(_db.UserSignatures.Where(s => s.EventId == eventId).Select(s => s.UserId))
                .Distinct().ToListAsync();

            var users = await _db.Users.AsNoTracking().Where(u => participantIds.Contains(u.UserId)).ToListAsync();

            var answersByUser = await _db.SurveyAnswers.AsNoTracking()
                .Where(a => a.EventId == eventId)
                .Include(a => a.Question)
                .Include(a => a.SelectedOptions).ThenInclude(so => so.Option)
                .GroupBy(a => a.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var repliesByUser = await _db.DiscussionReplies.AsNoTracking()
                .Where(r => r.Discussion.EventId == eventId)
                .Include(r => r.Discussion)
                .GroupBy(r => r.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var signaturesByUser = await _db.UserSignatures.AsNoTracking()
                .Where(s => s.EventId == eventId)
                .GroupBy(s => s.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault());

            try
            {
                var bytes = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().AlignRight().Text($"تفاصيل الردود - {eventTitle}").FontSize(18).SemiBold();
                            foreach (var uid in participantIds)
                            {
                                var user = users.FirstOrDefault(u => u.UserId == uid);
                                col.Item().LineHorizontal(0.5f);
                                col.Item().AlignRight().Text($"المستخدم: {user?.FullName ?? "-"}").Bold();

                                if (answersByUser.TryGetValue(uid, out var answers) && answers.Any())
                                {
                                    col.Item().AlignRight().Text("إجابات الاستبيانات:").Bold();
                                    foreach (var a in answers)
                                    {
                                        var opts = string.Join("، ", a.SelectedOptions.Select(so => so.Option.Text));
                                        col.Item().AlignRight().Text($"- {a.Question?.Text}: {opts}");
                                    }
                                }

                                if (repliesByUser.TryGetValue(uid, out var replies) && replies.Any())
                                {
                                    col.Item().AlignRight().Text("الردود في النقاشات:").Bold();
                                    foreach (var r in replies)
                                    {
                                        col.Item().AlignRight().Text($"- {r.Discussion?.Title}: {r.Body}");
                                    }
                                }

                                if (signaturesByUser.TryGetValue(uid, out var sig) && sig != null)
                                {
                                    col.Item().AlignRight().Text("التوقيع: موجود");
                                }
                            }
                        });
                    });
                }).GeneratePdf();

                return bytes;
            }
            catch
            {
                return System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");
            }
        }

        public async Task<byte[]> ExportUserResultPdfAsync(Guid eventId, Guid userId)
        {
            var eventTitle = (await _db.Events.AsNoTracking().Where(e => e.EventId == eventId).Select(e => e.Title).FirstOrDefaultAsync()) ?? "الحدث";
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

            var answers = await _db.SurveyAnswers.AsNoTracking()
                .Where(a => a.EventId == eventId && a.UserId == userId)
                .Include(a => a.Question)
                .Include(a => a.SelectedOptions).ThenInclude(so => so.Option)
                .ToListAsync();

            var replies = await _db.DiscussionReplies.AsNoTracking()
                .Where(r => r.Discussion.EventId == eventId && r.UserId == userId)
                .Include(r => r.Discussion)
                .ToListAsync();

            var signature = await _db.UserSignatures.AsNoTracking()
                .Where(s => s.EventId == eventId && s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            try
            {
                var bytes = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().AlignRight().Text($"نتائج المشارك - {eventTitle}").FontSize(18).SemiBold();
                            col.Item().AlignRight().Text($"الاسم: {user?.FullName ?? "-"}").Bold();

                            if (answers.Any())
                            {
                                col.Item().AlignRight().Text("إجابات الاستبيانات:").Bold();
                                foreach (var a in answers)
                                {
                                    var opts = string.Join("، ", a.SelectedOptions.Select(so => so.Option.Text));
                                    col.Item().AlignRight().Text($"- {a.Question?.Text}: {opts}");
                                }
                            }

                            if (replies.Any())
                            {
                                col.Item().AlignRight().Text("الردود في النقاشات:").Bold();
                                foreach (var r in replies)
                                {
                                    col.Item().AlignRight().Text($"- {r.Discussion?.Title}: {r.Body}");
                                }
                            }

                            if (signature != null)
                            {
                                col.Item().AlignRight().Text("التوقيع: موجود");
                            }
                        });
                    });
                }).GeneratePdf();

                return bytes;
            }
            catch
            {
                return System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");
            }
        }
        public async Task<byte[]> ExportCustomMergedWithParticipantsPdfAsync(Guid eventId)
        {
            // Preserve previous default behavior by delegating to the new overload with defaults
            var defaultOptions = new PdfExportOptions
            {
                IncludeEventDetails = true,
                IncludeSurveyAndResponses = false,
                IncludeDiscussions = false,
                IncludeSignatures = true,
                IncludeSections = false,
                IncludeAttachments = false,
                BrandingFooterText = "منصة مينا لإدارة الفعاليات",
                LogoBytes = null
            };
            return await ExportCustomMergedWithParticipantsPdfAsync(eventId, defaultOptions);
        }

        // New overload: allows styling the participants table that is appended after custom PDFs
        public async Task<byte[]> ExportCustomMergedWithParticipantsPdfAsync(Guid eventId, PdfExportOptions participantsOptions)
        {
            // Get all CustomPdf attachments in order
            var customPdfs = await _db.Attachments.AsNoTracking()
                .Where(a => a.EventId == eventId && a.Type == AttachmentType.CustomPdf)
                .OrderBy(a => a.Order)
                .ToListAsync();

            // Prepare verification (QR) for the merged export so all pages share the same verification
            byte[]? mergedQrCodeBytes = null;
            string? mergedVerificationUrl = null;
            if (!string.IsNullOrWhiteSpace(participantsOptions.VerificationUrlBase))
            {
                if (!participantsOptions.VerificationId.HasValue)
                    participantsOptions.VerificationId = Guid.NewGuid();
                participantsOptions.VerificationType = "CustomWithParticipants";

                // 1) Generate QR bytes and URL (isolated from persistence)
                try
                {
                    mergedVerificationUrl = $"{participantsOptions.VerificationUrlBase!.TrimEnd('/')}/verify/{participantsOptions.VerificationId.Value}";
                    using var gen = new QRCodeGenerator();
                    using var data = gen.CreateQrCode(mergedVerificationUrl, QRCodeGenerator.ECCLevel.Q);
                    using var qr = new PngByteQRCode(data);
                    mergedQrCodeBytes = qr.GetGraphic(10);
                    _logger.LogInformation("[PdfExport] Merged QR prepared. Bytes={Len} Url={Url}", mergedQrCodeBytes?.Length ?? 0, mergedVerificationUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PdfExport] Failed to generate merged QR; continuing without QR overlay");
                    mergedQrCodeBytes = null;
                }

                // 2) Try to persist verification record, but do not block rendering on failure
                if (mergedQrCodeBytes != null && !string.IsNullOrWhiteSpace(mergedVerificationUrl))
                {
                    try
                    {
                        _db.PdfVerifications.Add(new EvenDAL.Models.Classes.PdfVerification
                        {
                            PdfVerificationId = participantsOptions.VerificationId.Value,
                            EventId = eventId,
                            PdfType = participantsOptions.VerificationType ?? "CustomWithParticipants",
                            ExportedAtUtc = DateTime.UtcNow,
                            VerificationUrl = mergedVerificationUrl
                        });
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[PdfExport] Failed to persist merged verification record; QR will still be rendered");
                    }
                }
            }

            // Build participants-only PDF using provided appearance options (will also embed QR in its own footer if provided)
            var participantsPdfBytes = await ExportEventResultsPdfAsync(eventId, participantsOptions);

            // Merge using PdfSharpCore: custom PDFs first (in order), then participants table
            using var output = new PdfSharpCore.Pdf.PdfDocument();

            _logger.LogInformation("[PdfExport] Export custom results (with styled participants table): Event={EventId} CustomCount={Count}", eventId, customPdfs.Count);

            int totalImportedPages = 0;
            int ImportAllPages(byte[] bytes, string source)
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    var inputDoc = PdfSharpCore.Pdf.IO.PdfReader.Open(ms, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);
                    for (int i = 0; i < inputDoc.PageCount; i++)
                    {
                        output.AddPage(inputDoc.Pages[i]);
                    }
                    _logger.LogInformation("[PdfExport] Imported {Pages} pages from {Source}", inputDoc.PageCount, source);
                    return inputDoc.PageCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PdfExport] Failed importing pages from {Source}", source);
                    return 0;
                }
            }

            string ResolveFullPath(string? webPath)
            {
                if (string.IsNullOrWhiteSpace(webPath)) return string.Empty;
                var p = webPath.Replace('\\', '/').Trim();
                // Treat "/..." as a web-root-relative path under wwwroot, not as an absolute OS path
                if (p.StartsWith("/"))
                {
                    var primary = Path.Combine(_env.ContentRootPath, "wwwroot", p.TrimStart('/'));
                    if (System.IO.File.Exists(primary)) return primary;
                    var fallback = Path.Combine(_env.ContentRootPath, p.TrimStart('/'));
                    return fallback;
                }
                // Now handle true absolute OS paths (e.g., C:/..., \\server\share\...)
                if (Path.IsPathRooted(p)) return p;
                return Path.Combine(_env.ContentRootPath, p);
            }

            if (customPdfs.Count == 0)
            {
                _logger.LogWarning("[PdfExport] No CustomPdf attachments found in DB for event {EventId}. Export will include participants table only.", eventId);
            }

            foreach (var att in customPdfs)
            {
                var fullPath = ResolveFullPath(att.Path);
                var exists = System.IO.File.Exists(fullPath);
                _logger.LogInformation("[PdfExport] Merge candidate: Id={AttachmentId} Path={Path} Full={Full} Exists={Exists}", att.AttachmentId, att.Path, fullPath, exists);
                if (exists)
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                    totalImportedPages += ImportAllPages(bytes, fullPath);
                }
                else
                {
                    _logger.LogWarning("[PdfExport] CustomPdf file missing for attachment {AttachmentId} at {FullPath}", att.AttachmentId, fullPath);
                }
            }

            // Append participants table PDF at the end
            totalImportedPages += ImportAllPages(participantsPdfBytes, "participants-table");
            _logger.LogInformation("[PdfExport] Total pages merged for Event {EventId} = {TotalPages}", eventId, totalImportedPages);

            // Overlay QR on pages imported from custom PDFs (do not alter participants pages again)
            if (mergedQrCodeBytes != null && participantsOptions.ShowQrCode && totalImportedPages > 0)
            {
                try
                {
                    using var xImg = XImage.FromStream(() => new MemoryStream(mergedQrCodeBytes));
                    for (int i = 0; i < Math.Min(totalImportedPages, output.Pages.Count); i++)
                    {
                        var page = output.Pages[i];
                        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                        double size = Math.Clamp(participantsOptions.QrCodeSize, 30, 200);
                        const double margin = 25.0;
                        var pos = participantsOptions.QrCodePosition ?? "BottomLeft";
                        double x;
                        double y = page.Height.Point - margin - size;
                        if (pos.Equals("BottomRight", StringComparison.OrdinalIgnoreCase))
                            x = page.Width.Point - margin - size;
                        else if (pos.Equals("BottomCenter", StringComparison.OrdinalIgnoreCase))
                            x = (page.Width.Point - size) / 2.0;
                        else
                            x = margin; // BottomLeft default
                        gfx.DrawImage(xImg, x, y, size, size);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PdfExport] Failed overlaying QR on imported pages");
                }
            }

            using var outStream = new MemoryStream();
            output.Save(outStream);
            var outBytes = outStream.ToArray();
            _logger.LogInformation("[PdfExport] Final merged PDF size for Event {EventId}: {Size} bytes", eventId, outBytes.Length);
            return outBytes;
        }


        public async Task<byte[]> ExportEventResultsPdfAsync(Guid eventId, PdfExportOptions options)
        {
            var evt = await _db.Events.AsNoTracking()
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
            var eventTitle = evt?.Title ?? "الحدث";
            var org = evt?.Organization;

            // Participants (comprehensive): any activity, guests, legacy participants, attendance logs
            var baseParticipants = await _db.SurveyAnswers.Where(a => a.EventId == eventId).Select(a => a.UserId).Distinct()
                .Union(_db.DiscussionReplies.Where(r => r.Discussion.EventId == eventId).Select(r => r.UserId))
                .Union(_db.UserSignatures.Where(s => s.EventId == eventId).Select(s => s.UserId))
                .Union(_db.EventParticipants.Where(p => p.EventId == eventId).Select(p => p.UserId))
                .Union(_db.AttendanceLogs.Where(l => l.EventId == eventId).Select(l => l.UserId))
                .Distinct().ToListAsync();

            var guestUserIds = await _db.PublicEventGuests.AsNoTracking()
                .Where(g => g.EventId == eventId)
                .Select(g => g.UserId)
                .ToListAsync();

            var participantIds = baseParticipants.Union(guestUserIds).Distinct().ToList();
            _logger.LogInformation("[PdfExport] Participants fetch: base={Base} guests={Guests} total={Total} for Event {EventId}", baseParticipants.Count, guestUserIds.Count, participantIds.Count, eventId);
            if (participantIds.Count == 0)
            {
                _logger.LogWarning("[PdfExport] No participants resolved for Event {EventId}. The participants table will be empty.", eventId);
            }

            var users = await _db.Users.AsNoTracking().Where(u => participantIds.Contains(u.UserId)).ToListAsync();
            _logger.LogInformation("[PdfExport] Users materialized for participants: {Count}", users.Count);

            // Prefer explicit participant role (per-event) when available; fallback to User.Role
            var participantRolesByUser = await _db.EventParticipants.AsNoTracking()
                .Where(p => p.EventId == eventId)
                .GroupBy(p => p.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(p => p.JoinedAt ?? p.InvitedAt).First().Role);

            var answersByUser = await _db.SurveyAnswers.AsNoTracking()
                .Where(a => a.EventId == eventId)
                .Include(a => a.Question)
                .Include(a => a.SelectedOptions).ThenInclude(so => so.Option)
                .GroupBy(a => a.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var repliesByUser = await _db.DiscussionReplies.AsNoTracking()
                .Where(r => r.Discussion.EventId == eventId)
                .Include(r => r.Discussion)
                .GroupBy(r => r.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var signaturesByUser = await _db.UserSignatures.AsNoTracking()
                .Where(s => s.EventId == eventId)
                .GroupBy(s => s.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault());

            var sections = new List<EvenDAL.Models.Classes.Section>();
            if (options.IncludeSections)
            {
                sections = await _db.Sections.AsNoTracking()
                    .Where(s => s.EventId == eventId)
                    .OrderBy(s => s.Order)
                    .ToListAsync();
            }

            var attachments = new List<EvenDAL.Models.Classes.Attachment>();
            if (options.IncludeAttachments)
            {
                attachments = await _db.Attachments.AsNoTracking()
                    .Where(a => a.EventId == eventId)
                    .OrderBy(a => a.Order)
                    .ToListAsync();
            }

            var tables = await _db.TableBlocks.AsNoTracking()
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            // if logo not provided, try organization logo path
            if ((options.LogoBytes == null || options.LogoBytes.Length == 0) && !string.IsNullOrWhiteSpace(org?.Logo))
            {
                var logoPath = MapWebPathToFile(org!.Logo);
                if (File.Exists(logoPath)) options.LogoBytes = File.ReadAllBytes(logoPath);
            }

            var footerText = string.IsNullOrWhiteSpace(options.BrandingFooterText)
                ? "منصة مينا لإدارة الفعاليات"
                : options.BrandingFooterText!;

            // Colors (keep backgrounds), allow overriding text color
            var primary = string.IsNullOrWhiteSpace(options.TableHeaderBackgroundColorHex)
                ? Colors.Teal.Medium
                : options.TableHeaderBackgroundColorHex!;
            var sectionColor = Colors.Grey.Darken2;
            var accent = Colors.Teal.Darken2;

            // Resolve font family and base font size, and text color
            var fontFamily = !string.IsNullOrWhiteSpace(options.FontFamily) ? options.FontFamily! : EnsureArabicFont();
            var baseFontSize = options.BaseFontSize > 0 ? options.BaseFontSize : 11;
            var textColor = string.IsNullOrWhiteSpace(options.FontColorHex) ? Colors.Black : options.FontColorHex!;
            var headerTextColor = string.IsNullOrWhiteSpace(options.FontColorHex) ? Colors.White : textColor;

            // Pre-process background image for real opacity if provided
            byte[]? bgBytesToUse = options.BackgroundImageBytes;
            if (bgBytesToUse != null && bgBytesToUse.Length > 0)
            {
                var op = Math.Clamp(options.BackgroundOpacity, 0f, 1f);
                if (op >= 0f && op < 1f)
                {
                    bgBytesToUse = TryApplyOpacity(bgBytesToUse, op);
                }
            }
                // Ensure the background image covers A4 page ratio to avoid empty margins
                bgBytesToUse = TryEnsureA4Cover(bgBytesToUse, 1240, 1754) ?? bgBytesToUse;


            // Prepare verification (QR) if requested
            byte[]? qrCodeBytes = null;
            string? verificationUrl = null;
            if (!string.IsNullOrWhiteSpace(options.VerificationUrlBase) && options.VerificationId.HasValue)
            {
                // 1) Generate the QR and URL first (do not tie to DB persistence)
                try
                {
                    verificationUrl = $"{options.VerificationUrlBase!.TrimEnd('/')}/verify/{options.VerificationId.Value}";
                    using var qrGenerator = new QRCodeGenerator();
                    using var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrCodeData);
                    qrCodeBytes = qrCode.GetGraphic(10); // compact graphic, we'll size in footer
                    _logger.LogInformation("[PdfExport] QR prepared. Bytes={Len} Url={Url}", qrCodeBytes?.Length ?? 0, verificationUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PdfExport] Failed to generate QR bytes; continuing without QR");
                    qrCodeBytes = null;
                }

                // 2) Try to persist verification record separately; failure must not hide QR in the PDF
                if (qrCodeBytes != null && !string.IsNullOrWhiteSpace(verificationUrl))
                {
                    try
                    {
                        var pdfType = string.IsNullOrWhiteSpace(options.VerificationType) ? "Results" : options.VerificationType!;
                        _db.PdfVerifications.Add(new EvenDAL.Models.Classes.PdfVerification
                        {
                            PdfVerificationId = options.VerificationId.Value,
                            EventId = eventId,
                            PdfType = pdfType,
                            ExportedAtUtc = DateTime.UtcNow,
                            VerificationUrl = verificationUrl
                        });
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[PdfExport] Failed to persist verification record; QR will still be rendered");
                    }
                }
            }

            try
            {
                var bytes = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Margin(25);
                        page.Size(PageSizes.A4);

                        // Optional page background image (already opacity-processed)
                        if (bgBytesToUse != null && bgBytesToUse.Length > 0)
                        {
                            page.Background().Element(bg =>
                            {
                                bg.Image(bgBytesToUse).FitArea();
                            });
                        }

                        page.DefaultTextStyle(TextStyle.Default
                            .FontFamily(fontFamily)
                            .FontSize(baseFontSize)
                            .FontColor(textColor));

                        // Header with optional logo and large title
                        page.Header().Column(h =>
                        {
                            h.Spacing(6);
                            h.Item().Row(r =>
                            {
                                if (options.LogoBytes != null && options.LogoBytes.Length > 0)
                                    r.AutoItem().Height(50).Image(options.LogoBytes);
                                r.RelativeItem().AlignRight().Text(options.CustomTitle ?? $"{eventTitle}")
                                    .FontSize(24).Bold().FontColor(textColor);
                            });

                            if (options.IncludeEventDetails && evt != null)
                            {
                                h.Item().Row(row =>
                                {
                                    row.RelativeItem().AlignRight().Text($"التاريخ: {evt.StartAt:yyyy/MM/dd} - {evt.EndAt:yyyy/MM/dd}").FontSize(10);
                                    if (org != null)
                                        row.AutoItem().Text($"الجهة: {org.Name}").FontSize(10);
                                });
                            }
                        });

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            // 1) Event content first: Sections with body, attachments (images), and tables
                            if (options.IncludeSections && sections.Any())
                            {
                                col.Item().AlignRight().Text("محتوى الحدث").Bold().FontSize(16).FontColor(textColor);
                                foreach (var s in sections)
                                {
                                    col.Item().BorderBottom(0.5f).PaddingBottom(6).AlignRight().Text(s.Title)
                                        .Bold().FontSize(13).FontColor(string.IsNullOrWhiteSpace(options.FontColorHex) ? sectionColor : textColor);
                                    if (!string.IsNullOrWhiteSpace(s.Body))
                                        col.Item().AlignRight().Text(s.Body);

                                    // Section tables
                                    var secTables = tables.Where(t => t.SectionId == s.SectionId).ToList();
                                    foreach (var t in secTables)
                                    {
                                        col.Item().Element(BuildTable(t));
                                    }

                                    // Section attachments (images only)
                                    if (options.IncludeAttachments)
                                    {
                                        var imgs = attachments
                                            .Where(a => a.SectionId == s.SectionId && a.Type == AttachmentType.Image)
                                            .GroupBy(a => a.AttachmentId)
                                            .Select(g => g.First())
                                            .ToList();
                                        if (imgs.Any())
                                        {
                                            var imageBytes = new List<byte[]>();
                                            foreach (var img in imgs)
                                            {
                                                var bytes = TryReadWebFileBytes(img.Path);
                                                if (bytes != null) imageBytes.Add(bytes);
                                            }
                                            if (imageBytes.Count > 0)
                                            {
                                                col.Item().AlignRight().Text("الصور").SemiBold().FontColor(textColor);
                                                col.Item().Element(RenderImagesGrid(imageBytes, 3));
                                            }
                                        }
                                    }
                                }
                            }

                            // Any event-level tables (not attached to a section)
                            var eventLevelTables = tables.Where(t => t.SectionId == null).ToList();
                            foreach (var t in eventLevelTables) col.Item().Element(BuildTable(t));

                            // 2) Participants details as structured tables
                            if (options.IncludeSurveyAndResponses || options.IncludeDiscussions)
                            {
                                // Survey answers table
                                if (options.IncludeSurveyAndResponses)
                                {
                                    var surveyRows = new List<List<string>>();
                                    foreach (var uid in participantIds)
                                    {
                                        var user = users.FirstOrDefault(u => u.UserId == uid);
                                        if (answersByUser.TryGetValue(uid, out var answers) && answers.Any())
                                        {
                                            foreach (var a in answers)
                                            {
                                                var name = user?.FullName ?? "-";
                                                var question = a.Question?.Text ?? string.Empty;
                                                var answer = string.Join("، ", a.SelectedOptions.Select(so => so.Option.Text));
                                                surveyRows.Add(new List<string> { name, question, answer });
                                            }
                                        }
                                    }
                                    if (surveyRows.Count > 0)
                                    {
                                        col.Item().AlignRight().Text("إجابات الاستبيانات").Bold().FontSize(16).FontColor(textColor);
                                        col.Item().Element(BuildGridTable(new[] { "اسم المشارك", "السؤال", "الإجابة" }, surveyRows, new float[] { 2f, 4f, 4f }));
                                    }
                                }

                                // Discussion replies table
                                if (options.IncludeDiscussions)
                                {
                                    var discussionRows = new List<List<string>>();
                                    foreach (var uid in participantIds)
                                    {
                                        var user = users.FirstOrDefault(u => u.UserId == uid);
                                        if (repliesByUser.TryGetValue(uid, out var replies) && replies.Any())
                                        {
                                            foreach (var r in replies)
                                            {
                                                var name = user?.FullName ?? "-";
                                                var topic = r.Discussion?.Title ?? string.Empty;
                                                var reply = r.Body ?? string.Empty;
                                                discussionRows.Add(new List<string> { name, topic, reply });
                                            }
                                        }
                                    }
                                    if (discussionRows.Count > 0)
                                    {
                                        col.Item().AlignRight().Text("الردود في النقاشات").Bold().FontSize(16).FontColor(textColor);
                                        col.Item().Element(BuildGridTable(new[] { "اسم المشارك", "موضوع النقاش", "الرد/التعليق" }, discussionRows, new float[] { 2f, 4f, 4f }));
                                    }
                                }
                            }

                            // 3) Participants summary table with signature at the end
                            if (options.IncludeSignatures)
                            {
                                _logger.LogInformation("[PdfExport] Building participants table for Event {EventId} with {Count} rows", eventId, participantIds.Count);
                                col.Item().AlignRight().Text("ملخص المشاركين").Bold().FontSize(16).FontColor(textColor);
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);   // التوقيع (يسار)
                                        columns.RelativeColumn(1.2f); // الدور/المنصب
                                        columns.RelativeColumn(2.5f); // اسم المشارك
                                        columns.RelativeColumn(0.7f); // الترتيب (يمين)
                                    });

                                    // header (من اليسار إلى اليمين في الترتيب البرمجي)
                                    t.Header(h =>
                                    {
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("التوقيع").FontColor(headerTextColor);
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("الدور/المنصب").FontColor(headerTextColor);
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("اسم المشارك").FontColor(headerTextColor);
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("الترتيب").FontColor(headerTextColor);
                                    });

                                    int order = 1;
                                    foreach (var uid in participantIds)
                                    {
                                        var user = users.FirstOrDefault(u => u.UserId == uid);
                                        var name = user?.FullName ?? "-";
                                        string roleText;
                                        if (participantRolesByUser.TryGetValue(uid, out var pRole))
                                        {
                                            roleText = pRole switch
                                            {
                                                ParticipantRole.Organizer => "منظم",
                                                ParticipantRole.Attendee => "مشارك",
                                                ParticipantRole.Observer => "مراقب",
                                                _ => "—"
                                            };
                                        }
                                        else
                                        {
                                            roleText = user?.Role switch
                                            {
                                                UserRole.Admin => "المدير التنفيذي",
                                                UserRole.Organizer => "عضو مجلس إدارة",
                                                UserRole.Attendee => "عضو",
                                                _ => "—"
                                            };
                                        }

                                        // التوقيع (أول خلية يسارًا)
                                        byte[]? sigBytes = null;
                                        if (signaturesByUser.TryGetValue(uid, out var sig) && sig != null)
                                        {
                                            sigBytes = TryReadWebFileBytes(sig.ImagePath) ?? TryReadWebFileBytes($"/uploads/signatures/{eventId}/{uid}.png");
                                        }
                                        t.Cell().Border(0.5f).Padding(6).Element(cell =>
                                        {
                                            if (sigBytes != null)
                                                cell.Height(40).Image(sigBytes).FitArea();
                                            else
                                                cell.AlignCenter().Text("—");
                                        });

                                        // الدور
                                        t.Cell().Border(0.5f).Padding(6).AlignRight().Text(roleText);
                                        // الاسم
                                        t.Cell().Border(0.5f).Padding(6).AlignRight().Text(name);
                                        // الترتيب (أقصى اليمين)
                                        t.Cell().Border(0.5f).Padding(6).AlignCenter().Text(order.ToString());
                                        order++;
                                    }
                                });
                            }
                        });

                        page.Footer().Row(f =>
                        {
                            var showQr = options.ShowQrCode && qrCodeBytes != null;
                            var qrSize = Math.Clamp(options.QrCodeSize, 30, 200);
                            var pos = options.QrCodePosition ?? "BottomLeft";
                            var showUrlText = !string.IsNullOrWhiteSpace(verificationUrl) && options.ShowVerificationUrl;

                            // Left slot
                            f.AutoItem().Element(left =>
                            {
                                if (showQr && pos.Equals("BottomLeft", StringComparison.OrdinalIgnoreCase))
                                    left.Height(qrSize).Width(qrSize).Image(qrCodeBytes!);
                            });

                            // Center slot (branding + optional URL and/or QR if centered)
                            f.RelativeItem().AlignCenter().Column(center =>
                            {
                                if (showQr && pos.Equals("BottomCenter", StringComparison.OrdinalIgnoreCase))
                                    center.Item().Height(qrSize).Width(qrSize).Image(qrCodeBytes!);

                                center.Item().Text(txt =>
                                {
                                    txt.Span(footerText).FontSize(9);
                                    if (showUrlText)
                                    {
                                        txt.Line(" ");
                                        txt.Span(verificationUrl!).FontSize(7).FontColor(Colors.Grey.Darken2);
                                    }
                                });
                            });

                            // Right slot (page numbers always, QR optionally below when positioned right)
                            f.AutoItem().Column(right =>
                            {
                                right.Item().Text(txt =>
                                {
                                    txt.Span(" ");
                                    txt.CurrentPageNumber();
                                    txt.Span(" / ");
                                    txt.TotalPages();
                                });

                                if (showQr && pos.Equals("BottomRight", StringComparison.OrdinalIgnoreCase))
                                    right.Item().Height(qrSize).Width(qrSize).Image(qrCodeBytes!);
                            });
                        });
                    });
                }).GeneratePdf();

                return bytes;
            }
            catch
            {
                return System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");
            }
        }
        // Helpers
        private string MapWebPathToFile(string webPath)
        {
            var rel = (webPath ?? string.Empty).TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
            var root = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot");
            return System.IO.Path.Combine(root, rel);
        }

        private byte[]? TryReadWebFileBytes(string? webPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(webPath)) return null;
                var full = MapWebPathToFile(webPath);
                return System.IO.File.Exists(full) ? System.IO.File.ReadAllBytes(full) : null;
            }
            catch { return null; }
        }

        private Action<IContainer> BuildTable(EvenDAL.Models.Classes.TableBlock t)
        {
            return container =>
            {
                // Parse rows JSON
                var rows = new List<List<string>>();
                var hasHeader = t.HasHeader;
                try
                {
                    if (!string.IsNullOrWhiteSpace(t.RowsJson))
                    {
                        using var doc = JsonDocument.Parse(t.RowsJson);
                        if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rowEl in rowsEl.EnumerateArray())
                            {
                                var row = new List<string>();
                                if (rowEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var cellEl in rowEl.EnumerateArray())
                                    {
                                        var val = cellEl.ValueKind == JsonValueKind.String ? cellEl.GetString() ?? string.Empty :
                                                  (cellEl.ValueKind == JsonValueKind.Object && cellEl.TryGetProperty("value", out var v) ? v.GetString() ?? string.Empty : string.Empty);
                                        row.Add(val);
                                    }
                                }
                                else if (rowEl.ValueKind == JsonValueKind.Object && rowEl.TryGetProperty("cells", out var cellsEl) && cellsEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var cellEl in cellsEl.EnumerateArray())
                                        row.Add(cellEl.ValueKind == JsonValueKind.String ? (cellEl.GetString() ?? string.Empty) : string.Empty);
                                }
                                if (row.Count > 0) rows.Add(row);
                            }
                        }
                    }
                }
                catch { /* ignore malformed */ }

                if (!rows.Any()) return; // nothing to render

                container.Column(c =>
                {
                    c.Spacing(4);
                    c.Item().AlignRight().Text(t.Title).Bold();
                    if (!string.IsNullOrWhiteSpace(t.Description)) c.Item().AlignRight().Text(t.Description);
                    c.Item().Table(tab =>
                    {
                        // define columns based on max cell count
                        var maxCols = rows.Max(r => r.Count);
                        tab.ColumnsDefinition(cols =>
                        {
                            for (int i = 0; i < maxCols; i++) cols.RelativeColumn();
                        });

                        int startRow = 0;
                        if (hasHeader && rows.Count > 0)
                        {
                            tab.Header(h =>
                            {
                                foreach (var cell in rows[0])
                                    h.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(6).AlignRight().Text(cell).Bold();
                            });
                            startRow = 1;
                        }

                        for (int r = startRow; r < rows.Count; r++)
                        {
                            var row = rows[r];
                            foreach (var cell in row)
                                tab.Cell().Border(0.5f).Padding(5).AlignRight().Text(cell);
                        }
                    });
                });
            };
        }
        private Action<IContainer> BuildGridTable(string[] headers, List<List<string>> rows, float[]? weights = null)
        {
            return container =>
            {
                if (headers == null || headers.Length == 0 || rows == null || rows.Count == 0)
                    return;

                container.Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        if (weights != null && weights.Length == headers.Length)
                        {
                            foreach (var w in weights) cols.RelativeColumn(w);
                        }
                        else
                        {
                            for (int i = 0; i < headers.Length; i++) cols.RelativeColumn();
                        }
                    });

                    // Header row with distinct background
                    t.Header(h =>
                    {
                        foreach (var head in headers)
                            h.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(6).AlignRight().Text(head).Bold();
                    });

                    foreach (var row in rows)
                    {
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = i < row.Count ? row[i] : string.Empty;
                            t.Cell().Border(0.5f).Padding(5).AlignRight().Text(cell);
                        }
                    }
                });
            };
        }
        private string EnsureArabicFont()
        {
            try
            {
                // Try preferred families in order
                var candidates = new[]
                {
                    new { Family = "Cairo", Regular = "wwwroot/fonts/Cairo-Regular.ttf", Bold = "wwwroot/fonts/Cairo-Bold.ttf" },
                    new { Family = "Noto Kufi Arabic", Regular = "wwwroot/fonts/NotoKufiArabic-Regular.ttf", Bold = "wwwroot/fonts/NotoKufiArabic-Bold.ttf" },
                    new { Family = "Tajawal", Regular = "wwwroot/fonts/Tajawal-Regular.ttf", Bold = "wwwroot/fonts/Tajawal-Bold.ttf" }
                };

                foreach (var c in candidates)
                {
                    var regularPath = System.IO.Path.Combine(_env.ContentRootPath, c.Regular.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    var boldPath = System.IO.Path.Combine(_env.ContentRootPath, c.Bold.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(regularPath))
                    {
                        using var reg = System.IO.File.OpenRead(regularPath);
                        QuestPDF.Drawing.FontManager.RegisterFont(reg);
                        if (System.IO.File.Exists(boldPath))
                        {
                            using var b = System.IO.File.OpenRead(boldPath);
                            QuestPDF.Drawing.FontManager.RegisterFont(b);
                        }
                        return c.Family;
                    }
                }
            }
            catch { }
            // Fallback to system font if custom not found
            return "Segoe UI";
        }

        private Action<IContainer> RenderImagesGrid(List<byte[]> images, int columns = 3)
        {
            return container =>
            {
                if (images == null || images.Count == 0) return;
                var colCount = Math.Max(1, columns);
                int index = 0;
                while (index < images.Count)
                {
                    var count = Math.Min(colCount, images.Count - index);
                    var slice = images.GetRange(index, count);
                    index += count;

                    container.Row(row =>
                    {
                        row.Spacing(8);
                        foreach (var bytes in slice)
                        {
                            row.RelativeItem().Padding(2).Height(120).Image(bytes).FitArea();
                        }
                        // fill remaining cells to keep alignment
                        for (int i = slice.Count; i < colCount; i++)
                            row.RelativeItem();
                    });
                }
            };
        }

        private byte[]? TryApplyOpacity(byte[] imageBytes, float opacity)
        {
            try
            {
                opacity = Math.Clamp(opacity, 0f, 1f);
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            ref Rgba32 p = ref row[x];
                            p.A = (byte)(p.A * opacity);
                        }
                    }
                });
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PdfExport] Failed to apply background opacity, using original image bytes");
                return imageBytes;
            }
        }


        // Resize and center-crop the image to cover an A4 portrait page at given target size
        private byte[]? TryEnsureA4Cover(byte[] imageBytes, int targetWidth, int targetHeight)
        {
            try
            {
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

                // Scale image so that both dimensions are >= target
                var scale = Math.Max(targetWidth / (double)img.Width, targetHeight / (double)img.Height);
                int newW = Math.Max(1, (int)Math.Ceiling(img.Width * scale));
                int newH = Math.Max(1, (int)Math.Ceiling(img.Height * scale));

                img.Mutate(ctx => ctx.Resize(newW, newH));

                // Center-crop to exact target size
                int x = Math.Max(0, (newW - targetWidth) / 2);
                int y = Math.Max(0, (newH - targetHeight) / 2);
                var rect = new SixLabors.ImageSharp.Rectangle(x, y, Math.Min(targetWidth, newW), Math.Min(targetHeight, newH));
                using var cropped = img.Clone(ctx => ctx.Crop(rect));

                using var ms = new MemoryStream();
                cropped.SaveAsPng(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PdfExport] Failed to normalize background to A4 cover, using original image bytes");
                return imageBytes;
            }
        }




    }
}

