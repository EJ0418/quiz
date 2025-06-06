using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using DotNetEnv;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
// 加入 MariaDB
string server = Environment.GetEnvironmentVariable("DB_SERVER");
var connStr = builder.Configuration.GetConnectionString("MariaDb")
    .Replace("${DB_SERVER}", Environment.GetEnvironmentVariable("DB_SERVER"))
    .Replace("${DB_PORT}", Environment.GetEnvironmentVariable("DB_PORT"))
    .Replace("${DB_DB}", Environment.GetEnvironmentVariable("DB_DB"))
    .Replace("${DB_USER}", Environment.GetEnvironmentVariable("DB_USER"))
    .Replace("${DB_PASSWORD}", Environment.GetEnvironmentVariable("DB_PASSWORD"));
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseMySql(connStr,
        ServerVersion.AutoDetect(connStr)
    )
);

// Redis 設定
//註冊Redis連線物件
//把這個連線物件註冊為 Singleton，代表整個應用程式生命週期只會有一個實例。
//這樣做可以避免重複建立連線，提升效能並減少資源消耗。
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")},password={Environment.GetEnvironmentVariable("REDIS_PASSWORD")}"
    )
);
//註冊RedisCachService
builder.Services.AddScoped<RedisCacheService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(opt =>
    {
        opt.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
    }
    );
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"quiz API");
        c.RoutePrefix = "doc";
    });
}

app.MapControllers();

// 健康檢查 API
app.MapGet("/health", () => Results.Ok("後端服務Healthy"))
    .WithTags("Health")
    .WithMetadata(new SwaggerOperationAttribute(summary: "健康檢查", description: "檢查後端服務狀態"));

app.Run();
