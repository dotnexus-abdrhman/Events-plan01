using EvenDAL.Data.Configuration;
using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RouteDAl.Data.Contexts
{
    public class AppDbContext : DbContext
    {
        // ================================
        // DbSets (الجداول)
        // ================================
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<PlatformAdmin> PlatformAdmins { get; set; }

        // البنود والقرارات
        public DbSet<Section> Sections { get; set; }
        public DbSet<Decision> Decisions { get; set; }
        public DbSet<DecisionItem> DecisionItems { get; set; }

        // الاستبيانات
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyOption> SurveyOptions { get; set; }
        public DbSet<SurveyAnswer> SurveyAnswers { get; set; }
        public DbSet<SurveyAnswerOption> SurveyAnswerOptions { get; set; }

        // النقاشات
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<DiscussionReply> DiscussionReplies { get; set; }

        // الجداول والمرفقات
        public DbSet<TableBlock> TableBlocks { get; set; }
        public DbSet<Attachment> Attachments { get; set; }

        // التوقيعات والتدقيق
        public DbSet<UserSignature> UserSignatures { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // الجداول القديمة (للتوافق)
        public DbSet<EventParticipant> EventParticipants { get; set; }
        public DbSet<AgendaItem> AgendaItems { get; set; }
        public DbSet<VotingSession> VotingSessions { get; set; }
        public DbSet<VotingOption> VotingOptions { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<Localization> Localizations { get; set; }
        public DbSet<AppModule> AppModules { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DiscussionPost> DiscussionPosts { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<ProposalUpvote> ProposalUpvotes { get; set; }
        // إخفاء الأحداث على مستوى المستخدم فقط
        public DbSet<UserHiddenEvent> UserHiddenEvents { get; set; }
        public DbSet<EventPublicLink> EventPublicLinks { get; set; }
        public DbSet<PublicEventGuest> PublicEventGuests { get; set; }

        // ================================
        // Constructor
        // ================================
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ================================
        // Model Creating
        // ================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations automatically
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            // UserHiddenEvents mapping (composite key)
            modelBuilder.Entity<UserHiddenEvent>(b =>
            {
                b.HasKey(x => new { x.UserId, x.EventId });
                b.Property(x => x.HiddenAt).HasDefaultValueSql("SYSUTCDATETIME()");
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            });

            // EventPublicLink mapping
            modelBuilder.Entity<EventPublicLink>(b =>
            {
                b.HasIndex(x => x.Token).IsUnique();
                b.Property(x => x.Token).HasMaxLength(100);
                b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            });

            // PublicEventGuest mapping
            modelBuilder.Entity<PublicEventGuest>(b =>
            {
                b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
                b.Property(x => x.FullName).HasMaxLength(150);
                b.Property(x => x.Email).HasMaxLength(150);
                b.Property(x => x.Phone).HasMaxLength(20);
                b.Property(x => x.UniqueToken).HasMaxLength(100);
                b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Apply enums configuration
            modelBuilder.ConfigureEnums();

            // ================================
            // Seed البيانات الأساسية فقط
            // ================================
            var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            // جهة واحدة تجريبية
            modelBuilder.Entity<Organization>().HasData(new Organization
            {
                OrganizationId = orgId,
                Name = "الجهة التجريبية",
                NameEn = "Test Organization",
                Type = EvenDAL.Models.Shared.Enums.OrganizationType.Other,
                Logo = string.Empty,
                PrimaryColor = "#0d6efd",
                SecondaryColor = "#6c757d",
                Settings = "{}",
                LicenseKey = "MINA-SEED-2025",
                LicenseExpiry = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            });

            // أدمن واحد
            modelBuilder.Entity<PlatformAdmin>().HasData(new PlatformAdmin
            {
                Id = adminId,
                Email = "admin@mina.local",
                FullName = "مدير النظام",
                Phone = "0500000001",
                ProfilePicture = string.Empty,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastLogin = null
            });

            // مستخدم واحد
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = userId,
                OrganizationId = orgId,
                FullName = "مستخدم تجريبي",
                Email = "user@mina.local",
                Phone = "0500000000",
                Role = EvenDAL.Models.Shared.Enums.UserRole.Attendee,
                ProfilePicture = string.Empty,
                IsActive = true,
                LastLogin = null,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        // ================================
        // Override SaveChanges → لتحديث UpdatedAt
        // ================================
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Event eventEntity)
                {
                    eventEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
