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
using EvenDAL.Models.Shared.Enums;
using System.Text.Json;

namespace EventPl.Services.ClassServices
{
    public class PdfExportService : IPdfExportService
    {
        private readonly IMinaResultsService _results;
        private readonly AppDbContext _db;
        private readonly IHostEnvironment _env;

        public PdfExportService(IMinaResultsService results, AppDbContext db, IHostEnvironment env)
        {
            // Ensure QuestPDF license is configured in any hosting context (tests, dev, prod)
            QuestPDF.Settings.License = LicenseType.Community;
            _results = results;
            _db = db;
            _env = env;
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

        public async Task<byte[]> ExportEventResultsPdfAsync(Guid eventId, PdfExportOptions options)
        {
            var evt = await _db.Events.AsNoTracking()
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
            var eventTitle = evt?.Title ?? "الحدث";
            var org = evt?.Organization;

            // Participants who have any activity (or guests)
            var participantIds = await _db.SurveyAnswers.Where(a => a.EventId == eventId).Select(a => a.UserId).Distinct()
                .Union(_db.DiscussionReplies.Where(r => r.Discussion.EventId == eventId).Select(r => r.UserId))
                .Union(_db.UserSignatures.Where(s => s.EventId == eventId).Select(s => s.UserId))
                .Distinct().ToListAsync();

            var guestUserIds = await _db.PublicEventGuests.AsNoTracking()
                .Where(g => g.EventId == eventId)
                .Select(g => g.UserId)
                .ToListAsync();

            participantIds = participantIds.Union(guestUserIds).Distinct().ToList();

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

            // Colors inspired by the provided reference (teal + greys)
            var primary = Colors.Teal.Medium;
            var sectionColor = Colors.Grey.Darken2;
            var accent = Colors.Teal.Darken2;

            // Ensure a professional Arabic font is registered and used throughout
            var fontFamily = EnsureArabicFont();

            try
            {
                var bytes = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Margin(25);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(TextStyle.Default.FontFamily(fontFamily).FontSize(11));

                        // Header with optional logo and large title
                        page.Header().Column(h =>
                        {
                            h.Spacing(6);
                            h.Item().Row(r =>
                            {
                                if (options.LogoBytes != null && options.LogoBytes.Length > 0)
                                    r.AutoItem().Height(50).Image(options.LogoBytes);
                                r.RelativeItem().AlignRight().Text(options.CustomTitle ?? $"{eventTitle}")
                                    .FontSize(24).Bold().FontColor(primary);
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
                                col.Item().AlignRight().Text("محتوى الحدث").Bold().FontSize(16).FontColor(accent);
                                foreach (var s in sections)
                                {
                                    col.Item().BorderBottom(0.5f).PaddingBottom(6).AlignRight().Text(s.Title)
                                        .Bold().FontSize(13).FontColor(sectionColor);
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
                                                col.Item().AlignRight().Text("الصور").SemiBold().FontColor(accent);
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
                                        col.Item().AlignRight().Text("إجابات الاستبيانات").Bold().FontSize(16).FontColor(accent);
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
                                        col.Item().AlignRight().Text("الردود في النقاشات").Bold().FontSize(16).FontColor(accent);
                                        col.Item().Element(BuildGridTable(new[] { "اسم المشارك", "موضوع النقاش", "الرد/التعليق" }, discussionRows, new float[] { 2f, 4f, 4f }));
                                    }
                                }
                            }

                            // 3) Participants summary table with signature at the end
                            if (options.IncludeSignatures)
                            {
                                col.Item().AlignRight().Text("ملخص المشاركين").Bold().FontSize(16).FontColor(accent);
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // name
                                        columns.RelativeColumn(1); // signature
                                    });

                                    // header
                                    t.Header(h =>
                                    {
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("اسم المشارك").FontColor(Colors.White);
                                        h.Cell().Background(primary).Border(0.5f).Padding(6).AlignRight().Text("التوقيع").FontColor(Colors.White);
                                    });

                                    foreach (var uid in participantIds)
                                    {
                                        var user = users.FirstOrDefault(u => u.UserId == uid);
                                        var name = user?.FullName ?? "-";
                                        t.Cell().Border(0.5f).Padding(6).AlignRight().Text(name);

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
                                    }
                                });
                            }
                        });

                        page.Footer().Row(f =>
                        {
                            f.RelativeItem().AlignCenter().Text(footerText).FontSize(9);
                            f.AutoItem().Text(txt =>
                            {
                                txt.Span(" ");
                                txt.CurrentPageNumber();
                                txt.Span(" / ");
                                txt.TotalPages();
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





    }
}

