namespace Server.AI.Abstractions;

public interface ILlmClientFactory
{
    ILlmClient CreateClient(string providerKey);
}
