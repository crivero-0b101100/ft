using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using FoodTracker.Models;

namespace FoodTracker.Services
{
    public class TelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly UserService _userService;

        public TelegramService(IConfiguration configuration, UserService userService)
        {
            _httpClient = new HttpClient();
            _botToken = configuration["TELEGRAM_BOT_TOKEN"];
            _userService = userService;
        }

        public async Task ProcessUpdate(TelegramUpdate update)
        {
            if (update?.Message?.Text == null) return;

            var message = update.Message;
            var response = await ProcessCommand(message.Text, message.From.Id);

            if (!string.IsNullOrEmpty(response))
            {
                await SendMessage(message.Chat.Id, response);
            }
        }

        private async Task<string> ProcessCommand(string text, long userId)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var parts = text.Split(' ', 2);
            var command = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1] : null;

            try
            {
                return command switch
                {
                    "/s" => await _userService.SetupUser(userId, args),
                    "/wi" => await _userService.LogWeight(userId, args),
                    "/f" => await _userService.LogFood(userId, args),
                    "/e" => await _userService.LogExercise(userId, args),
                    "/h" => await _userService.GetHistory(userId),
                    "/p" => await _userService.GetProjection(userId),
                    "/tw" => await _userService.GetTheoreticalWeight(userId),
                    _ => "Unknown command. Use /s, /wi, /f, /e, /h, /p, or /tw"
                };
            }
            catch (Exception ex)
            {
                return $"Error processing command: {ex.Message}";
            }
        }

        private async Task SendMessage(long chatId, string text)
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "Markdown"
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            response.EnsureSuccessStatusCode();
        }
    }

    public class TelegramUpdate
    {
        public TelegramMessage Message { get; set; }
    }

    public class TelegramMessage
    {
        public long Chat { get; set; }
        public TelegramUser From { get; set; }
        public string Text { get; set; }
    }

    public class TelegramUser
    {
        public long Id { get; set; }
    }
} 