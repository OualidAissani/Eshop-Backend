using System.Text.Json.Serialization;

namespace Eshop.Payment.Models
{
    public class ItemsDto
    {
        public string name { get; set; }

        public int quantity { get; set; }

        public string  description { get; set; }

        public AmountDto unit_amount { get; set; }
    }
}
