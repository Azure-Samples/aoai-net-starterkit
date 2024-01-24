using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.Core;
using backend.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.SemanticKernel.Text;
using SharpToken;

namespace backend.Services;

public class SKService
{
    const int Chunk_size = 1024;
    private readonly IKernel kernel;
    private readonly ILogger<SKService> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly HttpClient client;

    public SKService(IKernel kernel, ILogger<SKService> logger, ILoggerFactory loggerFactory, HttpClient client)
    {
        this.kernel = kernel;
        this.logger = logger;
        this.loggerFactory = loggerFactory;
        this.client = client;
    }

    static List<string> ChunkText(string content, int chunk_size)
    {
        var lines = TextChunker.SplitPlainTextLines(content, chunk_size / 2);
        // return paragraphs
        return TextChunker.SplitPlainTextParagraphs(lines, chunk_size, chunk_size / 4);
    }

    public async Task<Tuple<int, Exception?>> IngestAsync(IngestRequest? ingestion)
    {
        if (ingestion is null || string.IsNullOrEmpty(ingestion.collection) || ingestion.urls.Count == 0)
        {
            return new Tuple<int, Exception?>(0, new ArgumentException("Missing required fields. Must include collection and urls."));
        }
        var count = 0;
        foreach (var url in ingestion.urls)
        {
            try
            {
                // Get the file name from the URL
                var fileName = Path.GetFileName(url);

                // Download the contents
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Error downloading {url}: {response.StatusCode}");
                    continue;
                }

                // Chunk in paragraphs
                var text = await response.Content.ReadAsStringAsync();
                var paragraphs = ChunkText(text, Chunk_size);

                // Save each paragraph as a memory
                var totalParagraphs = paragraphs.Count();
                var currentParagraph = 1;
                foreach (var paragraph in paragraphs)
                {
                    var memory = new Memory(ingestion.collection, $"{fileName}-{totalParagraphs}-{currentParagraph}", paragraph, url);
                    var (result, err) = await SaveMemoryAsync(memory);
                    if (err is not null)
                    {
                        logger.LogError(err, $"Error saving memory for {url}");
                    }
                    currentParagraph++;
                }
                count++;
            }
            finally { }
        }
        return new Tuple<int, Exception?>(count, null);
    }

    public async Task<Tuple<IList<string>?, Exception?>> GetCollections()
    {
        try
        {
            var collections = await kernel.Memory.GetCollectionsAsync();
            return new Tuple<IList<string>?, Exception?>(collections, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting collections");
            return new Tuple<IList<string>?, Exception?>(null, ex);
        }
    }

    public async Task<Tuple<MemoryQueryResult?, Exception?>> GetMemoryAsync(string collection, string key)
    {
        try
        {
            var result = await kernel.Memory.GetAsync(collection, key);
            return new Tuple<MemoryQueryResult?, Exception?>(result, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving memory");
            return new Tuple<MemoryQueryResult?, Exception?>(null, ex);
        }
    }

    public async Task<Tuple<string?, Exception?>> SaveMemoryAsync(Memory memory)
    {
        if (string.IsNullOrEmpty(memory.key) || string.IsNullOrEmpty(memory.collection) || string.IsNullOrEmpty(memory.text))
        {
            return new Tuple<string?, Exception?>(null, new ArgumentException("Missing required fields. Must include text, key, and collection."));
        }
        try
        {
            //var skMemory = await memorySkill.RetrieveAsync(memory.collection, memory.key, loggerFactory: loggerFactory);
            var result = await kernel.Memory.GetAsync(memory.collection, memory.key);
            if (result is not null)
            {
                await kernel.Memory.RemoveAsync(memory.collection, memory.key);
            }
            return new Tuple<string?, Exception?>(await kernel.Memory.SaveInformationAsync(memory.collection, memory.text, memory.key, memory.description, memory.additionalMetadata), null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving memory");
            return new Tuple<string?, Exception?>(null, ex);
        }
    }

    public async Task<Exception?> DeleteMemoryAsync(string collection, string key)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(collection))
        {
            return new ArgumentException("Missing required fields. Must include collection and key.");
        }
        try
        {
            await kernel.Memory.RemoveAsync(collection, key);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting memory");
            return ex;
        }
    }

    public async Task<Tuple<Completion?, Exception?>> QueryAsync(Query query)
    {
        if (string.IsNullOrEmpty(query.query) || string.IsNullOrEmpty(query.collection) || query.maxTokens == 0 || query.limit < 0 || query.minRelevanceScore < 0 || query.temperature < 0)
        {
            return new Tuple<Completion?, Exception?>(null, new ArgumentException("Missing required fields. Must include query and collection, and limit, minimum relevance score, and temperature must be greater than 0."));
        }

        try
        {
            IAsyncEnumerable<MemoryQueryResult> queryResults =
                        kernel.Memory.SearchAsync(query.collection, query.query, limit: query.limit, minRelevanceScore: query.minRelevanceScore);

            StringBuilder promptData = new();

            var citations = new List<Citation>();
            await foreach (MemoryQueryResult r in queryResults)
            {
                promptData.Append(r.Metadata.Text + "\n\n");
                var parts = r.Metadata.Id.Split("-");
                var fileName = Path.GetFileName(parts[0]);
                if (!citations.Any(c => c.collection == query.collection && c.fileName == fileName))
                {
                    // By convention, this app will use the description field to store the URL
                    citations.Add(new Citation(query.collection, fileName, r.Metadata.Description, r.Metadata.AdditionalMetadata));
                }
            }
            if (citations.Count == 0)
                return new Tuple<Completion?, Exception?>(null, new Exception("No citations found"));

            var augmentedText = promptData.ToString();

            const string ragFunctionDefinition = "{{$input}}\n\nText:\n\"\"\"{{$data}}\n\"\"\"Use only the provided text.";
            var ragFunction = kernel.CreateSemanticFunction(ragFunctionDefinition, maxTokens: query.maxTokens);
            var result = await kernel.RunAsync(ragFunction, new(query.query)
            {
                ["data"] = augmentedText
            });

            var completion = new Completion(query.query, query.collection, result.ToString(), null, citations);
            return new Tuple<Completion?, Exception?>(completion, null);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying");
            return new Tuple<Completion?, Exception?>(null, ex);
        }
    }

    public Tuple<int, List<int>> Tokenize(string text)
    {
        var encoding = GptEncoding.GetEncoding("cl100k_base");
        var tokens = encoding.Encode(text);
        return new Tuple<int, List<int>>(tokens.Count, tokens);
    }
}
