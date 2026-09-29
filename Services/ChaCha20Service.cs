using System;
using System.Security.Cryptography;

namespace MVCSampleApp.Services
{
    /// <summary>
    /// From-scratch ChaCha20 stream cipher (RFC 8439).
    /// Key: 32 bytes. Nonce: 12 bytes. Counter: 32-bit starting at 1.
    /// </summary>
    public class ChaCha20Service
    {
        private readonly byte[] _key;

        public ChaCha20Service(byte[] key)
        {
            if (key.Length != 32)
                throw new ArgumentException("ChaCha20 key must be 32 bytes.");
            _key = key;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Encrypt plaintext. Returns [12-byte nonce || ciphertext].</summary>
        public byte[] Encrypt(byte[] plaintext)
        {
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var keystream = GenerateKeystream(nonce, plaintext.Length);
            var ciphertext = new byte[plaintext.Length];
            for (int i = 0; i < plaintext.Length; i++)
                ciphertext[i] = (byte)(plaintext[i] ^ keystream[i]);

            var result = new byte[12 + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(ciphertext, 0, result, 12, ciphertext.Length);
            return result;
        }

        /// <summary>Decrypt blob produced by Encrypt (reads nonce from first 12 bytes).</summary>
        public byte[] Decrypt(byte[] blob)
        {
            if (blob.Length < 12)
                throw new ArgumentException("Blob too short to contain nonce.");

            var nonce = new byte[12];
            Buffer.BlockCopy(blob, 0, nonce, 0, 12);

            int ciphertextLen = blob.Length - 12;
            var ciphertext = new byte[ciphertextLen];
            Buffer.BlockCopy(blob, 12, ciphertext, 0, ciphertextLen);

            var keystream = GenerateKeystream(nonce, ciphertextLen);
            var plaintext = new byte[ciphertextLen];
            for (int i = 0; i < ciphertextLen; i++)
                plaintext[i] = (byte)(ciphertext[i] ^ keystream[i]);

            return plaintext;
        }

        // ── ChaCha20 internals (RFC 8439) ─────────────────────────────────

        private byte[] GenerateKeystream(byte[] nonce, int length)
        {
            var keystream = new byte[length];
            int offset = 0;
            uint counter = 1;

            while (offset < length)
            {
                var block = ChaCha20Block(nonce, counter++);
                int copy = Math.Min(64, length - offset);
                Buffer.BlockCopy(block, 0, keystream, offset, copy);
                offset += copy;
            }
            return keystream;
        }

        private byte[] ChaCha20Block(byte[] nonce, uint counter)
        {
            // Build initial state (16 x uint32)
            uint[] state = new uint[16];

            // Constants "expa", "nd 3", "2-by", "te k"
            state[0]  = 0x61707865;
            state[1]  = 0x3320646e;
            state[2]  = 0x79622d32;
            state[3]  = 0x6b206574;

            // Key (8 x uint32)
            for (int i = 0; i < 8; i++)
                state[4 + i] = ToUInt32LE(_key, i * 4);

            // Counter
            state[12] = counter;

            // Nonce (3 x uint32)
            state[13] = ToUInt32LE(nonce, 0);
            state[14] = ToUInt32LE(nonce, 4);
            state[15] = ToUInt32LE(nonce, 8);

            uint[] working = (uint[])state.Clone();

            // 20 rounds (10 double-rounds)
            for (int i = 0; i < 10; i++)
            {
                // Column rounds
                QuarterRound(ref working[0],  ref working[4],  ref working[8],  ref working[12]);
                QuarterRound(ref working[1],  ref working[5],  ref working[9],  ref working[13]);
                QuarterRound(ref working[2],  ref working[6],  ref working[10], ref working[14]);
                QuarterRound(ref working[3],  ref working[7],  ref working[11], ref working[15]);
                // Diagonal rounds
                QuarterRound(ref working[0],  ref working[5],  ref working[10], ref working[15]);
                QuarterRound(ref working[1],  ref working[6],  ref working[11], ref working[12]);
                QuarterRound(ref working[2],  ref working[7],  ref working[8],  ref working[13]);
                QuarterRound(ref working[3],  ref working[4],  ref working[9],  ref working[14]);
            }

            // Add original state
            for (int i = 0; i < 16; i++)
                working[i] += state[i];

            // Serialize to bytes (little-endian)
            var block = new byte[64];
            for (int i = 0; i < 16; i++)
                FromUInt32LE(working[i], block, i * 4);

            return block;
        }

        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            a += b; d ^= a; d = RotL(d, 16);
            c += d; b ^= c; b = RotL(b, 12);
            a += b; d ^= a; d = RotL(d,  8);
            c += d; b ^= c; b = RotL(b,  7);
        }

        private static uint RotL(uint x, int n) => (x << n) | (x >> (32 - n));

        private static uint ToUInt32LE(byte[] b, int i) =>
            (uint)(b[i] | b[i+1] << 8 | b[i+2] << 16 | b[i+3] << 24);

        private static void FromUInt32LE(uint v, byte[] b, int i)
        {
            b[i]   = (byte)(v);
            b[i+1] = (byte)(v >> 8);
            b[i+2] = (byte)(v >> 16);
            b[i+3] = (byte)(v >> 24);
        }
    }
}