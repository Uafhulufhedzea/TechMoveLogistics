using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ServiceRequestsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("TechMoveAPI");
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Helper Method: Handles the automated background authentication handshake with TechMove.API
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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }

            return false;
        }

        // GET: ServiceRequests/Create
        public async Task<IActionResult> Create()
        {
            if (!await AuthenticateClientAsync())
            {
                ViewBag.ErrorMessage = "Secure API Authentication Handshake Failed.";
                return View();
            }

            // Fetch contracts list via API to populate the active Dropdown Selection Menu
            var response = await _httpClient.GetAsync("/api/contracts");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Failed to retrieve reference contracts from service layer.";
                return View();
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var contracts = JsonSerializer.Deserialize<List<Contract>>(jsonString, _jsonOptions) ?? new List<Contract>();

            // Filter out only Active contracts to simplify valid UI entries for users
            var activeContracts = contracts.Where(c => c.Status == ContractStatus.Active).ToList();

            ViewBag.Contracts = new SelectList(activeContracts, "Id", "Id");

            // Set a default exchange rate for initial page load
            ViewBag.ExchangeRate = 18.50m;

            return View();
        }

        // BACKEND ROUTE: Provides the raw numeric exchange rate value to the JavaScript listener safely
        [HttpGet]
        public async Task<IActionResult> GetLiveRateValue()
        {
            if (!await AuthenticateClientAsync()) return Json(new { rate = 18.50 });

            var response = await _httpClient.GetAsync("/api/contracts/convert-currency?amountInUsd=1");
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("exchangeRate", out var rateProp))
                {
                    return Json(new { rate = rateProp.GetDecimal() });
                }
            }
            return Json(new { rate = 18.50 });
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            if (!await AuthenticateClientAsync())
            {
                ModelState.AddModelError("", "Authentication failure with service layer.");
                return View(serviceRequest);
            }

            if (ModelState.IsValid)
            {
                var requestDto = new
                {
                    ContractId = serviceRequest.ContractId,
                    Description = serviceRequest.Description,
                    Cost = serviceRequest.Cost // Saves the populated ZAR calculation value straight to DB records
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/contracts/servicerequests", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index", "Contracts");
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", errorMsg ?? "Server rejected request creation. Verify contract business boundaries.");
            }

            // Re-populate data layouts on model execution validation fallbacks
            var contractResponse = await _httpClient.GetAsync("/api/contracts");
            var contractJson = await contractResponse.Content.ReadAsStringAsync();
            var fallbackContracts = JsonSerializer.Deserialize<List<Contract>>(contractJson, _jsonOptions) ?? new List<Contract>();
            ViewBag.Contracts = new SelectList(fallbackContracts.Where(c => c.Status == ContractStatus.Active), "Id", "Id", serviceRequest.ContractId);

            return View(serviceRequest);
        }
    }
}
