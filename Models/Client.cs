using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCSampleApp.Models
{
    [Table("Clients")]
    public class Client : Person
    {
        public Client() {
            Services = new HashSet<Service>();
        }
  
        [Required] [Range(0,1000)]
        public string Income { get; set; } // encrypted string
        public virtual ICollection<Service> Services { get; set; }
        public string? PhotoVaultName { get; set; }
    }
}