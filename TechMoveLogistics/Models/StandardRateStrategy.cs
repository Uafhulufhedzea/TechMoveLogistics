namespace TechMoveLogistics.Models
{
   
    public class StandardRateStrategy : ICostStrategy
    {
        public decimal CalculateFinalCost(decimal baseUsdAmount, decimal exchangeRate)
        {
            return baseUsdAmount * exchangeRate; // Enforces standard conversion math
        }
    }
}

