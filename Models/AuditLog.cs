using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCSampleApp.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        // Who performed the action (ASP.NET Identity username/email)
        [Required]
        public string UserName { get; set; } = string.Empty;

        // e.g. "Client.Create", "Client.Edit", "Client.Delete"
        [Required]
        public string Action { get; set; } = string.Empty;

        // Primary key of the affected record as a string
        [Required]
        public string EntityId { get; set; } = string.Empty;

        // UTC timestamp of when the action occurred
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // SHA-256 hash of the canonical payload (proves what data was recorded)
        [Required]
        public string PayloadHash { get; set; } = string.Empty;

        // RSA-SHA256 signature over PayloadHash (proves server recorded it)
        [Required]
        public string Signature { get; set; } = string.Empty;

        // Human-readable summary stored alongside the hash (not trusted alone)
        public string? Summary { get; set; }
    }
}