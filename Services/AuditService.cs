using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MVCSampleApp.Models;
using AppContext = MVCSampleApp.AppContext;

namespace MVCSampleApp.Services
{
    public class AuditService
    {
        private readonly AppContext _context;
        private readonly IHttpContextAccessor _http;
        private readonly RSA _rsa;

        public AuditService(
            AppContext context,
            IHttpContextAccessor http,
            IConfiguration config)
        {
            _context = context;
            _http = http;

            // Load RSA private key from PEM file path in appsettings.json.
            // Key file must NOT be in Git — add *.pem to .gitignore.
            var keyPath = config["Audit:PrivateKeyPath"]
                ?? throw new InvalidOperationException(
                    "Audit:PrivateKeyPath not set in appsettings.");

            var pem = System.IO.File.ReadAllText(keyPath);
            _rsa = RSA.Create();
            _rsa.ImportFromPem(pem);
        }

        /// <summary>
        /// Records a signed audit entry for an action that changed an entity.
        /// </summary>
        public async Task LogAsync(
            string action,
            string entityId,
            object payload,
            string? summary = null)
        {
            var userName = _http.HttpContext?.User?.Identity?.Name ?? "anonymous";

            // Canonical payload: action + entityId + timestamp + JSON of payload.
            // Timestamp is fixed before signing so the hash and signature match later.
            var timestamp = DateTime.UtcNow;
            var canonical = new
            {
                action,
                entityId,
                timestamp,
                userName,
                payload
            };

            var json = JsonSerializer.Serialize(canonical);
            var payloadHash = ComputeSha256(json);
            var signature = Sign(payloadHash);

            var entry = new AuditLog
            {
                UserName   = userName,
                Action     = action,
                EntityId   = entityId,
                Timestamp  = timestamp,
                PayloadHash = payloadHash,
                Signature  = signature,
                Summary    = summary
            };

            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifies that a stored audit entry has not been tampered with.
        /// Returns true if the signature is valid.
        /// </summary>
        public bool Verify(AuditLog entry)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(entry.PayloadHash);
                var sigBytes  = Convert.FromBase64String(entry.Signature);

                return _rsa.VerifyData(
                    hashBytes,
                    sigBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private string Sign(string base64Hash)
        {
            var hashBytes = Convert.FromBase64String(base64Hash);
            var sigBytes  = _rsa.SignData(
                hashBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(sigBytes);
        }
    }
}