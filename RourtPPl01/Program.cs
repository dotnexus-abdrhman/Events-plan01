using EvenDAL.Repositories.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Services.ClassServices;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using QuestPDF.Infrastructure;

using RouteDAl.Data.Contexts;
using EvenDAL.Models.Classes;

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
            builder.Services.AddControllersWithViews();

            // Cookie Authentication (Default Scheme)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    // Auth cookie
                    options.Cookie.Name = ".mina.auth";
                    options.Cookie.Path = "/";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
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

            app.UseAuthentication();
            app.UseAuthorization();

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
                    var existingEvent = db.Events.AsNoTracking().FirstOrDefault(e => e.OrganizationId == orgId);
                    if (existingEvent == null)
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
