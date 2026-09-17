using System.ComponentModel.DataAnnotations.Schema;
namespace MVCSampleApp.Models
{
    [Table("FullAddresses")]
    public class FullAddress
    {
        public int ID { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }

    }
}
