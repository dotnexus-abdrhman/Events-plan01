using EvenDAL.Repositories.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Services.ClassServices;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using QuestPDF.Infrastructure;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.ResponseCompression;

using RouteDAl.Data.Contexts;
using EvenDAL.Models.Classes;

using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace RourtPPl01
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure QuestPDF license for non-production/test runs
            QuestPDF.Settings.License = LicenseType.Community;

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddCookieTempDataProvider();

            // In-memory cache for lightweight cross-request hints (e.g., recent broadcast title)
            builder.Services.AddMemoryCache();

            // Response compression to reduce payload sizes and improve page load times
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/html", "application/json" });
            });

            // Cookie Authentication (Default Scheme)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    // Auth cookie
                    options.Cookie.Name = ".mina.auth";
                    options.Cookie.Path = "/";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
                    // Prevent header-too-large causing HTTP 400 by chunking large cookies safely
                    options.CookieManager = new ChunkingCookieManager();
                    // لا تقم بعمل Redirect تلقائي لطلبات POST عند عدم المصادقة/منع الوصول؛ أعِد شيفرة حالة بدلاً من ذلك
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            }
                            context.Response.Redirect(context.RedirectUri);
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            // Always return 403 instead of redirecting to a page to make API/UI behavior explicit
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        },
                        OnValidatePrincipal = async context =>
                        {
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthValidation");
                            bool fromCache = true;
                            try
                            {
                                var principal = context.Principal;
                                if (principal == null)
                                {
                                    context.RejectPrincipal();
                                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                    return;
                                }
                                var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                                if (string.IsNullOrWhiteSpace(userIdStr))
                                {
                                    context.RejectPrincipal();
                                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                    return;
                                }

                                // Throttle DB validations with a short-lived memory cache to reduce per-request overhead
                                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                                var cacheKey = principal.IsInRole("PlatformAdmin") ? $"admin-active-{userIdStr}" : $"user-active-{userIdStr}";
                                if (!cache.TryGetValue<bool>(cacheKey, out var isActiveCached))
                                {
                                    fromCache = false;
                                    using var scope = context.HttpContext.RequestServices.CreateScope();
                                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                                    if (principal.IsInRole("PlatformAdmin"))
                                    {
                                        if (!Guid.TryParse(userIdStr, out var adminId))
                                        {
                                            context.RejectPrincipal();
                                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                            return;
                                        }
                                        var admin = await db.PlatformAdmins.AsNoTracking().FirstOrDefaultAsync(a => a.Id == adminId);
                                        isActiveCached = admin != null && admin.IsActive;
                                    }
                                    else
                                    {
                                        if (!Guid.TryParse(userIdStr, out var uid))
                                        {
                                            context.RejectPrincipal();
                                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                            return;
                                        }
                                        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == uid);
                                        isActiveCached = user != null && user.IsActive;
                                    }

                                    // cache the validation result with a TTL tuned by area to avoid DB on every request
                                    var reqPath = context.HttpContext.Request.Path.Value ?? string.Empty;
                                    var ttl = reqPath.StartsWith("/UserPortal", StringComparison.OrdinalIgnoreCase)
                                        ? TimeSpan.FromMinutes(5) // longer cache for UserPortal to speed up user pages
                                        : TimeSpan.FromSeconds(60); // keep tighter window for Admin and others
                                    cache.Set(cacheKey, isActiveCached, ttl);
                                }

                                if (!isActiveCached)
                                {
                                    context.RejectPrincipal();
                                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "OnValidatePrincipal error");
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                            finally
                            {
                                sw.Stop();
                                logger.LogInformation("OnValidatePrincipal {CacheStatus} in {Elapsed} ms for {Path}", fromCache ? "cache-hit" : "cache-miss", sw.ElapsedMilliseconds, context.HttpContext.Request.Path.Value);
                            }
                        }
                    };
                });


            // Antiforgery cookie configuration
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = ".mina.af";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
            });

            #region configration services
            builder.Services.AddDbContext<AppDbContext>(op =>
            {
                var cs = builder.Configuration.GetConnectionString("DefaultconnectionString");
                op.UseSqlServer(cs);
            });
            builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Repository ��������
            builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

            // AutoMapper
            builder.Services.AddAutoMapper(typeof(EventPl.Mapping.MinaEventsProfile));

            // Services - Legacy (Old System)
            builder.Services.AddScoped<ICrudService<OrganizationDto, Guid>, OrganizationService>();
            builder.Services.AddScoped<ICrudService<UserDto, Guid>, UserService>();
            builder.Services.AddScoped<ICrudService<EventDto, Guid>, EventService>();
            builder.Services.AddScoped<ICrudService<EventParticipantDto, Guid>, EventParticipantService>();
            builder.Services.AddScoped<ICrudService<AgendaItemDto, Guid>, AgendaItemService>();
            builder.Services.AddScoped<ICrudService<VotingSessionDto, Guid>, VotingSessionService>();
            builder.Services.AddScoped<ICrudService<VotingOptionDto, Guid>, VotingOptionService>();
            builder.Services.AddScoped<ICrudService<VoteDto, Guid>, VoteService>();
            builder.Services.AddScoped<ICrudService<NotificationDto, Guid>, NotificationService>();
            builder.Services.AddScoped<ICrudService<AttendanceLogDto, Guid>, AttendanceLogService>();
            builder.Services.AddScoped<ICrudService<LocalizationDto, Guid>, LocalizationService>();
            builder.Services.AddScoped<ICrudService<ModuleDto, Guid>, ModuleService>();
            builder.Services.AddScoped<ICrudService<DocumentDto, Guid>, DocumentService>();
            builder.Services.AddScoped<IEventResultsService, EventResultsService>();
            builder.Services.AddScoped<ICrudService<AdminDto, Guid>, AdminService>();

            // Services - Mina Events (New System)
            builder.Services.AddScoped<IMinaEventsService, MinaEventsService>();
            builder.Services.AddScoped<ISectionsService, SectionsService>();
            builder.Services.AddScoped<ISurveysService, SurveysService>();
            builder.Services.AddScoped<IDiscussionsService, DiscussionsService>();
            builder.Services.AddScoped<ITableBlocksService, TableBlocksService>();
            builder.Services.AddScoped<IAttachmentsService, AttachmentsService>();
            builder.Services.AddScoped<ISignaturesService, SignaturesService>();
            builder.Services.AddScoped<IMinaResultsService, MinaResultsService>();
            builder.Services.AddScoped<IPdfExportService, PdfExportService>();

            // Auth Service
            builder.Services.AddScoped<IAuthService, EventPl.Services.AuthService>();









            #endregion

            var app = builder.Build();

            // Maintenance: Drop & Migrate then seed minimal sample when CLEANSE_DB=1
            if (Environment.GetEnvironmentVariable("CLEANSE_DB") == "1")
            {
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1) Drop and 2) Migrate
                    db.Database.EnsureDeleted();
                    try { db.Database.Migrate(); }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[CLEANUP] Migrate failed; falling back to EnsureCreated(): " + ex.Message);
                        db.Database.EnsureCreated();
                    }

                    // 3) Seed minimal sample: 3 Organizations total, 2 Users (excluding PlatformAdmin), 0 Events
                    var org1Id = Guid.Parse("11111111-1111-1111-1111-111111111111"); // seeded by modelBuilder
                    var org2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
                    var org3Id = Guid.Parse("55555555-5555-5555-5555-555555555555");

                    if (!db.Organizations.AsNoTracking().Any(o => o.OrganizationId == org2Id))
                    {
                        db.Organizations.Add(new Organization
                        {
                            OrganizationId = org2Id,
                            Name = "منظمة 2",
                            NameEn = "Org 2",
                            Type = EvenDAL.Models.Shared.Enums.OrganizationType.Other,
                            Logo = string.Empty,
                            PrimaryColor = "#0d6efd",
                            SecondaryColor = "#6c757d",
                            Settings = "{}",
                            LicenseKey = "MINA-SEED-ORG2",
                            LicenseExpiry = DateTime.UtcNow.AddYears(5),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        });
                    }

                    if (!db.Organizations.AsNoTracking().Any(o => o.OrganizationId == org3Id))
                    {
                        db.Organizations.Add(new Organization
                        {
                            OrganizationId = org3Id,
                            Name = "منظمة 3",
                            NameEn = "Org 3",
                            Type = EvenDAL.Models.Shared.Enums.OrganizationType.Other,
                            Logo = string.Empty,
                            PrimaryColor = "#0d6efd",
                            SecondaryColor = "#6c757d",
                            Settings = "{}",
                            LicenseKey = "MINA-SEED-ORG3",
                            LicenseExpiry = DateTime.UtcNow.AddYears(5),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        });
                    }

                    // Users: modelBuilder seeds one user already (user@mina.local). Add exactly one more regular user.
                    var user2Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
                    if (!db.Users.AsNoTracking().Any(u => u.UserId == user2Id))
                    {
                        db.Users.Add(new User
                        {
                            UserId = user2Id,
                            OrganizationId = org2Id,
                            FullName = "مستخدم تجريبي 2",
                            Email = "user2@mina.local",
                            Phone = "0500000002",
                            Role = EvenDAL.Models.Shared.Enums.UserRole.Organizer,
                            ProfilePicture = string.Empty,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Ensure zero Events
                    if (db.Events.Any())
                    {
                        db.Events.RemoveRange(db.Events);
                    }

                    db.SaveChanges();
                    Console.WriteLine("[CLEANUP] Drop & Migrate done with minimal seed (3 orgs, 2 users, 0 events). Exiting...");
                }

                return; // stop app after maintenance
            }


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // In Development, avoid redirecting POSTs from HTTP->HTTPS to prevent auth loss
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseRouting();

            // Enable response compression early in the pipeline (before MVC)
            app.UseResponseCompression();

            app.UseAuthentication();
            app.UseAuthorization();
            // Append a recent-broadcast hint only to specific HTML pages (optimized)
            // Ensure UTF-8 charset for HTML responses so HttpClient decodes Arabic correctly in tests and dev
            app.Use(async (ctx, next) =>
            {
                await next();
                var ct = ctx.Response.ContentType;
                if (!string.IsNullOrEmpty(ct)
                    && ct.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                    && !ct.Contains("charset", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.ContentType = "text/html; charset=utf-8";
                }
            });

            app.Use(async (context, next) =>
            {
                await next();
                try
                {
                    // Only for GET requests
                    if (!HttpMethods.IsGet(context.Request.Method)) return;

                    var path = context.Request.Path.Value ?? string.Empty;
                    // Limit to events pages to avoid overhead on all HTML
                    bool shouldAppend =
                        path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase);
                    if (!shouldAppend) return;

                    if (context.Response.StatusCode != StatusCodes.Status200OK) return;

                    var ct = context.Response.ContentType ?? string.Empty;
                    if (!ct.Contains("text/html", StringComparison.OrdinalIgnoreCase)) return;

                    // Use an in-memory cache to avoid a DB hit on most GETs, but bypass cache on Admin/Events pages to reflect fresh broadcasts immediately
                    var cache = context.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                    const string cacheKey = "recent-broadcast-title";

                    string? title = null;
                    var isEventsPage = path.Contains("/Admin/Events", StringComparison.OrdinalIgnoreCase);
                    if (isEventsPage)
                    {
                        using var scope = context.RequestServices.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<RouteDAl.Data.Contexts.AppDbContext>();
                        title = await db.Events.AsNoTracking().Where(e => e.IsBroadcast)
                            .OrderByDescending(e => e.CreatedAt)
                            .Select(e => e.Title)
                            .FirstOrDefaultAsync();
                        cache.Set(cacheKey, title ?? string.Empty, TimeSpan.FromSeconds(10));
                    }


                    else if (!cache.TryGetValue<string>(cacheKey, out title))
                    {
                        using var scope = context.RequestServices.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<RouteDAl.Data.Contexts.AppDbContext>();
                        title = await db.Events.AsNoTracking().Where(e => e.IsBroadcast)
                            .OrderByDescending(e => e.CreatedAt)
                            .Select(e => e.Title)
                            .FirstOrDefaultAsync();
                        // Cache even empty to throttle DB checks; refresh quickly
                        cache.Set(cacheKey, title ?? string.Empty, TimeSpan.FromSeconds(30));
                    }

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        await context.Response.WriteAsync($"\n<!-- recent-broadcast: {title} -->");
                    }
                }
                catch { /* non-fatal */ }
            });


            // Block direct access to merged custom PDF for non-admin users
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                if (path.Contains("/uploads/events/", StringComparison.OrdinalIgnoreCase) &&
                    path.EndsWith("custom-merged.pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // Only PlatformAdmin can access the merged PDF directly
                    if (!(context.User?.Identity?.IsAuthenticated == true && context.User.IsInRole("PlatformAdmin")))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }
                await next();
            });


            app.MapStaticAssets();

            // Redirect root to Login
            app.MapGet("/", context => {
                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            });

            // Area routes
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Auth}/{action=Login}/{id?}")
                .WithStaticAssets();


            // Apply migrations (if any) and seed minimal data if missing
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();

                        // Fallback: ensure IsBroadcast column exists (in case migrations couldn't run in CI/tests)
                        try
                        {
                            var ensureIsBroadcastSql = @"IF COL_LENGTH('dbo.Events','IsBroadcast') IS NULL BEGIN
    ALTER TABLE [dbo].[Events] ADD [IsBroadcast] bit NOT NULL CONSTRAINT [DF_Events_IsBroadcast] DEFAULT(0);
END";
                            db.Database.ExecuteSqlRaw(ensureIsBroadcastSql);
                        }
                        catch { /* ignore */ }
                        // Fallback: ensure Events.Title is NVARCHAR to preserve Arabic
                        try
                        {
                            var ensureUnicodeSql = @"IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id AND o.type = 'U' AND o.name = 'Events'
    WHERE c.name = 'Title' AND c.system_type_id = 167 -- varchar
)
BEGIN
    ALTER TABLE [dbo].[Events] ALTER COLUMN [Title] NVARCHAR(200) NOT NULL;
END
IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id AND o.type = 'U' AND o.name = 'Events'
    WHERE c.name = 'Description' AND c.system_type_id = 167 -- varchar
)
BEGIN
    ALTER TABLE [dbo].[Events] ALTER COLUMN [Description] NVARCHAR(MAX) NULL;
END";
                            db.Database.ExecuteSqlRaw(ensureUnicodeSql);
                        }
                        catch { /* ignore */ }



                        // Fallback: ensure PdfVerifications table exists for legacy databases where migrations cannot run
                        try
                        {
                            var ensurePdfVerificationsSql = @"IF OBJECT_ID('dbo.PdfVerifications','U') IS NULL BEGIN
    CREATE TABLE [dbo].[PdfVerifications] (
        [PdfVerificationId] uniqueidentifier NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [PdfType] nvarchar(50) NOT NULL,
        [ExportedAtUtc] datetime2 NOT NULL,
        [VerificationUrl] nvarchar(300) NOT NULL,
        CONSTRAINT [PK_PdfVerifications] PRIMARY KEY ([PdfVerificationId])
    );
    CREATE UNIQUE INDEX [IX_PdfVerifications_PdfVerificationId] ON [dbo].[PdfVerifications]([PdfVerificationId]);
END";
                            db.Database.ExecuteSqlRaw(ensurePdfVerificationsSql);
                        }
                        catch { /* ignore */ }
                        // Fallback: ensure EventInvitedUsers table exists for individual invitations
                        try
                        {
                            var ensureEventInvitedUsersSql = @"IF OBJECT_ID('dbo.EventInvitedUsers','U') IS NULL BEGIN
    CREATE TABLE [dbo].[EventInvitedUsers] (
        [EventInvitedUserId] uniqueidentifier NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [InvitedAt] datetime2 NOT NULL CONSTRAINT [DF_EventInvitedUsers_InvitedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EventInvitedUsers] PRIMARY KEY ([EventInvitedUserId])
    );
    CREATE UNIQUE INDEX [IX_EventInvitedUsers_EventId_UserId] ON [dbo].[EventInvitedUsers]([EventId],[UserId]);
END";
                            db.Database.ExecuteSqlRaw(ensureEventInvitedUsersSql);
                        }
                        catch { /* ignore */ }
                        // Performance: ensure useful indexes for MyEvents queries
                        try
                        {
                            var ensureIndexesSql = @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Events_IsBroadcast_StartAt' AND object_id = OBJECT_ID('dbo.Events'))
    CREATE INDEX [IX_Events_IsBroadcast_StartAt] ON [dbo].[Events]([IsBroadcast], [StartAt] DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Events_Org_Status_StartAt' AND object_id = OBJECT_ID('dbo.Events'))
    CREATE INDEX [IX_Events_Org_Status_StartAt] ON [dbo].[Events]([OrganizationId], [Status], [StartAt] DESC);";
                            db.Database.ExecuteSqlRaw(ensureIndexesSql);
                        }
                        catch { /* ignore */ }




                    var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                    if (!db.Organizations.AsNoTracking().Any(o => o.OrganizationId == orgId))
                    {
                        db.Organizations.Add(new Organization
                        {
                            OrganizationId = orgId,
                            Name = "الجهة الافتراضية",
                            NameEn = "Default Organization",
                            Type = EvenDAL.Models.Shared.Enums.OrganizationType.Other,
                            Logo = string.Empty,
                            PrimaryColor = "#0d6efd",
                            SecondaryColor = "#6c757d",
                            Settings = "{}",
                            LicenseKey = "MINA-SEED",
                            LicenseExpiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        });
                    }

                    var adminEmail = "admin@mina.local";
                    if (!db.PlatformAdmins.AsNoTracking().Any(a => a.Email == adminEmail))
                    {
                        db.PlatformAdmins.Add(new PlatformAdmin
                        {
                            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            Email = adminEmail,
                            FullName = "مدير النظام",
                            Phone = "0500000001",
                            ProfilePicture = string.Empty,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    var userPhone = "0500000000";
                    if (!db.Users.AsNoTracking().Any(u => (u.Phone ?? "") == userPhone))
                    {
                        db.Users.Add(new User
                        {
                            UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                            OrganizationId = orgId,
                            FullName = "مستخدم تجريبي",
                            Email = "user@mina.local",
                            Phone = userPhone,
                            Role = EvenDAL.Models.Shared.Enums.UserRole.Attendee,
                            ProfilePicture = string.Empty,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Seed demo event with components and sample responses if none exists
                    // Optional: seed a demo event only when explicitly enabled via configuration
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var seedDemo = config.GetValue<bool>("SeedDemoData", false);

                    var existingEvent = db.Events.AsNoTracking().FirstOrDefault(e => e.OrganizationId == orgId);
                    if (seedDemo && Environment.GetEnvironmentVariable("ENABLE_DEMO_SEED") == "1" && existingEvent == null)
                    {
                        var evId = Guid.NewGuid();
                        var creatorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
                        var demoEvent = new Event
                        {
                            EventId = evId,
                            OrganizationId = orgId,
                            CreatedById = creatorId,
                            Title = "ورشة تجريبية",
                            Description = "حدث تجريبي يحتوي على استبيان ونقاش وتوقيع.",
                            StartAt = DateTime.UtcNow.AddDays(-1),
                            EndAt = DateTime.UtcNow.AddDays(1),
                            Status = EvenDAL.Models.Shared.Enums.EventStatus.Active,
                            RequireSignature = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Events.Add(demoEvent);

                        // Survey with two questions
                        var surveyId = Guid.NewGuid();
                        var survey = new Survey { SurveyId = surveyId, EventId = evId, Title = "استبيان رئيسي", Order = 1, IsActive = true };
                        db.Surveys.Add(survey);

                        var q1Id = Guid.NewGuid();
                        var q1 = new SurveyQuestion
                        {
                            SurveyQuestionId = q1Id,
                            SurveyId = surveyId,
                            Text = "هل توافق؟",
                            Type = EvenDAL.Models.Shared.Enums.SurveyQuestionType.Single,
                            Order = 1,
                            IsRequired = true
                        };
                        db.SurveyQuestions.Add(q1);

                        var oYesId = Guid.NewGuid();
                        var oNoId = Guid.NewGuid();
                        db.SurveyOptions.AddRange(
                            new SurveyOption { SurveyOptionId = oYesId, QuestionId = q1Id, Text = "نعم", Order = 1 },
                            new SurveyOption { SurveyOptionId = oNoId, QuestionId = q1Id, Text = "لا", Order = 2 }
                        );

                        var q2Id = Guid.NewGuid();
                        var q2 = new SurveyQuestion
                        {
                            SurveyQuestionId = q2Id,
                            SurveyId = surveyId,
                            Text = "اختر الميزات المفضلة",
                            Type = EvenDAL.Models.Shared.Enums.SurveyQuestionType.Multiple,
                            Order = 2,
                            IsRequired = false
                        };
                        db.SurveyQuestions.Add(q2);

                        var o1Id = Guid.NewGuid();
                        var o2Id = Guid.NewGuid();
                        db.SurveyOptions.AddRange(
                            new SurveyOption { SurveyOptionId = o1Id, QuestionId = q2Id, Text = "السرعة", Order = 1 },
                            new SurveyOption { SurveyOptionId = o2Id, QuestionId = q2Id, Text = "الواجهة", Order = 2 }
                        );

                        // Discussion
                        var discId = Guid.NewGuid();
                        db.Discussions.Add(new Discussion { DiscussionId = discId, EventId = evId, Title = "نقاش عام", Purpose = "أفكار واقتراحات", Order = 1, IsActive = true });

                        // Sample user responses
                        var userId = creatorId;
                        var ans1Id = Guid.NewGuid();
                        db.SurveyAnswers.Add(new SurveyAnswer { SurveyAnswerId = ans1Id, EventId = evId, QuestionId = q1Id, UserId = userId, CreatedAt = DateTime.UtcNow });
                        db.SurveyAnswerOptions.Add(new SurveyAnswerOption { SurveyAnswerId = ans1Id, OptionId = oYesId });

                        var ans2Id = Guid.NewGuid();
                        db.SurveyAnswers.Add(new SurveyAnswer { SurveyAnswerId = ans2Id, EventId = evId, QuestionId = q2Id, UserId = userId, CreatedAt = DateTime.UtcNow });
                        db.SurveyAnswerOptions.Add(new SurveyAnswerOption { SurveyAnswerId = ans2Id, OptionId = o1Id });
                        db.SurveyAnswerOptions.Add(new SurveyAnswerOption { SurveyAnswerId = ans2Id, OptionId = o2Id });

                        db.DiscussionReplies.Add(new DiscussionReply { DiscussionReplyId = Guid.NewGuid(), DiscussionId = discId, UserId = userId, Body = "مشاركة تجريبية", CreatedAt = DateTime.UtcNow });



                        db.UserSignatures.Add(new UserSignature { UserSignatureId = Guid.NewGuid(), EventId = evId, UserId = userId, ImagePath = string.Empty, Data = "seed", CreatedAt = DateTime.UtcNow });
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Startup-Seed] {ex.Message}");
                }


            }

            app.Run();
        }
    }
}
