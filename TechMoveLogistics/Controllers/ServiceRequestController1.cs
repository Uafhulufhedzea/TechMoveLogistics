using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly LogisticsWorkflowService _workflowService;
        private readonly IHttpClientFactory _clientFactory;

        public ServiceRequestsController(LogisticsWorkflowService workflowService, IHttpClientFactory clientFactory)
        {
            _workflowService = workflowService;
            _clientFactory = clientFactory;
        }

        // GET: ServiceRequests/Create
        public async Task<IActionResult> Create()
        {
            var contracts = await _workflowService.GetFilteredContractsAsync(null, null, null);
            ViewBag.Contracts = new SelectList(contracts, "Id", "Id");

            decimal usdToZarRate = 18.50m; // Fallback

            try
            {
                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync("https://er-api.com");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                        rates.TryGetProperty("ZAR", out var zarProperty))
                    {
                        usdToZarRate = zarProperty.GetDecimal();
                    }
                }
            }
            catch
            {
                //Error handling fallback
            }

            ViewBag.ExchangeRate = usdToZarRate;
            return View();
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest, decimal usdAmount, decimal exchangeRate)
        {
            // Delegation of all validation parameters
            var workflowResult = await _workflowService.ProcessServiceRequestAsync(serviceRequest, usdAmount, exchangeRate);

            if (!workflowResult.IsValid)
            {
                ModelState.AddModelError("ContractId", workflowResult.ErrorMessage);
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index", "Contracts");
            }

            var contractsFallback = await _workflowService.GetFilteredContractsAsync(null, null, null);
            ViewBag.Contracts = new SelectList(contractsFallback, "Id", "Id", serviceRequest.ContractId);
            ViewBag.ExchangeRate = exchangeRate;
            return View(serviceRequest);
        }
    }
}
