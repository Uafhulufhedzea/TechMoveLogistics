using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ContractsController : Controller
    {
        private readonly LogisticsWorkflowService _workflowService;
        private readonly IWebHostEnvironment _environment;

        public ContractsController(LogisticsWorkflowService workflowService, IWebHostEnvironment environment)
        {
            _workflowService = workflowService;
            _environment = environment;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? statusFilter)
        {
            var filteredContracts = await _workflowService.GetFilteredContractsAsync(startDate, endDate, statusFilter);

            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(filteredContracts);
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            var clients = await _workflowService.GetAllClientsAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "Name");
            return View();
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    var fileExtension = Path.GetExtension(signedAgreement.FileName).ToLower();
                    if (fileExtension != ".pdf")
                    {
                        ModelState.AddModelError("SignedAgreementFileName", "Only PDF files are allowed.");
                        var clientsList = await _workflowService.GetAllClientsAsync();
                        ViewBag.Clients = new SelectList(clientsList, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }

                    // UUID naming standard
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(signedAgreement.FileName);
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "agreements");
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await signedAgreement.CopyToAsync(fileStream);
                    }

                    contract.SignedAgreementFileName = signedAgreement.FileName;
                    contract.SignedAgreementFilePath = "/uploads/agreements/" + uniqueFileName;
                }

                await _workflowService.SaveContractAsync(contract);
                return RedirectToAction(nameof(Index));
            }

            var clientsFallback = await _workflowService.GetAllClientsAsync();
            ViewBag.Clients = new SelectList(clientsFallback, "Id", "Name", contract.ClientId);
            return View(contract);
        }
    }
}
