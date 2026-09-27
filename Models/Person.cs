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
        public string? DateOfBirth { get; set; } //encrypted string
        public byte[]? Photo { get; set; }
    }
}

