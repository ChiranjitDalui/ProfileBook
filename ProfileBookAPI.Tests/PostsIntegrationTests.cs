using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace ProfileBookAPI.Tests
{
    public class PostsIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly HttpClient _client;
        private readonly IntegrationTestFactory _factory;

        public PostsIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreatePost_WithoutImage_ReturnsPending()
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent("This is a test post content"), "Content");

            var response = await _client.PostAsync("/api/posts", form);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("status", out var statusProp))
            {
                statusProp.GetString().Should().Be("Pending");
            }
            else
            {
                json.Should().Contain("\"status\":\"Pending\"");
            }
        }

        [Fact]
        public async Task ApprovePost_AsAdmin_ChangesStatus()
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent("Please approve"), "Content");
            var createRes = await _client.PostAsync("/api/posts", form);
            createRes.EnsureSuccessStatusCode();
            var createdJson = await createRes.Content.ReadAsStringAsync();

            int createdId = -1;
            using (var doc = JsonDocument.Parse(createdJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                    createdId = idProp.GetInt32();
                else if (root.TryGetProperty("Id", out var idProp2) && idProp2.ValueKind == JsonValueKind.Number)
                    createdId = idProp2.GetInt32();
            }

            createdId.Should().BeGreaterThan(0);

            var approveRes = await _client.PutAsync($"/api/posts/approve/{createdId}", null);
            approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var allRes = await _client.GetAsync("/api/posts/all");
            allRes.EnsureSuccessStatusCode();
            var allJson = await allRes.Content.ReadAsStringAsync();

            using var allDoc = JsonDocument.Parse(allJson);
            var rootArr = allDoc.RootElement;
            rootArr.ValueKind.Should().Be(JsonValueKind.Array);

            bool foundApproved = false;
            foreach (var el in rootArr.EnumerateArray())
            {
                int id = -1;
                if (el.TryGetProperty("id", out var idP) && idP.ValueKind == JsonValueKind.Number) id = idP.GetInt32();
                else if (el.TryGetProperty("Id", out var idP2) && idP2.ValueKind == JsonValueKind.Number) id = idP2.GetInt32();

                if (id == createdId)
                {
                    if (el.TryGetProperty("status", out var statusEl))
                    {
                        statusEl.GetString().Should().Be("Approved");
                        foundApproved = true;
                        break;
                    }
                    else if (el.TryGetProperty("Status", out var statusEl2))
                    {
                        statusEl2.GetString().Should().Be("Approved");
                        foundApproved = true;
                        break;
                    }
                }
            }

            foundApproved.Should().BeTrue(because: $"Post {createdId} should be Approved");
        }
    }
}
