using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Tests
{
    public class ContractIntegrationTests : IClassFixture<WebApplicationFactory<TechMove.API.Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<TechMove.API.Program> _factory;
        private static int _seededClientId;

        public ContractIntegrationTests(WebApplicationFactory<TechMove.API.Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Override the connection string dynamically to point to a clean, isolated testing database file
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // This forces the in-memory test host to run its migrations on a separate test database catalog
                    context.Configuration["ConnectionStrings:DefaultConnection"] =
                        "Server=(localdb)\\mssqllocaldb;Database=TechMoveTestingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
                });

                builder.ConfigureServices(services =>
                {
                    // Bypass real JWT authentication verification during tests to prevent configuration file crashes
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                    })
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                });
            });

            _client = _factory.CreateClient();

            // Seed required structural lookup data cleanly on our live testing database layout
            SeedLookupData();
        }

        private void SeedLookupData()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();

                // Automatically create the physical database file and tables if they don't exist yet
                context.Database.EnsureCreated();

                // If no clients exist inside the test database catalog, add one safely
                if (!context.Clients.Any())
                {
                    var testClient = new Client
                    {
                        Name = "Integration Test Client",
                        ContactDetails = "test@techmove.com",
                        Region = "Standard"
                    };

                    context.Clients.Add(testClient);
                    context.SaveChanges();

                    _seededClientId = testClient.Id;
                }
                else
                {
                    _seededClientId = context.Clients.First().Id;
                }
            }
        }

        [Fact]
        public async Task GetContracts_ReturnsSuccessStatusCode_AndNonNullJson()
        {
            // Act: Call your running API endpoint
            var response = await _client.GetAsync("/api/contracts");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(responseString));
        }

        [Fact]
        public async Task CreateThenReadContract_VerifiesDataIntegrityNatively()
        {
            // 1. Arrange: Setup your contract data payload pointing to our dynamically generated client ID
            var newContractPayload = new
            {
                ClientId = _seededClientId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                ServiceLevel = "Premium Express Integration Test",
                SignedAgreementFileName = "integration_test_proof.pdf"
            };

            var postContent = new StringContent(JsonSerializer.Serialize(newContractPayload), Encoding.UTF8, "application/json");

            // 2. Act Part 1: CREATE the contract via POST endpoint
            var postResponse = await _client.PostAsync("/api/contracts", postContent);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // 3. Act Part 2: READ the dataset to verify storage integrity
            var getResponse = await _client.GetAsync("/api/contracts");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var responseString = await getResponse.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseString);
            var contractsArray = jsonDoc.RootElement;

            // 4. Assert: Verify the structural data layout
            Assert.Equal(JsonValueKind.Array, contractsArray.ValueKind);

            bool matchFound = false;
            foreach (var element in contractsArray.EnumerateArray())
            {
                if (element.TryGetProperty("serviceLevel", out var serviceLevelProp) &&
                    serviceLevelProp.GetString() == "Premium Express Integration Test")
                {
                    matchFound = true;
                    var clientIdProp = element.GetProperty("clientId");
                    Assert.Equal(_seededClientId, clientIdProp.GetInt32());
                    break;
                }
            }

            Assert.True(matchFound, "The created contract data was not found in the returned API validation stream.");
        }
    }

    // Lightweight test utility class to mock security validation rules cleanly without using files
    public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestAdmin"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Administrator")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestScheme");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }
}
