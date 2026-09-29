using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MVCSampleApp.Services
{
    /// <summary>
    /// Stores files on disk encrypted with ChaCha20.
    /// File format on disk: [12-byte nonce][ciphertext][32-byte HMAC-SHA256]
    /// </summary>
    public class FileVaultService
    {
        private readonly ChaCha20Service _chacha;
        private readonly byte[] _hmacKey;
        private readonly string _vaultPath;

        public FileVaultService(IConfiguration config)
        {
            // appsettings.json: "Vault": { "Key": "<64-char hex>", "HmacKey": "<64-char hex>", "Path": "vault" }
            var section = config.GetSection("Vault");

            _vaultPath = Path.GetFullPath(section["Path"] ?? "vault");
            Directory.CreateDirectory(_vaultPath);

            _chacha  = new ChaCha20Service(ParseHex(section["Key"]!));
            _hmacKey = ParseHex(section["HmacKey"]!);
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Encrypt and save stream to vault. Returns the stored filename.</summary>
        public async Task<string> SaveAsync(Stream source, string originalFileName)
        {
            using var ms = new MemoryStream();
            await source.CopyToAsync(ms);
            byte[] plaintext = ms.ToArray();

            // ChaCha20 encrypt (prepends nonce internally)
            byte[] blob = _chacha.Encrypt(plaintext);

            // Append HMAC-SHA256 tag for integrity
            byte[] tag = ComputeHmac(blob);

            string storedName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}.vault";
            string path = Path.Combine(_vaultPath, storedName);

            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            await fs.WriteAsync(blob);
            await fs.WriteAsync(tag);

            return storedName;
        }

        /// <summary>Read, verify HMAC, decrypt and return plaintext stream.</summary>
        public async Task<Stream> LoadAsync(string storedName)
        {
            string path = Path.Combine(_vaultPath, storedName);
            if (!File.Exists(path))
                throw new FileNotFoundException("Vault file not found.", storedName);

            byte[] raw = await File.ReadAllBytesAsync(path);

            if (raw.Length < 12 + 32)
                throw new InvalidDataException("Vault file too short.");

            // Split: blob = raw[..^32], tag = raw[^32..]
            byte[] blob = raw[..^32];
            byte[] tag  = raw[^32..];

            // Verify HMAC before decrypting (authenticate then decrypt)
            byte[] expected = ComputeHmac(blob);
            if (!CryptographicOperations.FixedTimeEquals(tag, expected))
                throw new CryptographicException("HMAC verification failed — file may be tampered.");

            byte[] plaintext = _chacha.Decrypt(blob);
            return new MemoryStream(plaintext);
        }

        /// <summary>Delete a vault file.</summary>
        public void Delete(string storedName)
        {
            string path = Path.Combine(_vaultPath, storedName);
            if (File.Exists(path))
                File.Delete(path);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private byte[] ComputeHmac(byte[] data)
        {
            using var hmac = new HMACSHA256(_hmacKey);
            return hmac.ComputeHash(data);
        }

        private static byte[] ParseHex(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length.");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}