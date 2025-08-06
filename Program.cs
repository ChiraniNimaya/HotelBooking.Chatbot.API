using HotelBooking.Chatbot.API.Services.Clients;
using HotelBooking.ChatBot.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IChatbotService, ChatbotService>();

builder.Services.AddHttpClient<ChatbotBookingApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7153/"); // BookingService base URL
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
