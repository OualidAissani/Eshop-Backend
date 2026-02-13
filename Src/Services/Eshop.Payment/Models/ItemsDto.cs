using System.Text.Json.Serialization;

namespace Eshop.Payement.Models
{
    public class ItemsDto
    {
        public string name { get; set; }

        public string quantity { get; set; }

        public string  description { get; set; }

        public AmountDto unit_amount { get; set; }
    }
}
