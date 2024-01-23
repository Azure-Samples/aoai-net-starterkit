namespace PCheck.Util;
public interface IOpenAI
{
    Task<(bool success, float[] vector)> CreateEmbedding(string content);
 
}
