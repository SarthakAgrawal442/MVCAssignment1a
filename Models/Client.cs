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
        public int Income { get; set; }
        public virtual ICollection<Service> Services { get; set; }

    }
}
