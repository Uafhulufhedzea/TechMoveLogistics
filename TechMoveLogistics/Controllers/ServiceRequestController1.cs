using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly LogisticsDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public ServiceRequestsController(LogisticsDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        // GET: ServiceRequests
        public async Task<IActionResult> Create()
        {
            ViewBag.Contracts = new SelectList(await _context.Contracts.Include(c => c.Client).ToListAsync(), "Id", "Id");

            //Fallback exchange rate if API is down
            decimal usdToZarRate = 18.50m;

            try
            {
                // Consume the free standard ExchangeRate-API
                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync("https://er-api.com");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    //Extract the ZAR conversion metric rate
                    if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                        rates.TryGetProperty("ZAR", out var zarProperty))
                    {
                        usdToZarRate = zarProperty.GetDecimal();
                    }
                }
            }
            catch
            {
                
            }

            // Send the live rate to the front-end view
            ViewBag.ExchangeRate = usdToZarRate;
            return View();
        }

        // POST: ServiceRequests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            var parentContract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == serviceRequest.ContractId);

            if (parentContract == null)
            {
                ModelState.AddModelError("ContractId", "The selected contract does not exist.");
            }
            else if (parentContract.Status == ContractStatus.Expired || parentContract.Status == ContractStatus.OnHold)
            {
                ModelState.AddModelError("ContractId", $"Workflow Violation: Cannot log requests against an {parentContract.Status} contract.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Contracts");
            }

            ViewBag.Contracts = new SelectList(await _context.Contracts.ToListAsync(), "Id", "Id", serviceRequest.ContractId);
            return View(serviceRequest);
        }
    }
}
