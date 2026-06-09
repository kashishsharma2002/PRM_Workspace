using Microsoft.AspNetCore.DataProtection;

namespace Server.Common;

public class ConfigEncryptionHelper(IDataProtectionProvider dataProtectionProvider)
{
    private const string ProtectorPurpose = "PRM.SystemConfig.LlmApiKey";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string protectedText) => _protector.Unprotect(protectedText);

    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

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
