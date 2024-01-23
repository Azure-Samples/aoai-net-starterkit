namespace PCheck.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

//Response Models
public class PolicyCheckResult 
{
    
    [JsonPropertyName("CalculatedDistance")]
    public float CalculatedDistance {get; set;} = 0.0f;

    [JsonPropertyName("AvgOptimalDistance")]
    public float AvgOptimalDistance {get;set;} = 0.0f;

    [JsonPropertyName("AvgOffTopicDistance")]
    public float AvgOffTopicDistance {get; set;} = 0.0f;

    [JsonPropertyName("DeviationOptimalDistance")]
    public float DeviationOptimalDistance {get; set;} = 0.0f;

    [JsonPropertyName("DeviationOffTopicDistance")]
    public float DeviationOffTopicDistance {get; set;} = 0.0f;
}


public class PolicyRegisterResult{
    [JsonPropertyName("PolicyUri")]
    public string[] PolicyUris {get; set;} = new string[0];
}

//Request Models
public class PolicyRegisterRequest
{
    [JsonPropertyName("PolicyId")]
    public string PolicyId {get; set;} = string.Empty;
    
    [JsonPropertyName("ContentToLookFor")]
    public string ContentToLookFor {get; set;} = string.Empty;
    
    [JsonPropertyName("PotentialPhrases")] 
    public string[] PotentialPhrases {get; set;} = new string[0]; 
    
    [JsonPropertyName("OffTopicPhrases")]
    public string[] OffTopicPhrases {get; set;} = new string[0];    
}

public class CheckRequest 
{
    [JsonPropertyName("PolicyId")]
    public string PolicyId {get; set;} = string.Empty;

    [JsonPropertyName("Content")]
    public string Content {get; set;} = string.Empty;
}

