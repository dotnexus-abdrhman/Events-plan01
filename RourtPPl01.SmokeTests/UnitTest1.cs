using Xunit;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RourtPPl01.SmokeTests;

public class SmokeTests
{
    private static string ExtractAntiForgeryToken(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")?.Groups[1].Value ?? string.Empty;

    private static string? ExtractFirstHrefEventId(string html)
        => Regex.Match(html, "/UserPortal/EventParticipation/Details/([0-9a-fA-F-]{36})").Groups[1].Value;

    private static (Guid qId, Guid optId)? ExtractFirstSingleQuestionAndOption(string html)
    {
        var m = Regex.Match(html, @"name=""SurveyAnswers\[([0-9a-fA-F-]{36})\]""[\s\S]*?value=""([0-9a-fA-F-]{36})""");
        if (!m.Success) return null;
        return (Guid.Parse(m.Groups[1].Value), Guid.Parse(m.Groups[2].Value));
    }

    private static Guid? ExtractFirstDiscussionId(string html)
    {
        var m = Regex.Match(html, @"name=""DiscussionReplies\[([0-9a-fA-F-]{36})\]""");
        return m.Success ? Guid.Parse(m.Groups[1].Value) : null;
    }

    [Fact]
    public async Task Admin_Can_Create_Event_Minimal()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1) GET login to fetch anti-forgery token
        var loginGet = await client.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var afToken = ExtractAntiForgeryToken(loginHtml);
        Assert.False(string.IsNullOrEmpty(afToken));

        // 2) POST login (admin)
        var loginForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        });
        var loginPost = await client.PostAsync("/Auth/Login", loginForm);
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // 3) GET Create Event page
        var createGet = await client.GetAsync("/Admin/Events/Create");
        Assert.Equal(HttpStatusCode.OK, createGet.StatusCode);
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var createToken = ExtractAntiForgeryToken(createHtml);
        Assert.False(string.IsNullOrEmpty(createToken));

        // 4) POST create minimal event (select first organization)
        var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOption));
        var now = DateTime.UtcNow;
        var startAt = now.ToString("s");
        var endAt = now.AddHours(2).ToString("s");
        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", createToken),
            new KeyValuePair<string,string>("Title", "اختبار حدث جديد"),
            new KeyValuePair<string,string>("Description", "وصف تجريبي"),
            new KeyValuePair<string,string>("StartAt", startAt),
            new KeyValuePair<string,string>("EndAt", endAt),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Draft"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!)
        });
        var createPost = await client.PostAsync("/Admin/Events/Create", createForm);
        Assert.Equal(HttpStatusCode.Redirect, createPost.StatusCode);
        var location = createPost.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Admin/Events/Details/", location);

        // 5) GET the details page after creation and ensure it renders
        var details = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
    }

    [Fact]
    public async Task User_Can_View_MyEvents()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        // 1) GET login to fetch anti-forgery token
        var loginGet = await client.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var afToken = ExtractAntiForgeryToken(loginHtml);
        Assert.False(string.IsNullOrEmpty(afToken));

        // 2) POST login (user)
        var loginForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("Identifier", "0500000000"),
            new KeyValuePair<string,string>("RememberMe", "false")
        });
        var loginPost = await client.PostAsync("/Auth/Login", loginForm);
        Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // 3) Navigate to MyEvents
        var myEvents = await client.GetAsync("/UserPortal/Events");
        Assert.Equal(HttpStatusCode.OK, myEvents.StatusCode);
        var content = await myEvents.Content.ReadAsStringAsync();
        Assert.Contains("أحداثي", content);
    }

    [Fact]
    public async Task Participation_Flow_Submit_Signature_Surveys_Discussion_Then_Admin_Results_And_PDF()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        // Separate clients for admin and user sessions
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        // ADMIN: create an Active event in the default organization
        var adminLoginGet2 = await admin.GetAsync("/Auth/Login");
        var adminLoginHtml2 = await adminLoginGet2.Content.ReadAsStringAsync();
        var adminToken2 = ExtractAntiForgeryToken(adminLoginHtml2);
        var adminLoginPost2 = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", adminToken2),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, adminLoginPost2.StatusCode);
        var evCreateGet = await admin.GetAsync("/Admin/Events/Create");
        var evCreateHtml = await evCreateGet.Content.ReadAsStringAsync();
        var evAf = ExtractAntiForgeryToken(evCreateHtml);
        var orgOptionForSeed = ExtractSelectOptionValueByText(evCreateHtml, "OrganizationId", "الجهة الافتراضية")
                                 ?? ExtractFirstOptionValueFromSelect(evCreateHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOptionForSeed));
        var t0 = DateTime.UtcNow;
        var evCreatePost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", evAf),
            new KeyValuePair<string,string>("Title", "حدث للمشاركة"),
            new KeyValuePair<string,string>("Description", "وصف"),
            new KeyValuePair<string,string>("StartAt", t0.ToString("s")),
            new KeyValuePair<string,string>("EndAt", t0.AddHours(2).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Active"),
            new KeyValuePair<string,string>("OrganizationId", orgOptionForSeed!)
        }));
        Assert.Equal(HttpStatusCode.Redirect, evCreatePost.StatusCode);
        var evDetailsUrl = evCreatePost.Headers.Location?.ToString() ?? string.Empty;
        var createdEventId = ExtractFirstGuid(evDetailsUrl);
        Assert.NotEqual(Guid.Empty, createdEventId);


        // USER: login
        var loginGet = await user.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        Assert.False(string.IsNullOrEmpty(token));
        var loginForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "0500000000"),
            new KeyValuePair<string,string>("RememberMe", "false")
        });
        var loginPost = await user.PostAsync("/Auth/Login", loginForm);
        Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Use the event we just created
        var eventId = createdEventId;

        // USER: open participation details to get token + dynamic IDs
        var details = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        var detailsHtml = await details.Content.ReadAsStringAsync();
        var formToken = ExtractAntiForgeryToken(detailsHtml);
        Assert.False(string.IsNullOrEmpty(formToken));

        // Extract first single question and option
        var qOpt = ExtractFirstSingleQuestionAndOption(detailsHtml);
        // Extract first discussion id (if exists)
        var discussionId = ExtractFirstDiscussionId(detailsHtml);

        var fields = new List<KeyValuePair<string,string>>
        {
            new("__RequestVerificationToken", formToken),
            new("EventId", eventId.ToString())
        };
        if (qOpt is { } qo)
        {
            fields.Add(new($"SurveyAnswers[{qo.qId}]", qo.optId.ToString()));
        }
        if (discussionId != null)
        {
            fields.Add(new($"DiscussionReplies[{discussionId}]", "رأي تجريبي"));
        }
        // Add a minimal signature if required; server only checks non-empty when required
        fields.Add(new("SignatureData", "data:image/png;base64,AA=="));

        var submitForm = new FormUrlEncodedContent(fields);
        var submit = await user.PostAsync("/UserPortal/EventParticipation/Submit", submitForm);
        Assert.True(submit.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
        var finalUrl = submit.Headers.Location?.ToString() ?? string.Empty;
        // Follow to confirmation
        var confirm = await user.GetAsync(finalUrl.Length > 0 ? finalUrl : $"/UserPortal/EventParticipation/Confirmation?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var confirmHtml = await confirm.Content.ReadAsStringAsync();
        Assert.Contains("تم إرسال ردك بنجاح", confirmHtml);

        // ADMIN: already logged in earlier with this client; proceed to results

        // ADMIN: Summary
        var summary = await admin.GetAsync($"/Admin/EventResults/Summary?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var summaryHtml = await summary.Content.ReadAsStringAsync();
        Assert.Contains("ملخص", summaryHtml, StringComparison.OrdinalIgnoreCase);

        // ADMIN: PDF exports
        var pdf1 = await admin.GetAsync($"/Admin/EventResults/ExportSummaryPdf?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, pdf1.StatusCode);
        Assert.Equal("application/pdf", pdf1.Content.Headers.ContentType?.MediaType);
        var pdf1Bytes = await pdf1.Content.ReadAsByteArrayAsync();
        Assert.True(pdf1Bytes.Length > 200);

        var pdf2 = await admin.GetAsync($"/Admin/EventResults/ExportDetailedPdf?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, pdf2.StatusCode);
        Assert.Equal("application/pdf", pdf2.Content.Headers.ContentType?.MediaType);
        var pdf2Bytes = await pdf2.Content.ReadAsByteArrayAsync();
        Assert.True(pdf2Bytes.Length > 200);
    }
    [Fact]
    public async Task Public_Link_Sharing_EndToEnd_Works_And_Guest_Shown_In_Results()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        // Admin login
        var loginGet = await admin.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var afLogin = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afLogin),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // Create an Active event
        var createGet = await admin.GetAsync("/Admin/Events/Create");
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var afCreate = ExtractAntiForgeryToken(createHtml);
        var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOption));
        var now = DateTime.UtcNow;
        var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "حدث عام للاختبار"),
            new KeyValuePair<string,string>("Description", "وصف"),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Active"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!)
        }));
        Assert.Equal(HttpStatusCode.Redirect, createPost.StatusCode);
        var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
        var eventId = ExtractFirstGuid(detailsUrl);
        Assert.NotEqual(Guid.Empty, eventId);

        // Open Edit to get anti-forgery token for AJAX-like POST
        var editGet = await admin.GetAsync($"/Admin/Events/Edit/{eventId}");
        var editHtml = await editGet.Content.ReadAsStringAsync();
        var afEdit = ExtractAntiForgeryToken(editHtml);
        Assert.False(string.IsNullOrEmpty(afEdit));

        // Generate public link via controller
        var genResp = await admin.PostAsync("/Admin/PublicLinks/Generate", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
            new KeyValuePair<string,string>("eventId", eventId.ToString())
        }));
        Assert.Equal(HttpStatusCode.OK, genResp.StatusCode);
        var genJson = await genResp.Content.ReadAsStringAsync();
        Assert.Contains("success", genJson);
        var urlMatch = Regex.Match(genJson, "\\\"url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
        Assert.True(urlMatch.Success, "public url not found in JSON");
        var publicUrl = urlMatch.Groups[1].Value;

        // Guest opens the public URL, fills name, and continues
        var guestGet = await guest.GetAsync(publicUrl);
        Assert.Equal(HttpStatusCode.OK, guestGet.StatusCode);
        var guestHtml = await guestGet.Content.ReadAsStringAsync();
        var afGuest = ExtractAntiForgeryToken(guestHtml);
        Assert.False(string.IsNullOrEmpty(afGuest));

        var enterName = await guest.PostAsync(publicUrl + "/EnterName", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afGuest),
            new KeyValuePair<string,string>("fullName", "ضيف تجريبي"),
            new KeyValuePair<string,string>("email", "guest@example.com"),
            new KeyValuePair<string,string>("phone", "05011112222")
        }));
        Assert.True(enterName.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);

        // Now the details page should render for the guest
        var details = await guest.GetAsync(publicUrl);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        var detailsHtml = await details.Content.ReadAsStringAsync();
        var afPart = ExtractAntiForgeryToken(detailsHtml);
        Assert.False(string.IsNullOrEmpty(afPart));

        // Submit minimal participation
        var fields = new List<KeyValuePair<string,string>>
        {
            new("__RequestVerificationToken", afPart),
            new("EventId", eventId.ToString()),
            new("SignatureData", "")
        };
        var submit = await guest.PostAsync(publicUrl + "/Submit", new FormUrlEncodedContent(fields));
        Assert.True(submit.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Admin: Summary should show a participant, potentially flagged as guest
        var summary = await admin.GetAsync($"/Admin/EventResults/Summary?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var summaryHtml = await summary.Content.ReadAsStringAsync();
        // Look for guest badge text and details section
        Assert.Contains("ضيف", summaryHtml);
        Assert.Contains("تفاصيل المشاركات", summaryHtml);
    }


        private static Guid ExtractFirstGuid(string text)
        {
            var m = Regex.Match(text, "[0-9a-fA-F-]{36}");
            return m.Success ? Guid.Parse(m.Value) : Guid.Empty;
        }

        [Fact]
        public async Task Admin_EndToEnd_Management_Flows_Work()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Login (admin) and fetch anti-forgery token from Create page for subsequent POSTs
            var loginGet = await client.GetAsync("/Auth/Login");
            var loginHtml = await loginGet.Content.ReadAsStringAsync();
            var afTokenLogin = ExtractAntiForgeryToken(loginHtml);
            var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenLogin),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

            // Dashboard loads
            var dash = await client.GetAsync("/Admin/Dashboard");
            Assert.Equal(HttpStatusCode.OK, dash.StatusCode);

            // Events Index loads
            var eventsIndex = await client.GetAsync("/Admin/Events");
            Assert.Equal(HttpStatusCode.OK, eventsIndex.StatusCode);

            // Create an event (to act upon)
            var createGet = await client.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var orgOption = ExtractSelectOptionValueByText(createHtml, "OrganizationId", " ") ?? ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            Assert.False(string.IsNullOrEmpty(orgOption));
            var now = DateTime.UtcNow;
            var createPost = await client.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "اختبار إدارة"),
                new KeyValuePair<string,string>("Description", "وصف إداري"),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "false"),
                new KeyValuePair<string,string>("Status", "Draft"),
                new KeyValuePair<string,string>("OrganizationId", orgOption!)
            }));
            Assert.Equal(HttpStatusCode.Redirect, createPost.StatusCode);
            var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
            Assert.Contains("/Admin/Events/Details/", detailsUrl);
            var eventId = ExtractFirstGuid(detailsUrl);
            Assert.NotEqual(Guid.Empty, eventId);

            // Edit page renders
            var editGet = await client.GetAsync($"/Admin/Events/Edit/{eventId}");
            Assert.Equal(HttpStatusCode.OK, editGet.StatusCode);

            // Prepare a general anti-forgery token for JSON POSTs
            var afTokenSource = ExtractAntiForgeryToken(createHtml);
            Assert.False(string.IsNullOrEmpty(afTokenSource));

            // EventSections: AddSection
            var addSection = await client.PostAsync("/Admin/EventSections/AddSection", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenSource),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "بند تجريبي"),
                new KeyValuePair<string,string>("Body", "نص تجريبي")
            }));
            Assert.Equal(HttpStatusCode.OK, addSection.StatusCode);
            var addSectionJson = await addSection.Content.ReadAsStringAsync();
            Assert.Contains("success", addSectionJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", addSectionJson, StringComparison.OrdinalIgnoreCase);
            var sectionId = ExtractFirstGuid(addSectionJson);
            Assert.NotEqual(Guid.Empty, sectionId);

            // EventSections: AddDecision
            var addDecision = await client.PostAsync("/Admin/EventSections/AddDecision", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenSource),
                new KeyValuePair<string,string>("SectionId", sectionId.ToString()),
                new KeyValuePair<string,string>("Title", "قرار تجريبي")
            }));
            Assert.Equal(HttpStatusCode.OK, addDecision.StatusCode);
            var addDecisionJson = await addDecision.Content.ReadAsStringAsync();
            Assert.Contains("success", addDecisionJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", addDecisionJson, StringComparison.OrdinalIgnoreCase);

            // EventComponents: AddSurvey
            var addSurvey = await client.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenSource),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "استبيان تجريبي")
            }));
            Assert.Equal(HttpStatusCode.OK, addSurvey.StatusCode);
            var addSurveyJson = await addSurvey.Content.ReadAsStringAsync();
            Assert.Contains("success", addSurveyJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", addSurveyJson, StringComparison.OrdinalIgnoreCase);

            // EventComponents: AddDiscussion
            var addDiscussion = await client.PostAsync("/Admin/EventComponents/AddDiscussion", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenSource),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "نقاش تجريبي"),
                new KeyValuePair<string,string>("Purpose", "غرض")
            }));
            Assert.Equal(HttpStatusCode.OK, addDiscussion.StatusCode);
            var addDiscussionJson = await addDiscussion.Content.ReadAsStringAsync();
            Assert.Contains("success", addDiscussionJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", addDiscussionJson, StringComparison.OrdinalIgnoreCase);

            // EventComponents: AddTable
            var addTable = await client.PostAsync("/Admin/EventComponents/AddTable", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afTokenSource),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "جدول تجريبي")
            }));
            Assert.Equal(HttpStatusCode.OK, addTable.StatusCode);
            var addTableJson = await addTable.Content.ReadAsStringAsync();
            Assert.Contains("success", addTableJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", addTableJson, StringComparison.OrdinalIgnoreCase);

            // EventComponents: UploadAttachment (small PNG bytes)
            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG signature
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(afTokenSource), "__RequestVerificationToken");
            form.Add(new StringContent(eventId.ToString()), "EventId");
            form.Add(new StringContent("مرفق تجريبي"), "Title");
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "File", "test.png");
            var upload = await client.PostAsync("/Admin/EventComponents/UploadAttachment", form);
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var uploadJson = await upload.Content.ReadAsStringAsync();
            Assert.Contains("success", uploadJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", uploadJson, StringComparison.OrdinalIgnoreCase);

            // EventResults: Details page renders
            var details = await client.GetAsync($"/Admin/EventResults/Details?eventId={eventId}");
            Assert.Equal(HttpStatusCode.OK, details.StatusCode);

        }

    [Fact]
    public async Task Admin_Events_Index_Results_Link_Opens_Summary()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login as admin
        var loginGet = await admin.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var af = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", af),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        var index = await admin.GetAsync("/Admin/Events");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        var indexHtml = await index.Content.ReadAsStringAsync();
        // Look for /Admin/EventResults/Summary?eventId=GUID
        var m = Regex.Match(indexHtml, @"/Admin/EventResults/Summary\?eventId=([0-9a-fA-F-]{36})");
        Assert.True(m.Success, "Results link not found in Events index");
        var url = m.Value;
        var res = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("نتائج الحدث", body);
    }

    [Fact]
    public async Task Results_Summary_Shows_Aggregates_From_Seed_Event()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login as admin
        var loginGet = await admin.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var af = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", af),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // Use the first results link on the Events page (seed creates one Active event with data)
        var index = await admin.GetAsync("/Admin/Events");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        var indexHtml = await index.Content.ReadAsStringAsync();
        var m = Regex.Match(indexHtml, @"/Admin/EventResults/Summary\?eventId=([0-9a-fA-F-]{36})");
        Assert.True(m.Success);
        var url = m.Value;

        var summary = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var html = await summary.Content.ReadAsStringAsync();

        // Summary labels should be present
        Assert.Contains("نتائج الحدث", html);
        Assert.Contains("نتائج الاستبيانات", html);
        Assert.Contains("تفاصيل المشاركات", html);

        // PDF exports should work for this event
        var evId = Regex.Match(url, @"([0-9a-fA-F-]{36})$").Groups[1].Value;
        var pdf1 = await admin.GetAsync($"/Admin/EventResults/ExportSummaryPdf?eventId={evId}");
        Assert.Equal(HttpStatusCode.OK, pdf1.StatusCode);
        Assert.Equal("application/pdf", pdf1.Content.Headers.ContentType?.MediaType);
        var pdf1Bytes = await pdf1.Content.ReadAsByteArrayAsync();
        Assert.True(pdf1Bytes.Length > 50);

        var pdf2 = await admin.GetAsync($"/Admin/EventResults/ExportDetailedPdf?eventId={evId}");
        Assert.Equal(HttpStatusCode.OK, pdf2.StatusCode);
        Assert.Equal("application/pdf", pdf2.Content.Headers.ContentType?.MediaType);
        var pdf2Bytes = await pdf2.Content.ReadAsByteArrayAsync();
        Assert.True(pdf2Bytes.Length > 50);
    }

    [Fact]
    public async Task Admin_Can_Export_Single_User_Results_Pdf()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Admin login
        var loginGet = await client.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        });
        var loginResp = await client.PostAsync("/Auth/Login", loginForm);
        Assert.True(loginResp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Go to Events index and extract first Results link
        var indexResp = await client.GetAsync("/Admin/Events");
        var indexHtml = await indexResp.Content.ReadAsStringAsync();
        var resLink = Regex.Match(indexHtml, @"/Admin/EventResults/Summary\?eventId=([0-9a-fA-F-]{36})");
        Assert.True(resLink.Success, "Results link not found");
        var eventId = resLink.Groups[1].Value;

        // Open Summary to load user list
        var summaryResp = await client.GetAsync($"/Admin/EventResults/Summary?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);
        var summaryHtml = await summaryResp.Content.ReadAsStringAsync();

        // Extract a userId from the dropdown we render
        var userMatch = Regex.Match(summaryHtml, "id=\"user-export-select\"[\\s\\S]*?<option value=\"([0-9a-fA-F-]{36})\">");
        if (!userMatch.Success)
        {
            // If no user responses seeded, skip the test gracefully
            return;
        }
        var userId = userMatch.Groups[1].Value;

        var pdfResp = await client.GetAsync($"/Admin/EventResults/ExportUserPdf?eventId={eventId}&userId={userId}");
        Assert.Equal(HttpStatusCode.OK, pdfResp.StatusCode);
        Assert.Equal("application/pdf", pdfResp.Content.Headers.ContentType?.MediaType);
        var bytes = await pdfResp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
    }


        [Fact]
        public async Task Security_Redirects_Without_Login()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var anon = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var resp1 = await anon.GetAsync("/Admin/Events");
            Assert.Equal(HttpStatusCode.Redirect, resp1.StatusCode);
            Assert.Contains("/Auth/Login", resp1.Headers.Location?.ToString() ?? string.Empty);

            var resp2 = await anon.GetAsync("/UserPortal/Events");
            Assert.Equal(HttpStatusCode.Redirect, resp2.StatusCode);
            Assert.Contains("/Auth/Login", resp2.Headers.Location?.ToString() ?? string.Empty);
        }

    [Fact]
    public async Task Admin_Logout_Redirects_To_Login()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login as admin
        var loginGet = await client.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // Fetch a page with the logout anti-forgery token in layout
        var dash = await client.GetAsync("/Admin/Dashboard");
        Assert.Equal(HttpStatusCode.OK, dash.StatusCode);
        var dashHtml = await dash.Content.ReadAsStringAsync();
        var afLogout = ExtractAntiForgeryToken(dashHtml);
        Assert.False(string.IsNullOrEmpty(afLogout));

        // Logout
        var logoutPost = await client.PostAsync("/Auth/Logout", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afLogout)
        }));
        Assert.Equal(HttpStatusCode.Redirect, logoutPost.StatusCode);
        Assert.Contains("/Auth/Login", logoutPost.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task User_Logout_Redirects_To_Login()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login as user
        var loginGet = await client.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "0500000000"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Fetch a page with the logout anti-forgery token in layout
        var myEvents = await client.GetAsync("/UserPortal/Events");
        Assert.Equal(HttpStatusCode.OK, myEvents.StatusCode);
        var myEventsHtml = await myEvents.Content.ReadAsStringAsync();
        var afLogout = ExtractAntiForgeryToken(myEventsHtml);
        Assert.False(string.IsNullOrEmpty(afLogout));
        // Logout
        var logoutPost = await client.PostAsync("/Auth/Logout", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afLogout)
        }));
        Assert.Equal(HttpStatusCode.Redirect, logoutPost.StatusCode);
        Assert.Contains("/Auth/Login", logoutPost.Headers.Location?.ToString() ?? string.Empty);
    }

    private static string? ExtractSelectOptionValueByText(string html, string selectName, string optionText)
    {
        // Rough regex to find option with text under a select for the given name
        var selectPattern = "<select[^>]*name=\\\"" + Regex.Escape(selectName) + "\\\"[\\s\\S]*?</select>";
        var mSelect = Regex.Match(html, selectPattern, RegexOptions.IgnoreCase);
        if (!mSelect.Success) return null;
        var block = mSelect.Value;
        var optionPattern = "<option[^>]*value=\\\"([0-9a-fA-F-]{36})\\\"[^>]*>\\s*" + Regex.Escape(optionText) + "\\s*</option>";
        var mOpt = Regex.Match(block, optionPattern, RegexOptions.IgnoreCase);
        return mOpt.Success ? mOpt.Groups[1].Value : null;
    }

    private static string? ExtractFirstOptionValueFromSelect(string html, string selectName)
    {
        var selectPattern = "<select[^>]*name=\\\"" + Regex.Escape(selectName) + "\\\"[\\s\\S]*?</select>";
        var mSelect = Regex.Match(html, selectPattern, RegexOptions.IgnoreCase);
        if (!mSelect.Success) return null;
        var block = mSelect.Value;
        var mOpt = Regex.Match(block, "<option[^>]*value=\\\"([0-9a-fA-F-]{36})\\\"", RegexOptions.IgnoreCase);
        return mOpt.Success ? mOpt.Groups[1].Value : null;
    }

        private static string? ExtractNthOptionValueFromSelect(string html, string selectName, int n)
        {
            var selectPattern = "<select[^>]*name=\\\"" + Regex.Escape(selectName) + "\\\"[\\s\\S]*?</select>";
            var mSelect = Regex.Match(html, selectPattern, RegexOptions.IgnoreCase);
            if (!mSelect.Success) return null;
            var block = mSelect.Value;
            var matches = Regex.Matches(block, "<option[^>]*value=\\\"([0-9a-fA-F-]{36})\\\"", RegexOptions.IgnoreCase);
            return (n >= 1 && n <= matches.Count) ? matches[n - 1].Groups[1].Value : null;
        }

        private static bool AreInOrder(string html, params string[] tokens)
        {
            var last = -1;
            foreach (var t in tokens)
            {
                var idx = html.IndexOf(t, StringComparison.Ordinal);
                if (idx < 0 || idx < last) return false;
                last = idx;
            }
            return true;
        }



    private static string? ExtractOrganizationIdByNameFromIndex(string html, string orgName)
    {
        var pattern = "<tr[\\s\\S]*?<td>\\s*" + Regex.Escape(orgName) + "\\s*</td>[\\s\\S]*?/Admin/Organizations/Edit/([0-9a-fA-F-]{36})[\\s\\S]*?</tr>";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }


    private static string? ExtractLastOrganizationIdFromIndex(string html)
    {
        var matches = Regex.Matches(html, "/Admin/Organizations/Edit/([0-9a-fA-F-]{36})");
        return matches.Count > 0 ? matches[matches.Count - 1].Groups[1].Value : null;
    }

    [Fact]
    public async Task Admin_Can_Create_Event_With_Organization_Selection()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // Login as admin
        var loginGet = await client.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // Open Create Event and pick an organization option
        var createGet = await client.GetAsync("/Admin/Events/Create");
        Assert.Equal(HttpStatusCode.OK, createGet.StatusCode);
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var afCreate = ExtractAntiForgeryToken(createHtml);
        Assert.False(string.IsNullOrEmpty(afCreate));
        var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOption));

        // Create event with selected organization
        var now = DateTime.UtcNow;
        var resp = await client.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "حدث بمنظمة محددة"),
            new KeyValuePair<string,string>("Description", "وصف"),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(2).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Draft"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!)
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/Admin/Events/Details/", resp.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Admin_Event_Details_Shows_All_Components()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login admin
        var loginGet = await client.GetAsync("/Auth/Login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);

        // Create event (pick first org)
        var createGet = await client.GetAsync("/Admin/Events/Create");
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var afCreate = ExtractAntiForgeryToken(createHtml);
        var orgOption = ExtractSelectOptionValueByText(createHtml, "OrganizationId", " ") ?? ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        var now = DateTime.UtcNow;
        var createPost = await client.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "تفاصيل تظهر مكونات"),
            new KeyValuePair<string,string>("Description", "عرض"),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Draft"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!)
        }));
        var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
        var eventId = ExtractFirstGuid(detailsUrl);
        Assert.NotEqual(Guid.Empty, eventId);

        // Use component endpoints to add items
        var afToken = ExtractAntiForgeryToken(createHtml);
        var addSection = await client.PostAsync("/Admin/EventSections/AddSection", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("EventId", eventId.ToString()),
            new KeyValuePair<string,string>("Title", "بند تجريبي"),
            new KeyValuePair<string,string>("Body", "نص")
        }));
        Assert.Equal(HttpStatusCode.OK, addSection.StatusCode);

        var addSurvey = await client.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("EventId", eventId.ToString()),
            new KeyValuePair<string,string>("Title", "استبيان تجريبي")
        }));
        Assert.Equal(HttpStatusCode.OK, addSurvey.StatusCode);

        var addDiscussion = await client.PostAsync("/Admin/EventComponents/AddDiscussion", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("EventId", eventId.ToString()),
            new KeyValuePair<string,string>("Title", "نقاش تجريبي"),
            new KeyValuePair<string,string>("Purpose", "غرض")
        }));
        Assert.Equal(HttpStatusCode.OK, addDiscussion.StatusCode);

        var addTable = await client.PostAsync("/Admin/EventComponents/AddTable", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afToken),
            new KeyValuePair<string,string>("EventId", eventId.ToString()),
            new KeyValuePair<string,string>("Title", "جدول تجريبي")
        }));
        Assert.Equal(HttpStatusCode.OK, addTable.StatusCode);

        // Now, details page should display these titles
        var details = await client.GetAsync(detailsUrl);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        var html = await details.Content.ReadAsStringAsync();
        Assert.Contains("البنود والقرارات", html);
        Assert.Contains("الاستبيانات", html);
        Assert.Contains("النقاشات", html);
        Assert.Contains("الجداول", html);
    }

    [Fact]
    public async Task User_Cannot_Access_Other_Organization_Event()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Admin login
        var lgGet = await admin.GetAsync("/Auth/Login");
        var lgHtml = await lgGet.Content.ReadAsStringAsync();
        var lgToken = ExtractAntiForgeryToken(lgHtml);
        var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", lgToken),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, lgPost.StatusCode);

        // Create a new Organization
        var orgCreateGet = await admin.GetAsync("/Admin/Organizations/Create");
        var orgCreateHtml = await orgCreateGet.Content.ReadAsStringAsync();
        var orgToken = ExtractAntiForgeryToken(orgCreateHtml);
        var orgName = "Org For Test " + Guid.NewGuid().ToString("N");
        var orgPost = await admin.PostAsync("/Admin/Organizations/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", orgToken),
            new KeyValuePair<string,string>("Name", orgName),
            new KeyValuePair<string,string>("NameEn", "Org For Test"),
            new KeyValuePair<string,string>("Type", "1"),
            new KeyValuePair<string,string>("IsActive", "true")
        }));
        Assert.True(orgPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Find the newly created org Id from the Events/Create dropdown by name
        var evCreateGet2 = await admin.GetAsync("/Admin/Events/Create");
        var evCreateHtml2 = await evCreateGet2.Content.ReadAsStringAsync();
        var newOrgId = ExtractSelectOptionValueByText(evCreateHtml2, "OrganizationId", orgName);
        if (string.IsNullOrEmpty(newOrgId))
        {
            // Fallback: scrape Organizations index if dropdown not updated yet
            var orgIndex = await admin.GetAsync("/Admin/Organizations");
            var orgIndexHtml = await orgIndex.Content.ReadAsStringAsync();
            newOrgId = ExtractOrganizationIdByNameFromIndex(orgIndexHtml, orgName)
                       ?? ExtractLastOrganizationIdFromIndex(orgIndexHtml);
        }
        Assert.False(string.IsNullOrEmpty(newOrgId));
        var afCreate = ExtractAntiForgeryToken(evCreateHtml2);
        var now = DateTime.UtcNow;

        var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "حدث لمنظمة أخرى"),
            new KeyValuePair<string,string>("Description", "وصف"),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Active"),
            new KeyValuePair<string,string>("OrganizationId", newOrgId!)
        }));
        Assert.True(createPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
        var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
        var eventId = ExtractFirstGuid(detailsUrl);

        // Create a second Organization and a user in it
        var org2CreateGet = await admin.GetAsync("/Admin/Organizations/Create");
        var org2CreateHtml = await org2CreateGet.Content.ReadAsStringAsync();
        var org2Token = ExtractAntiForgeryToken(org2CreateHtml);
        var org2Name = "Org For Isolation " + Guid.NewGuid().ToString("N");
        var org2Post = await admin.PostAsync("/Admin/Organizations/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", org2Token),
            new KeyValuePair<string,string>("Name", org2Name),
            new KeyValuePair<string,string>("NameEn", org2Name),
            new KeyValuePair<string,string>("Type", "1"),
            new KeyValuePair<string,string>("IsActive", "true")
        }));
        Assert.True(org2Post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Open Users/Create to get token and Org2 Id from the dropdown (Form.OrganizationId)
        var uCreateGet = await admin.GetAsync("/Admin/Users/Create");
        var uCreateHtml = await uCreateGet.Content.ReadAsStringAsync();
        var uCreateToken = ExtractAntiForgeryToken(uCreateHtml);
        var org2Id = ExtractSelectOptionValueByText(uCreateHtml, "Form.OrganizationId", org2Name);
        if (string.IsNullOrEmpty(org2Id))
        {
            var orgIndex2 = await admin.GetAsync("/Admin/Organizations");
            var orgIndex2Html = await orgIndex2.Content.ReadAsStringAsync();
            org2Id = ExtractOrganizationIdByNameFromIndex(orgIndex2Html, org2Name)
                     ?? ExtractLastOrganizationIdFromIndex(orgIndex2Html);
        }
        Assert.False(string.IsNullOrEmpty(org2Id));

        // Create a new user in Org2
        var phone = "05" + Random.Shared.Next(10000000, 99999999).ToString();
        var email = $"user_{Guid.NewGuid():N}@mina.local";
        var uPost = await admin.PostAsync("/Admin/Users/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", uCreateToken),
            new KeyValuePair<string,string>("Form.FullName", "مستخدم منظمة ثانية"),
            new KeyValuePair<string,string>("Form.Email", email),
            new KeyValuePair<string,string>("Form.Phone", phone),
            new KeyValuePair<string,string>("Form.OrganizationId", org2Id!),
            new KeyValuePair<string,string>("Form.RoleName", "Attendee"),
            new KeyValuePair<string,string>("Form.IsActive", "true")
        }));
        Assert.True(uPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Login as the user from Org2
        var ulGet = await user.GetAsync("/Auth/Login");
        var ulHtml = await ulGet.Content.ReadAsStringAsync();
        var utoken = ExtractAntiForgeryToken(ulHtml);
        var ulPost = await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", utoken),
            new KeyValuePair<string,string>("Identifier", phone),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.True(ulPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Try to access participation details of the other-org event -> should be blocked
        var part = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
        if (part.StatusCode == HttpStatusCode.OK)
        {
            // If OK is returned, ensure it is not the event details (should not contain the event title)
            var body = await part.Content.ReadAsStringAsync();
            Assert.DoesNotContain("حدث لمنظمة أخرى", body);
        }
        else
        {
            Assert.True(part.StatusCode == HttpStatusCode.Forbidden || part.StatusCode == HttpStatusCode.Redirect);
        }

        // MyEvents should not list the other-org event title
        var myEvents = await user.GetAsync("/UserPortal/Events");
        Assert.Equal(HttpStatusCode.OK, myEvents.StatusCode);
        var myHtml = await myEvents.Content.ReadAsStringAsync();
        Assert.DoesNotContain("حدث لمنظمة أخرى", myHtml);
    }


    [Fact]
    public async Task TableBlock_Data_Renders_For_Admin_And_User()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        // Admin login
        var lgGet = await admin.GetAsync("/Auth/Login");
        var lgHtml = await lgGet.Content.ReadAsStringAsync();
        var af = ExtractAntiForgeryToken(lgHtml);
        var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", af),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, lgPost.StatusCode);

        // GET Create
        var createGet = await admin.GetAsync("/Admin/Events/Create");
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var afCreate = ExtractAntiForgeryToken(createHtml);
        var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOption));

        // Builder JSON with a simple 2x2 table
        var rowsJson = "{\"rows\":[[{\"value\":\"A\"},{\"value\":\"B\"}],[{\"value\":\"1\"},{\"value\":\"2\"}]]}";
        var builderObj = new {
            sections = Array.Empty<object>(),
            surveys = Array.Empty<object>(),
            discussions = Array.Empty<object>(),
            tables = new[] { new { title = "Data Table", rowsJson = rowsJson } },
            images = Array.Empty<object>(),
            pdfs = Array.Empty<object>()
        };
        var builderJson = JsonSerializer.Serialize(builderObj);

        var now = DateTime.UtcNow;
        var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "Event With Table"),
            new KeyValuePair<string,string>("Description", "Desc"),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Active"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!),
            new KeyValuePair<string,string>("BuilderJson", builderJson)
        }));
        Assert.Equal(HttpStatusCode.Redirect, createPost.StatusCode);
        var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
        var eventId = ExtractFirstGuid(detailsUrl);
        Assert.NotEqual(Guid.Empty, eventId);

        // Admin Details table values
        var details = await admin.GetAsync(detailsUrl);
        var detailsBody = await details.Content.ReadAsStringAsync();
        Assert.Contains("A", detailsBody);
        Assert.Contains("B", detailsBody);
        Assert.Contains("1", detailsBody);
        Assert.Contains("2", detailsBody);

        // User login
        var uGet = await user.GetAsync("/Auth/Login");
        var uHtml = await uGet.Content.ReadAsStringAsync();
        var uaf = ExtractAntiForgeryToken(uHtml);
        var uPost = await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
            new KeyValuePair<string,string>("Identifier", "0500000000"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.True(uPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // MyEvents should list the event
        var my = await user.GetAsync("/UserPortal/Events");
        var myBody = await my.Content.ReadAsStringAsync();
        Assert.Contains("Event With Table", myBody);

        // Participation details should show values
        var part = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
        var partBody = await part.Content.ReadAsStringAsync();
        Assert.Contains("A", partBody);
        Assert.Contains("B", partBody);
        Assert.Contains("1", partBody);
        Assert.Contains("2", partBody);
    }


#if false

    [Fact(Skip="Temporarily skipped: encoding issue in literal strings; replaced by ASCII test later")]
    public async Task TableBlock_Data_Persists_And_Renders_Admin_And_User()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        // Admin login
        var lgGet = await admin.GetAsync("/Auth/Login");
        var lgHtml = await lgGet.Content.ReadAsStringAsync();
        var af = ExtractAntiForgeryToken(lgHtml);
        var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", af),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.Equal(HttpStatusCode.Redirect, lgPost.StatusCode);

        // Open Create page to get token and organization option
        var createGet = await admin.GetAsync("/Admin/Events/Create");
        var createHtml = await createGet.Content.ReadAsStringAsync();
        var afCreate = ExtractAntiForgeryToken(createHtml);
        var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
        Assert.False(string.IsNullOrEmpty(orgOption));

        // Prepare builder json with a table having data
        var rowsJson = "{\"rows\":[[{\"value\":\"A\"},{\"value\":\"B\"}],[{\"value\":\"1\"},{\"value\":\"2\"}]]}";
        var builderJson = $"{{\"sections\":[],\"surveys\":[],\"discussions\":[],\"tables\":[{{\"title\":\"            \",\"rowsJson\":\"{rowsJson.Replace(\"\"\",\\\"\")}\"}}],\"images\":[],\"pdfs\":[]}}";

        var now = DateTime.UtcNow;
        var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
            new KeyValuePair<string,string>("Title", "                 "),
            new KeyValuePair<string,string>("Description", "   "),
            new KeyValuePair<string,string>("StartAt", now.ToString("s")),
            new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
            new KeyValuePair<string,string>("RequireSignature", "false"),
            new KeyValuePair<string,string>("Status", "Active"),
            new KeyValuePair<string,string>("OrganizationId", orgOption!),
            new KeyValuePair<string,string>("BuilderJson", builderJson)
        }));
        Assert.Equal(HttpStatusCode.Redirect, createPost.StatusCode);
        var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
        var eventId = ExtractFirstGuid(detailsUrl);
        Assert.NotEqual(Guid.Empty, eventId);

        // Admin Details should render the table and its cell values
        var details = await admin.GetAsync(detailsUrl);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        var detailsBody = await details.Content.ReadAsStringAsync();
        Assert.NotNull(detailsBody);
        Assert.Contains("A", detailsBody);
        Assert.Contains("B", detailsBody);
        Assert.Contains("1", detailsBody);
        Assert.Contains("2", detailsBody);

        // User login
        var uGet = await user.GetAsync("/Auth/Login");
        var uHtml = await uGet.Content.ReadAsStringAsync();
        var uaf = ExtractAntiForgeryToken(uHtml);
        var uPost = await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
            new KeyValuePair<string,string>("Identifier", "0500000000"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.True(uPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // MyEvents should list the event (since it's Active and in default org)
        var my = await user.GetAsync("/UserPortal/Events");
        Assert.Equal(HttpStatusCode.OK, my.StatusCode);
        var myBody = await my.Content.ReadAsStringAsync();
        Assert.Contains("                 ", myBody);

        // User should see table values in participation details
        var part = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
        Assert.Equal(HttpStatusCode.OK, part.StatusCode);
        var partBody = await part.Content.ReadAsStringAsync();
        Assert.Contains("A", partBody);
        Assert.Contains("B", partBody);
        Assert.Contains("1", partBody);
        Assert.Contains("2", partBody);
    }
#endif
    [Fact]
    public async Task Admin_Can_Create_Organization_EndToEnd()
    {
        await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login as admin
        var loginGet = await client.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);
        var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
            new KeyValuePair<string,string>("RememberMe", "false")
        }));
        Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

        // Open create organization
        var orgCreateGet = await client.GetAsync("/Admin/Organizations/Create");
        Assert.Equal(HttpStatusCode.OK, orgCreateGet.StatusCode);
        var createHtml = await orgCreateGet.Content.ReadAsStringAsync();
        var af = ExtractAntiForgeryToken(createHtml);
        Assert.False(string.IsNullOrEmpty(af));

        var nameAr = "جهة آلية" + DateTime.UtcNow.Ticks;
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", af),
            new KeyValuePair<string,string>("Name", nameAr),
            new KeyValuePair<string,string>("NameEn", "Auto Org"),
            new KeyValuePair<string,string>("Type", "2"),
            new KeyValuePair<string,string>("LicenseExpiry", DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd")),
            new KeyValuePair<string,string>("IsActive", "true")
            // Intentionally omit LicenseKey to verify server generates one
        });
        var post = await client.PostAsync("/Admin/Organizations/Create", form);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        // Navigate to organizations index and assert the name exists (Arabic or English)
        var index = await client.GetAsync("/Admin/Organizations");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        var indexHtml = await index.Content.ReadAsStringAsync();
        Assert.True(indexHtml.Contains(nameAr) || indexHtml.Contains("Auto Org"), $"Expected to find organization '{nameAr}' or 'Auto Org' in index page.");
    }

        [Fact]
        public async Task Sections_Without_Components_and_Event_Without_Sections_Render_Correctly()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Login admin
            var lg = await admin.GetAsync("/Auth/Login");
            var lgHtml = await lg.Content.ReadAsStringAsync();
            var af = ExtractAntiForgeryToken(lgHtml);
            var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(lgPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Create Event E1
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            var now = DateTime.UtcNow;
            var post = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "E1 - Sections Only"),
                new KeyValuePair<string,string>("Description", ""),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "false"),
                new KeyValuePair<string,string>("Status", "Draft"),
                new KeyValuePair<string,string>("OrganizationId", orgOption!)
            }));
            Assert.True(post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            var detailsUrl = post.Headers.Location?.ToString() ?? string.Empty;
            var e1 = ExtractFirstGuid(detailsUrl);
            Assert.NotEqual(Guid.Empty, e1);

            // Add two sections without any components
            var afAdd = afCreate;
            var addS1 = await admin.PostAsync("/Admin/EventSections/AddSection", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afAdd),
                new KeyValuePair<string,string>("EventId", e1.ToString()),
                new KeyValuePair<string,string>("Title", "S1"),
                new KeyValuePair<string,string>("Body", "")
            }));
            Assert.Equal(HttpStatusCode.OK, addS1.StatusCode);
            var addS2 = await admin.PostAsync("/Admin/EventSections/AddSection", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afAdd),
                new KeyValuePair<string,string>("EventId", e1.ToString()),
                new KeyValuePair<string,string>("Title", "S2"),
                new KeyValuePair<string,string>("Body", "")
            }));
            Assert.Equal(HttpStatusCode.OK, addS2.StatusCode);

            // Admin details renders and shows S1, S2
            var adminDetails = await admin.GetAsync($"/Admin/Events/Details/{e1}");
            Assert.Equal(HttpStatusCode.OK, adminDetails.StatusCode);
            var adminDetailsHtml = await adminDetails.Content.ReadAsStringAsync();
            Assert.Contains("S1", adminDetailsHtml);
            Assert.Contains("S2", adminDetailsHtml);

            // Login user
            var ulg = await user.GetAsync("/Auth/Login");
            var ulgHtml = await ulg.Content.ReadAsStringAsync();
            var uaf = ExtractAntiForgeryToken(ulgHtml);
            var ulgPost = await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
                new KeyValuePair<string,string>("Identifier", "0500000000"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(ulgPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // User participation page renders, contains S1 & S2, and no survey inputs
            var up = await user.GetAsync($"/UserPortal/EventParticipation/Details/{e1}");
            Assert.Equal(HttpStatusCode.OK, up.StatusCode);
            var upHtml = await up.Content.ReadAsStringAsync();
            Assert.Contains("S1", upHtml);
            Assert.Contains("S2", upHtml);
            Assert.DoesNotContain("SurveyAnswers[", upHtml);

            // Create Event E2 WITHOUT sections, add event-level survey
            var createGet2 = await admin.GetAsync("/Admin/Events/Create");
            var createHtml2 = await createGet2.Content.ReadAsStringAsync();
            var afCreate2 = ExtractAntiForgeryToken(createHtml2);
            var org2 = ExtractFirstOptionValueFromSelect(createHtml2, "OrganizationId");
            var post2 = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate2),
                new KeyValuePair<string,string>("Title", "E2 - No Sections"),
                new KeyValuePair<string,string>("Description", ""),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "false"),
                new KeyValuePair<string,string>("Status", "Draft"),
                new KeyValuePair<string,string>("OrganizationId", org2!)
            }));
            var e2Url = post2.Headers.Location?.ToString() ?? string.Empty;
            var e2 = ExtractFirstGuid(e2Url);
            Assert.NotEqual(Guid.Empty, e2);

            // Add event-level survey
            var addSurvey = await admin.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate2),
                new KeyValuePair<string,string>("EventId", e2.ToString()),
                new KeyValuePair<string,string>("Title", "Global Survey")
            }));
            Assert.Equal(HttpStatusCode.OK, addSurvey.StatusCode);

            var up2 = await user.GetAsync($"/UserPortal/EventParticipation/Details/{e2}");
            Assert.Equal(HttpStatusCode.OK, up2.StatusCode);
            var up2Html = await up2.Content.ReadAsStringAsync();
            // Should render without errors and show the survey title (even if no questions yet)
            Assert.Contains("E2 - No Sections", up2Html);
        }

        [Fact]
        public async Task OneTime_Submission_Is_Enforced_And_Shows_ReadOnly_On_Second_Try()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Admin login and create simple event with a survey
            var lg = await admin.GetAsync("/Auth/Login");
            var af = ExtractAntiForgeryToken(await lg.Content.ReadAsStringAsync());
            await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var org = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            var now = DateTime.UtcNow;
            var post = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "E3 - OneTime"),
                new KeyValuePair<string,string>("Description", ""),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "true"),
                new KeyValuePair<string,string>("Status", "Draft"),
                new KeyValuePair<string,string>("OrganizationId", org!)
            }));
            var eUrl = post.Headers.Location?.ToString() ?? string.Empty;
            var eventId = ExtractFirstGuid(eUrl);
            Assert.NotEqual(Guid.Empty, eventId);

            var addSurvey = await admin.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "OneTime Survey")
            }));
            Assert.Equal(HttpStatusCode.OK, addSurvey.StatusCode);

            // User login
            var ulg = await user.GetAsync("/Auth/Login");
            var uaf = ExtractAntiForgeryToken(await ulg.Content.ReadAsStringAsync());
            await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
                new KeyValuePair<string,string>("Identifier", "0500000000"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));

            // First submission
            var details = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
            var detailsHtml = await details.Content.ReadAsStringAsync();
            var formToken = ExtractAntiForgeryToken(detailsHtml);
            var qOpt = ExtractFirstSingleQuestionAndOption(detailsHtml);
            var fields = new List<KeyValuePair<string,string>>
            {
                new("__RequestVerificationToken", formToken),
                new("EventId", eventId.ToString()),
                // Satisfy one-time participation via signature even if no survey questions render
                new("SignatureData", "data:image/png;base64,AA==")
            };
            if (qOpt is { } qo)
            {
                fields.Add(new($"SurveyAnswers[{qo.qId}]", qo.optId.ToString()));
            }
            var submit = await user.PostAsync("/UserPortal/EventParticipation/Submit", new FormUrlEncodedContent(fields));
            Assert.True(submit.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Second attempt should be blocked: server enforces one-time submission
            var details2 = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
            var html2 = await details2.Content.ReadAsStringAsync();
            var token2 = ExtractAntiForgeryToken(html2);
            var fields2 = new List<KeyValuePair<string,string>>
            {
                new("__RequestVerificationToken", token2),
                new("EventId", eventId.ToString()),
                new("SignatureData", "data:image/png;base64,AA==")
            };
            var submit2 = await user.PostAsync("/UserPortal/EventParticipation/Submit", new FormUrlEncodedContent(fields2));
            Assert.Equal(HttpStatusCode.Redirect, submit2.StatusCode);
            var loc = submit2.Headers.Location?.ToString() ?? string.Empty;
            Assert.Contains("/UserPortal/EventParticipation/Details", loc);
            Assert.Contains(eventId.ToString(), loc);

            // The details page should render in read-only state (inputs disabled)
            var details3 = await user.GetAsync(loc);
            var html3 = await details3.Content.ReadAsStringAsync();
            Assert.Contains("disabled", html3);
        }


        [Fact]
        public async Task Ordering_Sections_And_Components_Respects_Order()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Admin login
            var lg = await admin.GetAsync("/Auth/Login");
            var af = ExtractAntiForgeryToken(await lg.Content.ReadAsStringAsync());
            await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));

            // Create event
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var org = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            var now = DateTime.UtcNow;
            var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "E4 - Ordering"),
                new KeyValuePair<string,string>("Description", ""),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "false"),
                new KeyValuePair<string,string>("Status", "Draft"),
                new KeyValuePair<string,string>("OrganizationId", org!)
            }));
            var eventId = ExtractFirstGuid(createPost.Headers.Location?.ToString() ?? string.Empty);
            Assert.NotEqual(Guid.Empty, eventId);

            // Add sections S1 then S2
            async Task<Guid> AddSectionAsync(string title)
            {
                var resp = await admin.PostAsync("/Admin/EventSections/AddSection", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                    new KeyValuePair<string,string>("EventId", eventId.ToString()),
                    new KeyValuePair<string,string>("Title", title),
                    new KeyValuePair<string,string>("Body", "")
                }));
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("sectionId").GetGuid();
            }
            var s1 = await AddSectionAsync("S1-Order");
            var s2 = await AddSectionAsync("S2-Order");

            // Add two surveys into S1: First then Second
            async Task AddSurveyToSection(Guid sectionId, string title)
            {
                var resp = await admin.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                    new KeyValuePair<string,string>("EventId", eventId.ToString()),
                    new KeyValuePair<string,string>("SectionId", sectionId.ToString()),
                    new KeyValuePair<string,string>("Title", title)
                }));
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            }
            await AddSurveyToSection(s1, "First Survey");
            await AddSurveyToSection(s1, "Second Survey");

            // User login and open details
            var ulg = await user.GetAsync("/Auth/Login");
            var uaf = ExtractAntiForgeryToken(await ulg.Content.ReadAsStringAsync());
            await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
                new KeyValuePair<string,string>("Identifier", "0500000000"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            var details = await user.GetAsync($"/UserPortal/EventParticipation/Details/{eventId}");
            var html = await details.Content.ReadAsStringAsync();

            // Sections should be in order S1 then S2
            Assert.True(AreInOrder(html, "S1-Order", "S2-Order"));
            // Surveys within S1 should keep creation order
            Assert.True(AreInOrder(html, "First Survey", "Second Survey"));
        }

        [Fact]
        public async Task AccessControl_User_Forbidden_Admin_Sees_Events_Across_Orgs()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var userNoRedirect = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // User login
            var ulg = await userNoRedirect.GetAsync("/Auth/Login");
            var uaf = ExtractAntiForgeryToken(await ulg.Content.ReadAsStringAsync());
            await userNoRedirect.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uaf),
                new KeyValuePair<string,string>("Identifier", "0500000000"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            var forbidden = await userNoRedirect.GetAsync("/Admin/Events");
            Assert.True(forbidden.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);

            // Admin login
            var lg = await admin.GetAsync("/Auth/Login");
            var af = ExtractAntiForgeryToken(await lg.Content.ReadAsStringAsync());
            await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));

            // Create two events under potentially different orgs if available
            async Task CreateEventAsync(string title, int orgIndex)
            {
                var cg = await admin.GetAsync("/Admin/Events/Create");
                var ch = await cg.Content.ReadAsStringAsync();
                var caf = ExtractAntiForgeryToken(ch);
                var orgVal = ExtractNthOptionValueFromSelect(ch, "OrganizationId", orgIndex) ?? ExtractFirstOptionValueFromSelect(ch, "OrganizationId");
                var now = DateTime.UtcNow;
                var resp = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", caf),
                    new KeyValuePair<string,string>("Title", title),
                    new KeyValuePair<string,string>("Description", ""),
                    new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                    new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                    new KeyValuePair<string,string>("RequireSignature", "true"),
                    new KeyValuePair<string,string>("Status", "Draft"),
                    new KeyValuePair<string,string>("OrganizationId", orgVal!)
                }));
                Assert.True(resp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            }
            await CreateEventAsync("E5 - Org1", 1);
            await CreateEventAsync("E6 - Org2", 2);

            var index = await admin.GetAsync("/Admin/Events");
            var html = await index.Content.ReadAsStringAsync();
            Assert.Contains("E5 - Org1", html);
            Assert.Contains("E6 - Org2", html);
        }




        [Fact]
        public async Task User_Can_Hide_Single_Event_And_Only_For_Self()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var user1 = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
            using var user2 = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Admin login
            var lgGet = await admin.GetAsync("/Auth/Login");
            var lgHtml = await lgGet.Content.ReadAsStringAsync();
            var af = ExtractAntiForgeryToken(lgHtml);
            var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.Equal(HttpStatusCode.Redirect, lgPost.StatusCode);

            // Create event
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            var now = DateTime.UtcNow;
            var title = "Evt Hide Single " + Guid.NewGuid().ToString("N");
            var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", title),
                new KeyValuePair<string,string>("Description", "desc"),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "false"),
                new KeyValuePair<string,string>("Status", "Active"),
                new KeyValuePair<string,string>("OrganizationId", orgOption!)
            }));
            Assert.True(createPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Create user1 in the same organization as the event
            var u1CreateGet = await admin.GetAsync("/Admin/Users/Create");
            var u1CreateHtml = await u1CreateGet.Content.ReadAsStringAsync();
            var u1CreateToken = ExtractAntiForgeryToken(u1CreateHtml);
            var phone1 = "05" + Random.Shared.Next(10000000, 99999999).ToString();
            var u1CreatePost = await admin.PostAsync("/Admin/Users/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", u1CreateToken),
                new KeyValuePair<string,string>("Form.FullName", "User1"),
                new KeyValuePair<string,string>("Form.Email", $"u1_{Guid.NewGuid():N}@mina.local"),
                new KeyValuePair<string,string>("Form.Phone", phone1),
                new KeyValuePair<string,string>("Form.OrganizationId", orgOption!),
                new KeyValuePair<string,string>("Form.RoleName", "Attendee"),
                new KeyValuePair<string,string>("Form.IsActive", "true")
            }));
            Assert.True(u1CreatePost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Login as user1
            var u1Get = await user1.GetAsync("/Auth/Login");
            var u1Html = await u1Get.Content.ReadAsStringAsync();
            var u1Af = ExtractAntiForgeryToken(u1Html);
            var u1Post = await user1.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", u1Af),
                new KeyValuePair<string,string>("Identifier", phone1),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(u1Post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // MyEvents shows the event
            var my1 = await user1.GetAsync("/UserPortal/Events");
            var my1Html = await my1.Content.ReadAsStringAsync();
            Assert.Contains(title, my1Html);
            var hideToken = ExtractAntiForgeryToken(my1Html);

            // Hide single
            var hideResp = await user1.PostAsync("/UserPortal/Events/Hide", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", hideToken),
                new KeyValuePair<string,string>("eventId", ExtractFirstGuid(createPost.Headers.Location?.ToString() ?? string.Empty).ToString())
            }));
            Assert.True(hideResp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            var my1After = await user1.GetAsync("/UserPortal/Events");
            var my1AfterHtml = await my1After.Content.ReadAsStringAsync();
            Assert.DoesNotContain(title, my1AfterHtml);

            // Admin still sees it in Admin/Events
            var adminIndex = await admin.GetAsync("/Admin/Events");
            var adminHtml = await adminIndex.Content.ReadAsStringAsync();
            Assert.Contains(title, adminHtml);

            // Create another user in same org and ensure they still see the event
            var uCreateGet = await admin.GetAsync("/Admin/Users/Create");
            var uCreateHtml = await uCreateGet.Content.ReadAsStringAsync();
            var uCreateToken = ExtractAntiForgeryToken(uCreateHtml);
            var phone = "05" + Random.Shared.Next(10000000, 99999999).ToString();
            var uCreatePost = await admin.PostAsync("/Admin/Users/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uCreateToken),
                new KeyValuePair<string,string>("Form.FullName", "User2"),
                new KeyValuePair<string,string>("Form.Email", $"u2_{Guid.NewGuid():N}@mina.local"),
                new KeyValuePair<string,string>("Form.Phone", phone),
                new KeyValuePair<string,string>("Form.OrganizationId", orgOption!),
                new KeyValuePair<string,string>("Form.RoleName", "Attendee"),
                new KeyValuePair<string,string>("Form.IsActive", "true")
            }));
            Assert.True(uCreatePost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Login as user2
            var u2Get = await user2.GetAsync("/Auth/Login");
            var u2Html = await u2Get.Content.ReadAsStringAsync();
            var u2Af = ExtractAntiForgeryToken(u2Html);
            var u2Post = await user2.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", u2Af),
                new KeyValuePair<string,string>("Identifier", phone),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(u2Post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            var my2 = await user2.GetAsync("/UserPortal/Events");
            var my2Html = await my2.Content.ReadAsStringAsync();
            Assert.Contains(title, my2Html);
        }

        [Fact]
        public async Task User_Can_Hide_All_Events()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var user = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Admin login
            var aGet = await admin.GetAsync("/Auth/Login");
            var aHtml = await aGet.Content.ReadAsStringAsync();
            var af = ExtractAntiForgeryToken(aHtml);
            var aPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", af),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.Equal(HttpStatusCode.Redirect, aPost.StatusCode);

            // Create two events
            async Task CreateEventAsync(string title)
            {
                var createGet = await admin.GetAsync("/Admin/Events/Create");
                var createHtml = await createGet.Content.ReadAsStringAsync();
                var token = ExtractAntiForgeryToken(createHtml);
                var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
                var now = DateTime.UtcNow;
                var resp = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", token),
                    new KeyValuePair<string,string>("Title", title),
                    new KeyValuePair<string,string>("Description", "desc"),
                    new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                    new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                    new KeyValuePair<string,string>("RequireSignature", "false"),
                    new KeyValuePair<string,string>("Status", "Active"),
                    new KeyValuePair<string,string>("OrganizationId", orgOption!)
                }));
                Assert.True(resp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            }
            var t1 = "Evt HideAll 1 " + Guid.NewGuid().ToString("N");
            var t2 = "Evt HideAll 2 " + Guid.NewGuid().ToString("N");
            await CreateEventAsync(t1);
            await CreateEventAsync(t2);

            // Create a user in the same organization as created events
            var uCreateGet2 = await admin.GetAsync("/Admin/Users/Create");
            var uCreateHtml2 = await uCreateGet2.Content.ReadAsStringAsync();
            var uCreateToken2 = ExtractAntiForgeryToken(uCreateHtml2);
            var phoneU = "05" + Random.Shared.Next(10000000, 99999999).ToString();
            // Reuse the orgOption from Events/Create page to ensure org match
            var orgGet = await admin.GetAsync("/Admin/Events/Create");
            var orgHtml = await orgGet.Content.ReadAsStringAsync();
            var orgIdForUser = ExtractFirstOptionValueFromSelect(orgHtml, "OrganizationId");
            var uCreatePost2 = await admin.PostAsync("/Admin/Users/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uCreateToken2),
                new KeyValuePair<string,string>("Form.FullName", "HideAllUser"),
                new KeyValuePair<string,string>("Form.Email", $"u_hideall_{Guid.NewGuid():N}@mina.local"),
                new KeyValuePair<string,string>("Form.Phone", phoneU),
                new KeyValuePair<string,string>("Form.OrganizationId", orgIdForUser!),
                new KeyValuePair<string,string>("Form.RoleName", "Attendee"),
                new KeyValuePair<string,string>("Form.IsActive", "true")
            }));
            Assert.True(uCreatePost2.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // User login
            var uGet = await user.GetAsync("/Auth/Login");
            var uHtml = await uGet.Content.ReadAsStringAsync();
            var uAf = ExtractAntiForgeryToken(uHtml);
            var uPost = await user.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", uAf),
                new KeyValuePair<string,string>("Identifier", phoneU),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(uPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // MyEvents contains titles
            var my = await user.GetAsync("/UserPortal/Events");
            var myHtml = await my.Content.ReadAsStringAsync();
            Assert.Contains(t1, myHtml);
            Assert.Contains(t2, myHtml);
            var hideAllToken = ExtractAntiForgeryToken(myHtml);

            // Hide all
            var hideAll = await user.PostAsync("/UserPortal/Events/HideAll", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", hideAllToken)
            }));
            Assert.True(hideAll.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            var myAfter = await user.GetAsync("/UserPortal/Events");
            var myAfterHtml = await myAfter.Content.ReadAsStringAsync();
            Assert.DoesNotContain(t1, myAfterHtml);
            Assert.DoesNotContain(t2, myAfterHtml);
            Assert.Contains("لا توجد أحداث متاحة حالياً", myAfterHtml);
        }


        [Fact]
        public async Task Public_Link_Full_Lifecycle_And_Guest_Interaction_Works()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Admin login
            var lgGet = await admin.GetAsync("/Auth/Login");
            var lgHtml = await lgGet.Content.ReadAsStringAsync();
            var afLogin = ExtractAntiForgeryToken(lgHtml);
            var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afLogin),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(lgPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Create Active event with RequireSignature = true
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            Assert.False(string.IsNullOrEmpty(orgOption));
            var now = DateTime.UtcNow;
            var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "Public Link Lifecycle Test"),
                new KeyValuePair<string,string>("Description", "Desc"),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "true"),
                new KeyValuePair<string,string>("Status", "Active"),
                new KeyValuePair<string,string>("OrganizationId", orgOption!)
            }));
            Assert.True(createPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
            var eventId = ExtractFirstGuid(detailsUrl);
            Assert.NotEqual(Guid.Empty, eventId);

            // Add Survey and Discussion
            var addSurvey = await admin.PostAsync("/Admin/EventComponents/AddSurvey", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "Guest Survey 1")
            }));
            Assert.Equal(HttpStatusCode.OK, addSurvey.StatusCode);
            var addDiscussion = await admin.PostAsync("/Admin/EventComponents/AddDiscussion", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("EventId", eventId.ToString()),
                new KeyValuePair<string,string>("Title", "Guest Discussion"),
                new KeyValuePair<string,string>("Purpose", "Test")
            }));
            Assert.Equal(HttpStatusCode.OK, addDiscussion.StatusCode);

            // Open Edit for AF token used by PublicLinks endpoints
            var editGet = await admin.GetAsync($"/Admin/Events/Edit/{eventId}");
            var editHtml = await editGet.Content.ReadAsStringAsync();
            var afEdit = ExtractAntiForgeryToken(editHtml);
            Assert.False(string.IsNullOrEmpty(afEdit));

            // Generate public link
            var genResp = await admin.PostAsync("/Admin/PublicLinks/Generate", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString())
            }));
            Assert.Equal(HttpStatusCode.OK, genResp.StatusCode);
            var genJson = await genResp.Content.ReadAsStringAsync();
            var urlMatch = Regex.Match(genJson, "\\\"url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            Assert.True(urlMatch.Success, "public url not found in JSON");
            var publicUrl = urlMatch.Groups[1].Value;

            // Toggle OFF
            var toggleOff = await admin.PostAsync("/Admin/PublicLinks/Toggle", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString()),
                new KeyValuePair<string,string>("enabled", "false")
            }));
            Assert.Equal(HttpStatusCode.OK, toggleOff.StatusCode);

            var offResp = await guest.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, offResp.StatusCode);
            var offHtml = await offResp.Content.ReadAsStringAsync();
            Assert.Contains("عذراً، هذا الرابط غير متاح", offHtml);

            // Toggle ON
            var toggleOn = await admin.PostAsync("/Admin/PublicLinks/Toggle", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString()),
                new KeyValuePair<string,string>("enabled", "true")
            }));
            Assert.Equal(HttpStatusCode.OK, toggleOn.StatusCode);

            // Set expiry in the past
            var pastIso = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("o");
            var setPast = await admin.PostAsync("/Admin/PublicLinks/SetExpiry", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString()),
                new KeyValuePair<string,string>("expiresAt", pastIso)
            }));
            Assert.Equal(HttpStatusCode.OK, setPast.StatusCode);

            var expResp = await guest.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, expResp.StatusCode);
            var expHtml = await expResp.Content.ReadAsStringAsync();
            Assert.Contains("عذراً، هذا الرابط غير متاح", expHtml);

            // Clear expiry (post without expiresAt)
            var clear = await admin.PostAsync("/Admin/PublicLinks/SetExpiry", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString())
            }));
            Assert.Equal(HttpStatusCode.OK, clear.StatusCode);

            // Now EnterName page should render
            var enterGet = await guest.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, enterGet.StatusCode);
            var enterHtml = await enterGet.Content.ReadAsStringAsync();
            var afGuest = ExtractAntiForgeryToken(enterHtml);
            Assert.False(string.IsNullOrEmpty(afGuest));
            Assert.Contains("الاسم الكامل", enterHtml);

            // EnterName
            var enterPost = await guest.PostAsync(publicUrl + "/EnterName", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afGuest),
                new KeyValuePair<string,string>("fullName", "ضيف دورة حياة"),
                new KeyValuePair<string,string>("email", "guest.lifecycle@example.com"),
                new KeyValuePair<string,string>("phone", "0501234567")
            }));
            Assert.True(enterPost.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);

            // Details page for guest
            var details = await guest.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, details.StatusCode);
            var detailsHtml = await details.Content.ReadAsStringAsync();
            var afPart = ExtractAntiForgeryToken(detailsHtml);
            Assert.False(string.IsNullOrEmpty(afPart));
            var qOpt = ExtractFirstSingleQuestionAndOption(detailsHtml);
            var discussionId = ExtractFirstDiscussionId(detailsHtml);

            var fields = new List<KeyValuePair<string,string>>
            {
                new("__RequestVerificationToken", afPart),
                new("EventId", eventId.ToString()),
                new("SignatureData", "data:image/png;base64,AA==")
            };
            if (qOpt is { } qo)
            {
                fields.Add(new($"SurveyAnswers[{qo.qId}]", qo.optId.ToString()));
            }
            if (discussionId != null)
            {
                fields.Add(new($"DiscussionReplies[{discussionId}]", "رد ضيف"));
            }

            var submit = await guest.PostAsync(publicUrl + "/Submit", new FormUrlEncodedContent(fields));
            Assert.True(submit.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Admin Summary contains guest badge/text
            var summary = await admin.GetAsync($"/Admin/EventResults/Summary?eventId={eventId}");
            Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
            var summaryHtml = await summary.Content.ReadAsStringAsync();
            Assert.Contains("ضيف", summaryHtml);

            // Edge: Reopen link with same guest client should skip EnterName (cookie)
            var reopen = await guest.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
            var reopenHtml = await reopen.Content.ReadAsStringAsync();
            Assert.DoesNotContain("الاسم الكامل", reopenHtml);
        }

        [Fact]
        public async Task Public_Details_Has_PublicSubmit_Action_And_Scripts_Present()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var admin = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            // Admin login
            var lgGet = await admin.GetAsync("/Auth/Login");
            var lgHtml = await lgGet.Content.ReadAsStringAsync();
            var afLogin = ExtractAntiForgeryToken(lgHtml);
            var lgPost = await admin.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afLogin),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(lgPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Create Active event (RequireSignature=true to ensure signature UI is present)
            var createGet = await admin.GetAsync("/Admin/Events/Create");
            var createHtml = await createGet.Content.ReadAsStringAsync();
            var afCreate = ExtractAntiForgeryToken(createHtml);
            var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId");
            Assert.False(string.IsNullOrEmpty(orgOption));
            var now = DateTime.UtcNow;
            var createPost = await admin.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afCreate),
                new KeyValuePair<string,string>("Title", "Public Details Scripts Test"),
                new KeyValuePair<string,string>("Description", "Desc"),
                new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                new KeyValuePair<string,string>("RequireSignature", "true"),
                new KeyValuePair<string,string>("Status", "Active"),
                new KeyValuePair<string,string>("OrganizationId", orgOption!)
            }));
            Assert.True(createPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
            var detailsUrl = createPost.Headers.Location?.ToString() ?? string.Empty;
            var eventId = ExtractFirstGuid(detailsUrl);
            Assert.NotEqual(Guid.Empty, eventId);

            // Prepare to generate public link
            var editGet = await admin.GetAsync($"/Admin/Events/Edit/{eventId}");
            var editHtml = await editGet.Content.ReadAsStringAsync();
            var afEdit = ExtractAntiForgeryToken(editHtml);

            // Generate link
            var genResp = await admin.PostAsync("/Admin/PublicLinks/Generate", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afEdit),
                new KeyValuePair<string,string>("eventId", eventId.ToString())
            }));
            Assert.Equal(HttpStatusCode.OK, genResp.StatusCode);
            var genJson = await genResp.Content.ReadAsStringAsync();
            var urlMatch = Regex.Match(genJson, "\\\"url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            Assert.True(urlMatch.Success, "public url not found in JSON");
            var publicUrl = urlMatch.Groups[1].Value;

            // EnterName page
            var enterGet = await guest.GetAsync(publicUrl);
            var enterHtml = await enterGet.Content.ReadAsStringAsync();
            var afGuest = ExtractAntiForgeryToken(enterHtml);
            Assert.False(string.IsNullOrEmpty(afGuest));

            var enterPost = await guest.PostAsync(publicUrl + "/EnterName", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afGuest),
                new KeyValuePair<string,string>("fullName", "ضيف اختبار السكربتات"),
                new KeyValuePair<string,string>("email", "guest.scripts@example.com"),
                new KeyValuePair<string,string>("phone", "0500000000")
            }));
            Assert.True(enterPost.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);

            // Details page HTML
            var details = await guest.GetAsync(publicUrl);
            var html = await details.Content.ReadAsStringAsync();

            // Extract token from URL and assert action points to /Public/Event/{token}/Submit
            var tokenMatch = Regex.Match(publicUrl, "/Public/Event/([^/]+)");
            Assert.True(tokenMatch.Success, "token not found in publicUrl");
            var token = tokenMatch.Groups[1].Value;
            var expectedAction = $"/Public/Event/{token}/Submit";
            Assert.True(html.Contains(expectedAction) || html.Contains("form.setAttribute('action', `/Public/Event/"), $"Expected to find '{expectedAction}' or the client-side action setter in the HTML.");

            // Shared scripts/modal present
            Assert.Contains("signature-pad.js", html);
            Assert.Contains("id=\"imageModal\"", html);
        }

}
