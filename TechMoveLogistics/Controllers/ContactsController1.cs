using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ContractsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions;

        // Decoupled: Injected IHttpClientFactory to replace direct workflow database access
        public ContractsController(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
        {
            _httpClient = httpClientFactory.CreateClient("TechMoveAPI");
            _environment = environment;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Helper Method: Handles the behind-the-scenes JWT authentication handshake with TechMove.API
        private async Task<bool> AuthenticateClientAsync()
        {
            var loginPayload = new { Username = "admin", Password = "password123" };
            var jsonContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", jsonContent);
            if (!response.IsSuccessStatusCode) return false;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
            {
                var token = tokenProp.GetString();
                // Attach the JWT Bearer validation token to your outgoing HTTP request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }

            return false;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? statusFilter)
        {
            if (!await AuthenticateClientAsync())
            {
                ViewBag.ErrorMessage = "Secure API Authentication Handshake Failed.";
                return View(new List<Contract>());
            }

            // Build dynamic query variables to match your API backend LINQ filters
            var statusString = statusFilter?.ToString();
            var queryPath = $"/api/contracts?status={statusString}&startDate={startDate?.ToString("yyyy-MM-dd")}&endDate={endDate?.ToString("yyyy-MM-dd")}";

            var response = await _httpClient.GetAsync(queryPath);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Error retrieving contracts from secure service layer.";
                return View(new List<Contract>());
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var filteredContracts = JsonSerializer.Deserialize<List<Contract>>(jsonString, _jsonOptions) ?? new List<Contract>();

            // Retained your exact Task 2 UI binding keys
            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(filteredContracts);
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            if (!await AuthenticateClientAsync())
            {
                ViewBag.ErrorMessage = "Authentication with backend api failed.";
                return View();
            }

            // Fetch active Client data list via API endpoint instead of direct DB calls
            var response = await _httpClient.GetAsync("/api/contracts"); // Reuses GET endpoint to fetch related structural information
            var jsonString = await response.Content.ReadAsStringAsync();
            var contracts = JsonSerializer.Deserialize<List<Contract>>(jsonString, _jsonOptions) ?? new List<Contract>();

            // Extract distinct Client models from the service layer data stream safely
            var clients = contracts.Select(c => c.Client).Where(c => c != null).GroupBy(c => c!.Id).Select(g => g.First()).ToList();

            ViewBag.Clients = new SelectList(clients, "Id", "Name");
            return View();
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, IFormFile? signedAgreement)
        {
            if (!await AuthenticateClientAsync())
            {
                ModelState.AddModelError("", "Secure identity credentials rejected by background server.");
                return View(contract);
            }

            if (ModelState.IsValid)
            {
                string uniqueFileName = "";
                string displayFileName = "default_agreement.pdf";

                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    var fileExtension = Path.GetExtension(signedAgreement.FileName).ToLower();
                    if (fileExtension != ".pdf")
                    {
                        ModelState.AddModelError("SignedAgreementFileName", "Only PDF files are allowed.");

                        // Fallback client fetching logic over API
                        var clientResponse = await _httpClient.GetAsync("/api/contracts");
                        var clientJson = await clientResponse.Content.ReadAsStringAsync();
                        var fallbackContracts = JsonSerializer.Deserialize<List<Contract>>(clientJson, _jsonOptions) ?? new List<Contract>();
                        var fallbackClients = fallbackContracts.Select(c => c.Client).Where(c => c != null).GroupBy(c => c!.Id).Select(g => g.First()).ToList();

                        ViewBag.Clients = new SelectList(fallbackClients, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }

                    // Retained your exact UUID file naming logic patterns
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(signedAgreement.FileName);
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "agreements");

                    // Create the local directories if they don't exist yet on frontend
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await signedAgreement.CopyToAsync(fileStream);
                    }

                    displayFileName = signedAgreement.FileName;
                    contract.SignedAgreementFilePath = "/uploads/agreements/" + uniqueFileName;
                }

                // Map UI fields into a lightweight structural object matching API DTO schemas
                var contractDto = new
                {
                    ClientId = contract.ClientId,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    ServiceLevel = contract.ServiceLevel ?? "Standard",
                    SignedAgreementFileName = displayFileName
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(contractDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/contracts", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Server rejected contract creation. Verify your workflow validation parameters.");
            }

            // Fallback for invalid model states over API channels
            var finalResponse = await _httpClient.GetAsync("/api/contracts");
            var finalJson = await finalResponse.Content.ReadAsStringAsync();
            var errorContracts = JsonSerializer.Deserialize<List<Contract>>(finalJson, _jsonOptions) ?? new List<Contract>();
            var errorClients = errorContracts.Select(c => c.Client).Where(c => c != null).GroupBy(c => c!.Id).Select(g => g.First()).ToList();

            ViewBag.Clients = new SelectList(errorClients, "Id", "Name", contract.ClientId);
            return View(contract);
        }
    }
}
