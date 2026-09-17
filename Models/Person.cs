using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;

namespace MVCSampleApp.Models
{
    [Table("People")]
    public class Person
    {
        public Guid ID { get; set; }
        public FullName? Name { get; set; }
        public FullAddress? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public byte[]? Photo { get; set; }
    }
}
