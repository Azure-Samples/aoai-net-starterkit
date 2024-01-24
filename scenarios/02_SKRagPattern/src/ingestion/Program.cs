using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel.Text;

namespace ingestion;
class Program
{
    private const string Source_Folder = "../../data/docs/";
    private const string Endpoint = "https://alemoskcapp.azurewebsites.net/api/gpt/v1/memory";//"http://localhost:5087/api/gpt/v1/memory";

    private const string RootURL = "https://alemoraoaist.z13.web.core.windows.net/docs/";
    private const int Chunk_size = 512;
    private const string HR_Subfolder = "HR";
    private const string Engineering_Subfolder = "Engineering";
    static HttpClient client = new();

    static List<string> ChunkText(string content, int chunk_size)
    {
        var lines = TextChunker.SplitPlainTextLines(content, chunk_size / 2);
        // return paragraphs
        return TextChunker.SplitPlainTextParagraphs(lines, chunk_size);
    }

    static void Main(string[] args)
    {
        var fileParagraphs = ExtractParagraphsFromFilesInFolderAsync(Path.Combine(Source_Folder, HR_Subfolder)).GetAwaiter().GetResult();
        ProcessParagraphs(fileParagraphs, HR_Subfolder);

        fileParagraphs = ExtractParagraphsFromFilesInFolderAsync(Path.Combine(Source_Folder, Engineering_Subfolder)).GetAwaiter().GetResult();
        ProcessParagraphs(fileParagraphs, Engineering_Subfolder);
    }

    static void ProcessParagraphs(List<Tuple<string, string>> fileParagraphs, string collection)
    {
        foreach (var fileParagraph in fileParagraphs)
        {
            int count = 1;
            var chunks = ChunkText(fileParagraph.Item2, Chunk_size);
            foreach (var chunk in chunks)
            {
                var payload = new
                {
                    collection,
                    key = $"{fileParagraph.Item1}-{chunks.Count}-{count}",
                    chunk,
                    description = $"{RootURL}{collection}/{fileParagraph.Item1}",
                    additionalMetadata = $"{RootURL}{collection}/{fileParagraph.Item1}"
                };
                var jsonContent = JsonSerializer.Serialize(payload);
                var resp = client.PostAsync(new Uri(Endpoint), new StringContent(jsonContent, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Added {fileParagraph.Item1}-{chunks.Count}-{count}");
                }
                else
                {
                    Console.WriteLine($"Failed to add {fileParagraph.Item1}-{chunks.Count}-{count}");
                }
                count++;
            }
        }
    }

    static Task<Tuple<string, string>> GetFileContent(string filePath)
    {
        return Task.Run(() =>
        {
            var fileName = Path.GetFileName(filePath);
            var fileContent = File.ReadAllText(filePath);
            return new Tuple<string, string>(fileName, fileContent);
        });
    }

    static async Task<List<Tuple<string, string>>> ExtractParagraphsFromFilesInFolderAsync(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.txt");
        var fileContent = new List<Tuple<string, string>>();

        // Read all files in parallel
        var tasks = new List<Task<Tuple<string, string>>>();
        foreach (var file in files)
        {
            tasks.Add(GetFileContent(file));
        }
        await Task.WhenAll(tasks);

        // Add file name and content to list
        foreach (var task in tasks)
        {
            fileContent.Add(new Tuple<string, string>(task.Result.Item1, task.Result.Item2));
        }

        return fileContent;
    }
}
