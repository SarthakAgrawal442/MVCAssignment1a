using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace MVCSampleApp.Models
{
    [Table("FullNames")]
    public class FullName
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }
}

//[Index(nameof(FirstName), nameof(LastName), IsUnique = true)]