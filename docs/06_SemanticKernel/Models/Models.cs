public record Chunk(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("text")] string Text);

public class ChunkInfo
{
    public int Index { get; set; }
    public string? Content { get; set; }
    public string? Summary { get; set; }
    public int Words { get; set; }
    public int Characters { get; set; }
    public int Tokens { get; set; }
}
