using Microsoft.AspNetCore.DataProtection;

namespace Server.Common;

public class ConfigEncryptionHelper(IDataProtectionProvider dataProtectionProvider)
{
    private const string ProtectorPurpose = "PRM.SystemConfig.LlmApiKey";
    public const string EncryptedPrefix = "enc:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Encrypt(string plainText) => EncryptedPrefix + _protector.Protect(plainText);

    public string Decrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            return _protector.Unprotect(value[EncryptedPrefix.Length..]);

        return _protector.Unprotect(value);
    }

    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            return true;

        try
        {
            _protector.Unprotect(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
