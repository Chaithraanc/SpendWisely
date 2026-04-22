using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using OpenTelemetry.Metrics;
using Quartz;
using Serilog;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Infrastructure.AI;
using SpendWiselyAPI.Infrastructure.Authentication;
using SpendWiselyAPI.Infrastructure.Authentication.AuthContext;
using SpendWiselyAPI.Infrastructure.BackgroundServices;
using SpendWiselyAPI.Infrastructure.Caching;
using SpendWiselyAPI.Infrastructure.Caching.MonthlySummary;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Messaging;
using SpendWiselyAPI.Infrastructure.Messaging.Consumers;
using SpendWiselyAPI.Infrastructure.Messaging.Publishers;
using SpendWiselyAPI.Infrastructure.MongoDB;
using SpendWiselyAPI.Infrastructure.MongoDB.Repositories;
using SpendWiselyAPI.Infrastructure.Repositories;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;
using StackExchange.Redis;
using System.Net;
using System.Text;


System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Dependency Injection for DbContext
builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
// Dependency Injection for Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
builder.Services.AddScoped<IDashboardMonthlySummaryRepository, DashboardMonthlySummaryRepository>();
builder.Services.AddScoped<IAIInsightsRepository, AIInsightsRepository>();
// Dependency Injection for Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IDashboardMonthlySummaryService, DashboardMonthlySummaryService>();
builder.Services.AddScoped<IAIInsightsService, AIInsightsService>();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<INightlyReconciliationService, NightlyReconciliationService>();


// Bind settings from configuration for MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Register as singleton 
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

// Register repository
builder.Services.AddSingleton<IEventStoreRepository, EventStoreRepository>();

// Configure MongoDB Bson serialization for Guid to ensure compatibility and consistency
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Dependency Injection for RabbitMQ Event Publisher
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Dependency Injection for OutboxEventRepository
builder.Services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();


// Register the OutboxProcessor as a hosted service to run in the background as singleton since it doesn't maintain any state and can be shared across the application.
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddSingleton<MessagingPolicies>();
// Register the MongoDBConsumer as a hosted service to run in the background as singleton since it doesn't maintain any state and can be shared across the application.
builder.Services.AddHostedService<MongoDBConsumer>();
// Register the AIExpenseCategorizationConsumer as a hosted service to run in the background as singleton since it doesn't maintain any state and can be shared across the application.
builder.Services.AddHostedService<AIExpenseCategorizationConsumer>();
// Register the AIMonthlyInsightsConsumer as a hosted service to run in the background as singleton since it doesn't maintain any state and can be shared across the application.
builder.Services.AddHostedService<AIMonthlyInsightsConsumer>();

// Dependency Injection for AIService with HttpClient configuration
var openAiSection = builder.Configuration.GetSection("OpenAI");
builder.Services.Configure<OpenAISettings>(openAiSection);

var openAiSettings = openAiSection.Get<OpenAISettings>();
builder.Services.AddSingleton<IAIService, AIService>();

builder.Services.AddScoped<IRedisService, RedisService>();
//builder.Services.AddHttpClient<IAIService, AIService>(client =>
//{
//    client.BaseAddress = new Uri(openAiSettings.BaseUrl);
//    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiSettings.ApiKey}");
//});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var config = builder.Configuration;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpendWisely API", Version = "v1" });

    // Add JWT Auth to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token **without** Bearer prefix",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddAuthorization();

//builder.Services.AddSingleton<IConnectionMultiplexer>(RedisConnection.Connection);
//builder.Services.AddSingleton<IRedisCache, RedisCache>();

//var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
//Register Redis connection
//builder.Services.AddSingleton<IConnectionMultiplexer>(
//    ConnectionMultiplexer.Connect(redisConnectionString));
//// Dependency Injection for RedisCache
//builder.Services.AddSingleton<IRedisCache, RedisCache>();

builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("RedisSettings"));


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
    Console.WriteLine("Redis: " + settings.ConnectionString);
    return ConnectionMultiplexer.Connect(settings.ConnectionString);
});


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Quartz.NET for scheduling the NightlyReconciliationJob to run at 2 AM Singapore time daily
builder.Services.AddQuartz(q =>
{
    

    // Register your job
    q.AddJob<NightlyReconciliationJob>(opts => opts.WithIdentity("NightlyReconciliationJob").StoreDurably());

    // Trigger at 2 AM Singapore time daily
    q.AddTrigger(opts => opts
        .ForJob("NightlyReconciliationJob")
        .WithIdentity("NightlyReconciliationTrigger")
        .WithCronSchedule("0 0 2 * * ?", x => x
        .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"))
        )
    );
       // .WithCronSchedule("0 * * * * ?", x => x
    
});
//builder.Services.AddHostedService<StartupJobRunner>(); //to trigger the job immediately on application startup for testing purposes. You can remove this in production if you only want the job to run on schedule.

// Add Quartz Hosted Service to run the Quartz scheduler in the background
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

builder.Services.AddScoped<NightlyReconciliationJob>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddMetrics();
//builder.Services.AddMetricsTrackingMiddleware();
//builder.Services.AddMetricsEndpoints();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();

        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

        metrics.AddPrometheusExporter();
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Add Serilog request logging middleware to log details of each HTTP request and response, including method, path, status code, and elapsed time.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

//app.UseHttpsRedirection();
app.MapPrometheusScrapingEndpoint();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
// Metrics middleware BEFORE endpoints
//app.UseMetricsAllMiddleware();


// Your API endpoints
app.MapControllers();

// Metrics endpoints AFTER routing
//app.UseMetricsAllEndpoints();


app.Run();
