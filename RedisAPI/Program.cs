using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisAPI.Data;
using RedisAPI.Models;
using System.Text.Json;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
///Limitar requests por IP, Devolver 429 Too Many Requests
///Prueba local : for /L %i in (1,1,10) do curl -k -i https://localhost:7016/api/v1/products/1

//builder.Services.AddRateLimiter(options =>
//{
//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

//    options.AddFixedWindowLimiter("fixed", limiter =>
//    {
//        limiter.PermitLimit = 5;
//        limiter.Window = TimeSpan.FromMinutes(1);
//        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//        limiter.QueueLimit = 0;
//    });
//});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseRateLimiter();
app.UseHttpsRedirection();

//////////////////////////////////////////////

app.MapGet("/health", () => Results.Ok("OK"));

///Sin Redis 
//app.MapGet("/api/v1/products/{id:int}", async (int id, AppDbContext db) =>
//{
//    var product = await db.Products.FindAsync(id);
//    return product is not null ? Results.Ok(product) : Results.NotFound();
//});


app.MapGet("/api/v1/products/{id:int}",
async (int id, AppDbContext db, IDistributedCache cache) =>
{
    var cacheKey = $"v1:product:{id}";

    var cached = await cache.GetStringAsync(cacheKey);
    if (cached != null)
        return Results.Ok(JsonSerializer.Deserialize<Product>(cached));

    var product = await db.Products.FindAsync(id);
    if (product is null)
        return Results.NotFound();

    await cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(product),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

    return Results.Ok(product);
});
//.RequireRateLimiting("fixed");

app.Run();