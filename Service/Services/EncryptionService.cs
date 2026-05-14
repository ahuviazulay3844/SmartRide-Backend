using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

public class EncryptionService
{
    private readonly string _key; // מפתח סודי באורך 32 תווים

    public EncryptionService(IConfiguration configuration)
    {
        // נשלוף את המפתח מהגדרות האפליקציה
        _key = configuration["EncryptionKey"] ?? "12345678901234567890123456789012";
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = new byte[16]; // וקטור אתחול קבוע לצורך הפשטות (במערכות רגישות מאוד משתמשים ב-IV דינמי)

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch { return "Error Decrypting"; }
    }
}