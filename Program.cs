using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FoodTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TelegramService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Set up webhook
var telegramService = app.Services.GetRequiredService<TelegramService>();
var botToken = builder.Configuration["TELEGRAM_BOT_TOKEN"];
var webhookUrl = builder.Configuration["WEBHOOK_URL"];

if (!string.IsNullOrEmpty(webhookUrl))
{
    using var scope = app.Services.CreateScope();
    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
    var setWebhookUrl = $"https://api.telegram.org/bot{botToken}/setWebhook?url={webhookUrl}/api/telegram";
    await httpClient.GetAsync(setWebhookUrl);
}

app.Run(); 