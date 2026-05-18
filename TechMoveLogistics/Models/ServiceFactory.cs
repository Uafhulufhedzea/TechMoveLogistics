namespace TechMoveLogistics.Models
{
    public static class ServiceFactory
    {
        public static ICostStrategy GetCostStrategy(string region)
        {
            // Decoupled object creation
            return new StandardRateStrategy();
        }
    }
}
