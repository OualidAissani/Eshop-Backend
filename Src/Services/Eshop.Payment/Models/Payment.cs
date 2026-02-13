using Eshop.Payement.Data;

namespace Eshop.Payement.Models
{
    public class Payment
    {
        public Guid Id { get; set; }
        public string CaptureId { get; set; }
        public string OrderId { get; set; }
        public Status Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime CapturedAt { get; set; }


        public string UserId { get; set; }
    }
}
