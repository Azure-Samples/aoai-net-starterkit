namespace SKit.Scenario.PolicyCheck.Models;

using System.Text.Json.Serialization;
using static SKit.Scenario.PolicyCheck.Util.PolicyRepository;

public class PolicyRule : EntityBase
{
    public string PolicyId { get; set; } = string.Empty;
    public float[] ContentToLookFor {get; set;} = new float[1536];
    public float AvgOptimalDistance {get;set;} = 0.001f;
    public float AvgOffTopicDistance {get; set;} = 0.001f;

}


//Response Models
public class PolicyRuleCheckResponse<T> : PolicyRuleCheckResponseBase where T : new()
{
    [JsonPropertyName("Result")]
    public T Result { get; set; } = new T(); 
}

public class PolicyRuleCheckResponseBase {
    [JsonPropertyName("Status")]
    public PolicyRuleCheckResponseStatus Status { get; set; } = PolicyRuleCheckResponseStatus.Success;
    [JsonPropertyName("Error")]
    public IList<string> Error { get; set; } = new List<string>();
}

public enum PolicyRuleCheckResponseStatus {
    [JsonPropertyName("Success")]
    Success = 0,
    [JsonPropertyName("Failure")]
    Failure = 1
}

//Request Models
public class PolicyRuleCheckRequest<T> where T : new()
{
    [JsonPropertyName("Request")]
    public T RequestData { get; set; } = new T();
}


