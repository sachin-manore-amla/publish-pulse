using ZnodePublishUtility.API.Infrastructure.Elasticsearch;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MongoDB;
using ZnodePublishUtility.Data.Repositories;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
    ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]
    ?? "ZnodePublishUtility";
var healthServiceBaseUrl = builder.Configuration["HealthService:BaseUrl"]
    ?? string.Empty;
var elasticsearchBaseUrl = builder.Configuration["Elasticsearch:BaseUrl"]
    ?? string.Empty;

// ── MongoDB ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton(_ => new MongoDbContext(mongoConnectionString, mongoDatabaseName));

// ── HTTP client for HealthService proxy ──────────────────────────────────────
builder.Services.AddHttpClient("HealthService", client =>
{
    if (!string.IsNullOrWhiteSpace(healthServiceBaseUrl))
    {
        client.BaseAddress = new Uri(healthServiceBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
});

// ── HTTP client for direct Elasticsearch access ──────────────────────────────
builder.Services.AddHttpClient("Elasticsearch", client =>
{
    if (!string.IsNullOrWhiteSpace(elasticsearchBaseUrl))
    {
        client.BaseAddress = new Uri(elasticsearchBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
});
builder.Services.AddScoped<IElasticsearchClient, ElasticsearchClient>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IPortalRepository, PortalRepository>();
builder.Services.AddScoped<IPublishJobRepository, PublishJobRepository>();
builder.Services.AddScoped<IPublishHistoryRepository, PublishHistoryRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IPublishJobService, PublishJobService>();
builder.Services.AddScoped<IPublishHistoryService, PublishHistoryService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// ── API + Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Publish Utility API",
        Version = "v1",
        Description = "BFF for Znode Publish Management — catalog, store, CMS publishing with MongoDB persistence"
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:8080", "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddLogging();

// ── Response compression ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Publish Utility API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
