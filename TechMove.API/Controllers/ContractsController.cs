using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TechMove.API.DTOs;
using TechMove.API.Services;

namespace TechMove.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly IHttpClientFactory _httpClientFactory;

        // Injected IHttpClientFactory to consume external exchange rate data securely on the service layer
        public ContractsController(IContractService contractService, IHttpClientFactory httpClientFactory)
        {
            _contractService = contractService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetContracts([FromQuery] string? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var contracts = await _contractService.GetFilteredContractsAsync(status, startDate, endDate);
            return Ok(contracts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract([FromBody] ContractCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var contract = await _contractService.CreateContractAsync(dto);
            return CreatedAtAction(nameof(GetContracts), new { id = contract.Id }, contract);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ContractStatusUpdateDto dto)
        {
            if (string.IsNullOrEmpty(dto.Status)) return BadRequest("Status cannot be empty.");

            var updatedContract = await _contractService.UpdateContractStatusAsync(id, dto.Status);
            if (updatedContract == null) return NotFound($"Contract ID {id} was not found or status is invalid.");

            return Ok(updatedContract);
        }

        [HttpPost("servicerequests")]
        public async Task<IActionResult> LogServiceRequest([FromBody] ServiceRequestCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var request = await _contractService.CreateServiceRequestAsync(dto);
                if (request == null) return NotFound("Parent contract was not found.");
                return Ok(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // NEW ENDPOINT: Securely fetches external currency data and performs calculation rules natively
        [HttpGet("convert-currency")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] decimal amountInUsd)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Consumes a free, reliable open-exchange tracking rate structure
                var response = await client.GetAsync("https://er-api.com");

                decimal exchangeRate = 18.50m; // Solid production fallback margin value if network is throttled

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("rates", out var rates) && rates.TryGetProperty("ZAR", out var zarProp))
                    {
                        exchangeRate = zarProp.GetDecimal();
                    }
                }

                decimal finalizedCostInZar = amountInUsd * exchangeRate;

                return Ok(new
                {
                    ExchangeRate = exchangeRate,
                    ConvertedAmount = Math.Round(finalizedCostInZar, 2)
                });
            }
            catch (Exception ex)
            {
                // Smooth safe handling fallback matrix if external sandbox endpoint suffers downtime
                decimal fallbackZar = amountInUsd * 18.50m;
                return Ok(new { ExchangeRate = 18.50m, ConvertedAmount = Math.Round(fallbackZar, 2) });
            }
        }
    }
}
