using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;

namespace Infrastructure.Services.Security;

/// <summary>
/// A-4 — AES-256-CBC Encryption service.
/// Key is derived from "CRM_ENCRYPTION_KEY" environment variable.
/// Prepends IV to cipher text for self-contained decryption.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService()
    {
        var rawKey = Environment.GetEnvironmentVariable("CRM_ENCRYPTION_KEY");
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            // Development fallback, hashed to protect integrity
            rawKey = "SafaCrmDevelopmentDefaultSecretKey123!@#";
        }
        
        // SHA256 ensures we always get a valid 32-byte key
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        
        // Write the IV first to the stream so it's prepended
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipherTextBytes = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherTextBytes, 0, cipherTextBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        using var ms = new MemoryStream(cipherTextBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        return sr.ReadToEnd();
    }
}
