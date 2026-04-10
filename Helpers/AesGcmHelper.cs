using System.Security.Cryptography;
using System.Text;

namespace Rapsodia.Helpers;

internal static class AesGcmHelper 
{
    internal static string Encrypt(string plaintext, byte[] keyBytes)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        var nonce = new byte[System.Security.Cryptography.AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[System.Security.Cryptography.AesGcm.TagByteSizes.MaxSize];
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new System.Security.Cryptography.AesGcm(keyBytes, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    internal static string Decrypt(string cipherBase64, byte[] keyBytes)
    {
        if (string.IsNullOrEmpty(cipherBase64)) return cipherBase64;

        var combined = Convert.FromBase64String(cipherBase64);
        int nonceSize = 12;
        int tagSize = 16;

        var nonce = combined[..nonceSize];
        var ciphertext = combined[nonceSize..^tagSize];
        var tag = combined[^tagSize..];
        var plaintextBytes = new byte[ciphertext.Length];

        using var aes = new System.Security.Cryptography.AesGcm(keyBytes, tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}