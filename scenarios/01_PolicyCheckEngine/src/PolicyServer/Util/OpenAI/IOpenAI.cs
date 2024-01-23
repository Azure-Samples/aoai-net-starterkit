namespace SKit.Scenario.PolicyCheck.Util;
public interface IOpenAI
{
    Task<(bool success, float[] vector)> CreateEmbedding(string content);
 
}
