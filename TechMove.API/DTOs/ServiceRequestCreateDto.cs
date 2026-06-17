namespace TechMove.API.DTOs
{
    public class ServiceRequestCreateDto
    {
        public int ContractId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }
}
