using System;
using System.Security.Cryptography;
using System.Text;

namespace CS2_Echo.Infrastructure.Security;

public static class DpapiHelper
{
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DPAPI] Encryption failed: {ex.Message}");
            return string.Empty;
        }
    }

    public static bool TryDecrypt(string encryptedText, out string plainText)
    {
        plainText = string.Empty;
        if (string.IsNullOrEmpty(encryptedText)) return false;

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            plainText = Encoding.UTF8.GetString(decryptedBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Decrypt(string encryptedText)
    {
        if (TryDecrypt(encryptedText, out string plainText))
        {
            return plainText;
        }

        return string.Empty;
    }
}

