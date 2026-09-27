using System.Security.Cryptography;
using System.Text;

namespace MVCSampleApp.Services
{
    public class FieldEncryptionService
    {
        private readonly byte[] _key;

        public FieldEncryptionService(IConfiguration config)
        {
            var keyStr = config["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption:Key not configured.");
            _key = Convert.FromBase64String(keyStr);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key must be 32 bytes (256-bit) base64.");
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
            RandomNumberGenerator.Fill(nonce);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            // Store as: base64(nonce) + "." + base64(tag) + "." + base64(ciphertext)
            return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(ciphertext)}";
        }

        public string Decrypt(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return stored;
            var parts = stored.Split('.');
            if (parts.Length != 3) return stored; // not encrypted, return as-is
            var nonce = Convert.FromBase64String(parts[0]);
            var tag = Convert.FromBase64String(parts[1]);
            var ciphertext = Convert.FromBase64String(parts[2]);
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }

        public string EncryptInt(int value) => Encrypt(value.ToString());
        public int DecryptInt(string stored) => int.Parse(Decrypt(stored));

        public string EncryptDate(DateTime? value) =>
            value == null ? "" : Encrypt(value.Value.ToString("o"));
        public DateTime? DecryptDate(string stored) =>
            string.IsNullOrEmpty(stored) ? null : DateTime.Parse(Decrypt(stored));
    }
}