namespace TechMoveLogistics.Models
{
    public interface IObserver
    {
        void OnContractActivity(string message);
    }
}

