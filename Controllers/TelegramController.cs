using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoodTracker.Services;

namespace FoodTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private readonly TelegramService _telegramService;

        public TelegramController(TelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TelegramUpdate update)
        {
            await _telegramService.ProcessUpdate(update);
            return Ok();
        }
    }
} 