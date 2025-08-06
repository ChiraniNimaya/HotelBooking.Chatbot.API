namespace HotelBooking.ChatBot.API.Services
{
    public interface IChatbotService
    {
        Task<string> HandleUserMessageAsync(string input);
    }
}
