using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Controllers
{
    public class ContractsController : Controller
    {
        private readonly LogisticsDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ContractsController(LogisticsDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? statusFilter)
        {
            // Query capture matching IQueryable tracking
            var contractsQuery = _context.Contracts.Include(c => c.Client).AsQueryable();

            //LINQ Filter by Status if provided
            if (statusFilter.HasValue)
            {
                contractsQuery = contractsQuery.Where(c => c.Status == statusFilter.Value);
            }

            //LINQ Filter by Date Ranges if provided
            if (startDate.HasValue)
            {
                contractsQuery = contractsQuery.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                contractsQuery = contractsQuery.Where(c => c.EndDate <= endDate.Value);
            }

            //Pass options back to UI
            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(await contractsQuery.ToListAsync());
        }

        // GET: Contract
        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = new SelectList(await _context.Clients.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Contracts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    // PDF validation
                    var fileExtension = Path.GetExtension(signedAgreement.FileName).ToLower();
                    if (fileExtension != ".pdf")
                    {
                        ModelState.AddModelError("SignedAgreementFileName", "Only PDF files are allowed.");
                        ViewBag.Clients = new SelectList(await _context.Clients.ToListAsync(), "Id", "Name", contract.ClientId);
                        return View(contract);
                    }

                    // Create unique file name
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(signedAgreement.FileName);
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "agreements");
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);

                    // Saving file to the wwwroot folder path
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await signedAgreement.CopyToAsync(fileStream);
                    }

                    // Storing details in DB
                    contract.SignedAgreementFileName = signedAgreement.FileName;
                    contract.SignedAgreementFilePath = "/uploads/agreements/" + uniqueFileName;
                }

                _context.Add(contract);
                await _context.SaveChangesAsync();

                //Redirect admin to the view matrix table list
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = new SelectList(await _context.Clients.ToListAsync(), "Id", "Name", contract.ClientId);
            return View(contract);
        }
    }
}
