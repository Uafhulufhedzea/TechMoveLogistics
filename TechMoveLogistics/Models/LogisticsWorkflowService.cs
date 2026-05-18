using Microsoft.EntityFrameworkCore;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Data
{
    public class LogisticsWorkflowService
    {
        private readonly LogisticsDbContext _context;
        private readonly List<IObserver> _observers = new();

        public LogisticsWorkflowService(LogisticsDbContext context)
        {
            _context = context;
            
            _observers.Add(new NotificationService());
        }

     
        public async Task<List<Contract>> GetFilteredContractsAsync(DateTime? start, DateTime? end, ContractStatus? status)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }
            if (start.HasValue)
            {
                query = query.Where(c => c.StartDate >= start.Value);
            }
            if (end.HasValue)
            {
                query = query.Where(c => c.EndDate <= end.Value);
            }

            return await query.ToListAsync();
        }

      
        public async Task SaveContractAsync(Contract contract)
        {
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients.ToListAsync();
        }

     
        public async Task<(bool IsValid, string ErrorMessage)> ProcessServiceRequestAsync(ServiceRequest request, decimal usdAmount, decimal rate)
        {
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == request.ContractId);

            if (contract == null)
            {
                return (false, "The selected contract does not exist.");
            }

            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                string alertMsg = $"BLOCKED - Service request creation failed against {contract.Status} contract ID {contract.Id}";

             
                _observers.ForEach(o => o.OnContractActivity(alertMsg));

                return (false, $"Workflow Violation: Cannot log requests against an {contract.Status} contract.");
            }

            var costStrategy = ServiceFactory.GetCostStrategy(contract.Client?.Region ?? "Standard");
            request.Cost = costStrategy.CalculateFinalCost(usdAmount, rate);

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();

            return (true, string.Empty);
        }
    }
}

