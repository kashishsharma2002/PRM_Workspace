using Microsoft.AspNetCore.DataProtection;
using Server.Common;

namespace Tests;

public class ConfigEncryptionHelperTests
{
    private readonly ConfigEncryptionHelper _encryption = new(DataProtectionProvider.Create("Tests"));

    [Fact]
    public void Encrypt_PrefixesValueWithEnc()
    {
        var encrypted = _encryption.Encrypt("my-secret-key-123");

        Assert.StartsWith(ConfigEncryptionHelper.EncryptedPrefix, encrypted);
        Assert.NotEqual("my-secret-key-123", encrypted);
    }

    [Fact]
    public void Decrypt_RoundTripsPrefixedValue()
    {
        var encrypted = _encryption.Encrypt("AIzaSyExampleKey");

        var decrypted = _encryption.Decrypt(encrypted);

        Assert.Equal("AIzaSyExampleKey", decrypted);
    }

    [Fact]
    public void IsEncrypted_ReturnsTrue_ForPrefixedValue()
    {
        var encrypted = _encryption.Encrypt("test-key");

        Assert.True(_encryption.IsEncrypted(encrypted));
    }

    [Fact]
    public void IsEncrypted_ReturnsFalse_ForPlainTextKey()
    {
        Assert.False(_encryption.IsEncrypted("AIzaSyPlainTextKey"));
    }

    [Fact]
    public void Decrypt_RoundTripsLegacyUnprefixedValue()
    {
        var protector = DataProtectionProvider.Create("Tests")
            .CreateProtector("PRM.SystemConfig.LlmApiKey");
        var legacyBlob = protector.Protect("legacy-key");

        Assert.True(_encryption.IsEncrypted(legacyBlob));
        Assert.Equal("legacy-key", _encryption.Decrypt(legacyBlob));
    }
}
