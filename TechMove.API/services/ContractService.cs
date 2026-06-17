using Microsoft.EntityFrameworkCore;
using TechMoveLogistics.Data;
using TechMoveLogistics.Models;
using TechMove.API.DTOs;

namespace TechMove.API.Services
{
    public interface IContractService
    {
        Task<List<Contract>> GetFilteredContractsAsync(string? status, DateTime? startDate, DateTime? endDate);
        Task<Contract> CreateContractAsync(ContractCreateDto dto);
        Task<Contract?> UpdateContractStatusAsync(int id, string status);
        Task<ServiceRequest?> CreateServiceRequestAsync(ServiceRequestCreateDto dto);
    }

    public class ContractService : IContractService
    {
        private readonly LogisticsDbContext _context;

        public ContractService(LogisticsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contract>> GetFilteredContractsAsync(string? status, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
            }

            if (startDate.HasValue && startDate.Value > new DateTime(1753, 1, 1))
            {
                query = query.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue && endDate.Value > new DateTime(1753, 1, 1))
            {
                query = query.Where(c => c.EndDate <= endDate.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<Contract> CreateContractAsync(ContractCreateDto dto)
        {
            var validStart = dto.StartDate > new DateTime(1753, 1, 1) ? dto.StartDate : DateTime.Now;
            var validEnd = dto.EndDate > new DateTime(1753, 1, 1) ? dto.EndDate : DateTime.Now.AddMonths(6);

            var contract = new Contract
            {
                ClientId = dto.ClientId,
                StartDate = validStart,
                EndDate = validEnd,
                Status = ContractStatus.Draft,
                ServiceLevel = dto.ServiceLevel,
                SignedAgreementFilePath = "/uploads/agreements/" + dto.SignedAgreementFileName
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return contract;
        }

        public async Task<Contract?> UpdateContractStatusAsync(int id, string status)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return null;

            if (Enum.TryParse<ContractStatus>(status, true, out var parsedStatus))
            {
                contract.Status = parsedStatus;
                _context.Entry(contract).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return contract;
            }

            return null;
        }

        public async Task<ServiceRequest?> CreateServiceRequestAsync(ServiceRequestCreateDto dto)
        {
            var contract = await _context.Contracts.FindAsync(dto.ContractId);
            if (contract == null) return null;

            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                throw new InvalidOperationException("Cannot create service requests for Expired or On Hold contracts.");
            }

            var request = new ServiceRequest
            {
                ContractId = dto.ContractId,
                Description = dto.Description,
                Cost = dto.Cost,
                Status = "Pending"
            };

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }
}
