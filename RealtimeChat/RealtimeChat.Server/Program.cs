using RealtimeChat.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using RealtimeChat.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRealtimeChatWeb", policy =>
    {
        policy
            .WithOrigins("https://localhost:7170")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContextFactory<ChatDbContext>(options =>
{
    string connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection 연결 문자열이 없습니다.");

    options.UseSqlite(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowRealtimeChatWeb");

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();
