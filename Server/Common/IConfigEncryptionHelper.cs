namespace Server.Common;

public interface IConfigEncryptionHelper
{
    string Encrypt(string plainText);
    string Decrypt(string value);
    bool IsEncrypted(string value);
}
