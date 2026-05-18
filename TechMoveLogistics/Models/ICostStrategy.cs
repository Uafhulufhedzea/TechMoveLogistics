namespace TechMoveLogistics.Models
{
    public interface ICostStrategy
    {
        decimal CalculateFinalCost(decimal baseUsdAmount, decimal exchangeRate);
    }
}

