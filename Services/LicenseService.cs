using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MVCSampleApp.Services
{
    public class LicenseService
    {
        public record LicenseCertificate(string AppName, DateTime ExpiryDate);

        private readonly RSA _rsa;
        private readonly string _certPath;

        public LicenseService(IConfiguration config)
        {
            var pubKeyPath = config["License:PublicKeyPath"]
                ?? throw new InvalidOperationException(
                    "License:PublicKeyPath not set in appsettings.");

            _certPath = config["License:CertificatePath"]
                ?? throw new InvalidOperationException(
                    "License:CertificatePath not set in appsettings.");

            var pem = File.ReadAllText(pubKeyPath);
            _rsa = RSA.Create();
            _rsa.ImportFromPem(pem);
        }

        /// <summary>
        /// Verifies the license certificate on disk.
        /// Throws InvalidOperationException if the license is missing,
        /// tampered with, or expired.
        /// </summary>
        public void Verify()
        {
            if (!File.Exists(_certPath))
                throw new InvalidOperationException(
                    "License certificate not found. App cannot start.");

            var raw      = File.ReadAllText(_certPath);
            var parts    = raw.Split('\n', 2);

            if (parts.Length != 2)
                throw new InvalidOperationException(
                    "License certificate format is invalid.");

            var payloadJson = parts[0].Trim();
            var signature   = parts[1].Trim();

            // Verify RSA-SHA256 signature
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var sigBytes     = Convert.FromBase64String(signature);

            bool valid = _rsa.VerifyData(
                payloadBytes,
                sigBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!valid)
                throw new InvalidOperationException(
                    "License certificate signature is invalid or tampered.");

            // Deserialize and check expiry
            var cert = JsonSerializer.Deserialize<LicenseCertificate>(payloadJson)
                ?? throw new InvalidOperationException("Could not parse license.");

            if (DateTime.UtcNow > cert.ExpiryDate)
                throw new InvalidOperationException(
                    $"License expired on {cert.ExpiryDate:yyyy-MM-dd}. App cannot start.");

            Console.WriteLine(
                $"[License] Valid. App: {cert.AppName}, Expires: {cert.ExpiryDate:yyyy-MM-dd}");
        }
    }
}