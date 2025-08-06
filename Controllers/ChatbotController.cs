using HotelBooking.ChatBot.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.ChatBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _service;

        public ChatbotController(IChatbotService service)
        {
            _service = service;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string chatRequest)
        {
            if (string.IsNullOrWhiteSpace(chatRequest))
                return BadRequest("Question is required.");

            string reply = await _service.HandleUserMessageAsync(chatRequest);
            return Ok(new
            {
                status = "success",
                chatResponse = reply
            });
        }
    }
}
