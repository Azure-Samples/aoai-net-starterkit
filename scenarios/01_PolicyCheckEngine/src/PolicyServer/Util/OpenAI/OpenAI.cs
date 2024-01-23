using Azure;
using Azure.AI.OpenAI;

namespace SKit.Scenario.PolicyCheck.Util;

public class OpenAI : IOpenAI {

    string _apiKey = "";
    string _endpoint = "";
    string _embeddingModelDeploymentName = "";
    
    AzureKeyCredential _azureKeyCredential;
    OpenAIClient _openAIClient;

    public OpenAI(string apiKey, string endpoint, string embeddingModelDeploymentName)
    {
        _apiKey = apiKey;
        _endpoint = endpoint;
        _embeddingModelDeploymentName = embeddingModelDeploymentName;

        _azureKeyCredential = new AzureKeyCredential(_apiKey);
        _openAIClient = new OpenAIClient(new Uri(_endpoint), _azureKeyCredential);  
    }    


    public async Task<(bool success, float[] vector)> CreateEmbedding(string content)
    {
        
        EmbeddingsOptions embeddingsOptions = new EmbeddingsOptions(content);

        try {
            var embedding = await _openAIClient.GetEmbeddingsAsync(_embeddingModelDeploymentName, embeddingsOptions);
            float[] vector = embedding.Value.Data[0].Embedding.ToArray<float>();
            return (true, vector);
        } catch (Exception exE) {
            Console.WriteLine(exE.Message);
            return (false, new float[0]); 
        }
    }

}
