using Eshop.Payment.Data;

namespace Eshop.Payment.Models
{
    public class Webhook
    {
        public int id { get; set; }
        public required string event_type { get; set; }
        public required string eventId { get; set; }
        public required string payload { get; set; }

        public Status status { get; set; } = Status.Pending;
    }
}
