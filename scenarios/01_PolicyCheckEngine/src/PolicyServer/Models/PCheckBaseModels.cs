namespace PCheck.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using static PCheck.Util.PolicyRepository;

public class Policy : EntityBase
{
    public string PolicyId { get; set; } = string.Empty;
    public float[] ContentToLookFor {get; set;} = new float[1536];
    public float AvgOptimalDistance {get;set;} = 0.001f;
    public float AvgOffTopicDistance {get; set;} = 0.001f;

}


//Response Models
public class PCheckResponse<T> : PCheckResponseBase where T : new()
{
    [JsonPropertyName("Result")]
    public T Result { get; set; } = new T(); 
}

public class PCheckResponseBase {
    [JsonPropertyName("Status")]
    public PCheckResponseStatus Status { get; set; } = PCheckResponseStatus.Success;
    [JsonPropertyName("Error")]
    public IList<string> Error { get; set; } = new List<string>();
}

public enum PCheckResponseStatus {
    [JsonPropertyName("Success")]
    Success = 0,
    [JsonPropertyName("Failure")]
    Failure = 1
}

//Request Models
public class PCheckRequest<T> where T : new()
{
    [JsonPropertyName("Request")]
    public T RequestData { get; set; } = new T();
}


