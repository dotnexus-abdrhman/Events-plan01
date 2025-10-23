using Xunit;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RourtPPl01.SmokeTests
{
    public class BulkDeleteTests
    {
        private static string ExtractAntiForgeryToken(string html)
            => Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")?.Groups[1].Value ?? string.Empty;

        private static string? ExtractFirstOptionValueFromSelect(string html, string selectName)
        {
            var mSelect = Regex.Match(html, "<select[^>]*name=\\\"" + Regex.Escape(selectName) + "\\\"[\\s\\S]*?</select>", RegexOptions.IgnoreCase);
            if (!mSelect.Success) return null;
            var block = mSelect.Value;
            var mOpt = Regex.Match(block, "<option[^>]*value=\\\"([0-9a-fA-F-]{36})\\\"", RegexOptions.IgnoreCase);
            return mOpt.Success ? mOpt.Groups[1].Value : null;
        }

        private static Guid ExtractFirstGuid(string text)
        {
            var m = Regex.Match(text, "[0-9a-fA-F-]{36}");
            return m.Success ? Guid.Parse(m.Value) : Guid.Empty;
        }

        [Fact]
        public async Task Admin_Can_Bulk_Delete_Events_Successfully()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Login as admin
            var loginGet = await client.GetAsync("/Auth/Login");
            var loginHtml = await loginGet.Content.ReadAsStringAsync();
            var afLogin = ExtractAntiForgeryToken(loginHtml);
            var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afLogin),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Helper to create an event and return its id
            async Task<Guid> CreateEventAsync(string title)
            {
                var createGet = await client.GetAsync("/Admin/Events/Create");
                var createHtml = await createGet.Content.ReadAsStringAsync();
                var af = ExtractAntiForgeryToken(createHtml);
                var orgOption = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId")!;
                var now = DateTime.UtcNow;
                var resp = await client.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", af),
                    new KeyValuePair<string,string>("Title", title),
                    new KeyValuePair<string,string>("Description", "to-delete"),
                    new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                    new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                    new KeyValuePair<string,string>("RequireSignature", "false"),
                    new KeyValuePair<string,string>("Status", "Draft"),
                    new KeyValuePair<string,string>("OrganizationId", orgOption)
                }));
                Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
                var detailsUrl = resp.Headers.Location?.ToString() ?? string.Empty;
                var id = ExtractFirstGuid(detailsUrl);
                Assert.NotEqual(Guid.Empty, id);
                return id;
            }

            var id1 = await CreateEventAsync("to-delete " + Guid.NewGuid().ToString("N").Substring(0,6));
            var id2 = await CreateEventAsync("to-delete " + Guid.NewGuid().ToString("N").Substring(0,6));

            // Fetch anti-forgery token from Events index (bulk form)
            var indexGet = await client.GetAsync("/Admin/Events");
            var indexHtml = await indexGet.Content.ReadAsStringAsync();
            var afBulk = ExtractAntiForgeryToken(indexHtml);
            Assert.False(string.IsNullOrEmpty(afBulk));

            // POST bulk delete
            var form = new List<KeyValuePair<string,string>>
            {
                new("__RequestVerificationToken", afBulk),
                new("selectedIds", id1.ToString()),
                new("selectedIds", id2.ToString())
            };
            var delResp = await client.PostAsync("/Admin/Events/BulkDelete", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
            var json = await delResp.Content.ReadAsStringAsync();
            Assert.Contains("\"success\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);

            // Verify Details now redirect (not found)
            var d1 = await client.GetAsync($"/Admin/Events/Details/{id1}");
            Assert.Equal(HttpStatusCode.Redirect, d1.StatusCode);
            var d2 = await client.GetAsync($"/Admin/Events/Details/{id2}");
            Assert.Equal(HttpStatusCode.Redirect, d2.StatusCode);
        }

        [Fact]
        public async Task Admin_Can_Bulk_Delete_All_Existing_Events()
        {
            await using var factory = new WebApplicationFactory<RourtPPl01.Program>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Login as admin
            var loginGet = await client.GetAsync("/Auth/Login");
            var loginHtml = await loginGet.Content.ReadAsStringAsync();
            var afLogin = ExtractAntiForgeryToken(loginHtml);
            var loginPost = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", afLogin),
                new KeyValuePair<string,string>("Identifier", "admin@mina.local"),
                new KeyValuePair<string,string>("RememberMe", "false")
            }));
            Assert.True(loginPost.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);

            // Create a fixed set of events to represent "Select All" scenario (stable and isolated)
            var createdIds = new List<Guid>();
            async Task<Guid> CreateAsync(string title)
            {
                var createGet = await client.GetAsync("/Admin/Events/Create");
                var createHtml = await createGet.Content.ReadAsStringAsync();
                var af = ExtractAntiForgeryToken(createHtml);
                var org = ExtractFirstOptionValueFromSelect(createHtml, "OrganizationId")!;
                var now = DateTime.UtcNow;
                var resp = await client.PostAsync("/Admin/Events/Create", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("__RequestVerificationToken", af),
                    new KeyValuePair<string,string>("Title", title),
                    new KeyValuePair<string,string>("Description", "bulk-all"),
                    new KeyValuePair<string,string>("StartAt", now.ToString("s")),
                    new KeyValuePair<string,string>("EndAt", now.AddHours(1).ToString("s")),
                    new KeyValuePair<string,string>("RequireSignature", "false"),
                    new KeyValuePair<string,string>("Status", "Draft"),
                    new KeyValuePair<string,string>("OrganizationId", org)
                }));
                Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
                var detailsUrl = resp.Headers.Location?.ToString() ?? string.Empty;
                var id = ExtractFirstGuid(detailsUrl);
                Assert.NotEqual(Guid.Empty, id);
                return id;
            }

            for (int i = 0; i < 5; i++)
                createdIds.Add(await CreateAsync($"bulk-all-{i}-" + Guid.NewGuid().ToString("N").Substring(0,6)));

            // Fetch anti-forgery token from Events index (bulk form)
            var indexGet = await client.GetAsync("/Admin/Events");
            var indexHtml = await indexGet.Content.ReadAsStringAsync();
            var afBulk = ExtractAntiForgeryToken(indexHtml);
            Assert.False(string.IsNullOrEmpty(afBulk));

            // Submit bulk delete for the created events only
            var form = new List<KeyValuePair<string,string>> { new("__RequestVerificationToken", afBulk) };
            foreach (var id in createdIds)
                form.Add(new("selectedIds", id.ToString()));

            var delResp = await client.PostAsync("/Admin/Events/BulkDelete", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
            var json = await delResp.Content.ReadAsStringAsync();
            Assert.Contains("\"success\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);

            // Verify those events are gone
            foreach (var id in createdIds)
            {
                var detailsResp = await client.GetAsync($"/Admin/Events/Details/{id}");
                Assert.Equal(HttpStatusCode.Redirect, detailsResp.StatusCode);
            }
        }

    }
}

