namespace Server.AI;

public interface ILlmClientFactory
{
    ILlmClient CreateClient(string providerKey);
}
