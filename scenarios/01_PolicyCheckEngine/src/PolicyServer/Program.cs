using DotNetEnv;
using SKit.Scenario.PolicyCheck.Models;
using SKit.Scenario.PolicyCheck.Util;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

// Add services to the container.
webApplicationBuilder.Services.AddControllers();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddSwaggerGen();
AddServiceInstances(); 

WebApplication webApplication = webApplicationBuilder.Build();

// Configure the HTTP request pipeline.
if (webApplication.Environment.IsDevelopment())
{
    webApplication.UseSwagger();
    webApplication.UseSwaggerUI();
}

webApplication.UseHttpsRedirection();
webApplication.UseAuthorization();
webApplication.MapControllers();
webApplication.Run();

void AddServiceInstances() {
    string configurationFile = @"../../../../docs/01_DemoEnvironment/conf/application.env";

    Env.Load(configurationFile);
    
    webApplicationBuilder.Services.AddTransient<IOpenAI>(service => new OpenAI(
        apiKey: (Environment.GetEnvironmentVariable("SKIT_AOAI_APIKEY") ?? "AOAI KEY NOT SET"),
        endpoint: (Environment.GetEnvironmentVariable("SKIT_AOAI_ENDPOINT") ?? "AOAI ENDPOINT NOT SET"),
        embeddingModelDeploymentName: (Environment.GetEnvironmentVariable("SKIT_EMBEDDING_DEPLOYMENTNAME")?? "AOAI DEPLOYMENT NAME NOT SET"))
    );

    webApplicationBuilder.Services.AddSingleton<IRepository<PolicyRule>>(service => new PolicyRepository(
        dataFolder: "../../../../assets/scenarios/01_PolicyCheckEngine/preloaded_policies/")
    );
}
