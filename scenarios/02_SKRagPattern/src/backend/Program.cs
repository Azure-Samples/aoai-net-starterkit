using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.Sqlite;
using Microsoft.SemanticKernel.Skills.Core;
using backend.Models;
using backend.Services;
using SharpToken;

var builder = WebApplication.CreateBuilder(args);

// Read environment variables
var appSettings = new AppSettings();

// Configure Semantic Kernel
var sqliteStore = await SqliteMemoryStore.ConnectAsync(appSettings.DbPath);
IKernel kernel = new KernelBuilder()
    .WithAzureChatCompletionService(appSettings.GptDeploymentName, appSettings.Endpoint, appSettings.ApiKey)
    .WithAzureTextEmbeddingGenerationService(appSettings.AdaDeploymentName, appSettings.Endpoint, appSettings.ApiKey)
    .WithMemoryStorage(sqliteStore)
    .Build();

var memorySkill = new TextMemorySkill(kernel.Memory);
kernel.ImportSkill(memorySkill);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton<SKService>();

// Build the WebApplication
var app = builder.Build();

//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

var group = app.MapGroup("/api/gpt/v1");

// Routes
group.MapGet("/memory/{collection}/{key}", async (string collection, string key, SKService service) =>
{
    if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(key))
    {
        return Results.BadRequest(new { message = "Missing required fields. Must include collection, and key." });
    }
    var (result, err) = await service.GetMemoryAsync(collection, key);
    if (err is not null)
    {
        return Results.NotFound();
    }
    return Results.Ok(result);
    //return Results.Ok(new Memory(collection, key, result?.Metadata.Text, result?.Metadata.Description, result?.Metadata.AdditionalMetadata));
})
.WithName("GetMemory")
.WithOpenApi();

group.MapPost("/memory", async ([FromBody] Memory memory, SKService service) =>
{
    if (string.IsNullOrEmpty(memory.text) || string.IsNullOrEmpty(memory.key) || string.IsNullOrEmpty(memory.collection))
    {
        return Results.BadRequest(new { message = "Missing required fields. Must include text, key, and collection." });
    }
    var (result, err) = await service.SaveMemoryAsync(memory);
    if (err is not null)
    {
        return Results.BadRequest(new { message = "Error saving memory." });
    }
    return Results.Ok(memory);
})
.WithName("PostMemory")
.WithOpenApi();

group.MapPost("/ingest", async ([FromBody] IngestRequest? request, SKService service) =>
{
    if (request is null || request.urls.Count == 0 || string.IsNullOrEmpty(request.collection))
    {
        return Results.BadRequest(new { message = "Missing required fields. The ingestion request must include the collection name and list of one or more urls." });
    }
    var (ingestedCount, err) = await service.IngestAsync(request);
    if (err is not null)
    {
        return Results.BadRequest(new { message = $"Error ingesting. {err.Message}" });
    }
    return Results.Ok(new { ingestedCount });
})
.WithName("PostIngest")
.WithOpenApi();

group.MapGet("/collection", async (SKService service) =>
{
    var (collections, err) = await service.GetCollections();
    if (err is not null)
    {
        return Results.NotFound(new { message = "Error getting collections." });
    }
    return Results.Ok(collections);
})
.WithName("GetCollections")
.WithOpenApi();

group.MapDelete("/memory", async ([FromBody] Memory memory, SKService service) =>
{
    var ex = await service.DeleteMemoryAsync(memory.collection, memory.key);
    if (ex is not null)
    {
        return Results.NotFound(new { message = "Error deleting memory." });
    }
    return Results.Ok(memory);
})
.WithName("DeleteMemory")
.WithOpenApi();

group.MapPost("/query", async ([FromBody] Query query, SKService service) =>
{
    var (completion, err) = await service.QueryAsync(query);
    if (err is not null)
    {
        return Results.BadRequest(new { message = $"Error querying. {err.Message}" });
    }
    return Results.Ok(completion);
})
.WithName("PostQuery")
.WithOpenApi();

group.MapPost("/tokenize", ([FromBody] TokenizeRequest request, SKService service) =>
{
    if (string.IsNullOrEmpty(request.text))
    {
        return Results.BadRequest(new { message = "Missing required fields. Must include text." });
    }
    var (count, tokens) = service.Tokenize(request.text);
    return Results.Ok(new TokenizeResponse(count, tokens));
})
.WithName("PostTokenize")
.WithOpenApi();

// Serve static files from wwwroot folder
app.UseStaticFiles();

// automatically serve the index.html file
app.MapFallbackToFile("index.html");

app.Run();
