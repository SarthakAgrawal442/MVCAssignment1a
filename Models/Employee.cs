using System.ComponentModel.DataAnnotations.Schema;

namespace MVCSampleApp.Models
{
    [Table("Employees")]
    public class Employee : Person
    {
        public Service? MyService { get; set; }
       
        public int? ServiceId { get; set; }
        public decimal Salary { get; set; }
        public string Roles { get; set; }   
    }
}
