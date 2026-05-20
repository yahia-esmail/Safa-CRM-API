namespace Application.Common.Interfaces;

/// <summary>
/// A-4 — Encryption/Decryption service for sensitive data like SMTP passwords.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
