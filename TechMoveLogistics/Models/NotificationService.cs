using System.Diagnostics;

namespace TechMoveLogistics.Models
{
    //Observer Implementation
    public class NotificationService : IObserver
    {
        public void OnContractActivity(string message)
        {
            Debug.WriteLine($"[Compliance Notification Observer]: {message}");
        }
    }
}
