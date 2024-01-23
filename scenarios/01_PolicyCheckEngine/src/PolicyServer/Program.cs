using DotNetEnv;
using PCheck.Models;
using PCheck.Util;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
AddServiceInstances(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



void AddServiceInstances() {

    string configurationFile = @"../../../../docs/01_DemoEnvironment/conf/application.env";

    Env.Load(configurationFile);
    
    builder.Services.AddTransient<IOpenAI>(service => new OpenAI(
        apiKey: (Environment.GetEnvironmentVariable("SKIT_AOAI_APIKEY") ?? "AOAI KEY NOT SET"),
        endpoint: (Environment.GetEnvironmentVariable("SKIT_AOAI_ENDPOINT") ?? "AOAI ENDPOINT NOT SET"),
        embeddingModelDeploymentName: (Environment.GetEnvironmentVariable("SKIT_EMBEDDING_DEPLOYMENTNAME")?? "AOAI DEPLOYMENT NAME NOT SET"))
    );

    builder.Services.AddSingleton<IRepository<Policy>>(service => new PolicyRepository(
        dataFolder: "../../preloaded_policies/")
    );

}
