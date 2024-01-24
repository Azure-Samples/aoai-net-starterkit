namespace backend.Models;
public record Memory(string collection, string key, string? text, string? description = null, string? additionalMetadata = null);
public record Query(string collection, string query, int maxTokens = 1000, double temperature = 0.3, int limit = 3, double minRelevanceScore = 0.77);
public record Completion(string query, string collection, string text, object? usage, List<Citation>? citations = null);
public record Citation(string collection, string fileName, string? description = null, string? additionalMetadata = null);
public record IngestRequest(string collection, List<string> urls);
public record TokenizeRequest(string text);
public record TokenizeResponse(int count, List<int> tokens);