namespace Server.AI;

public interface ILlmClientResolver
{
    ILlmClient Resolve(string provider);
}
