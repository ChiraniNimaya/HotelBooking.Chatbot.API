using AutoMapper;
using HotelBooking.API.DTOs;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.Chatbot.API.Services.Clients
{
    public class ChatbotBookingApiClient
    {
        private readonly HttpClient _httpClient;

        public ChatbotBookingApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public class ApiResponse<T>
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }

        public async Task<List<Booking>> GetBookingsByRoomTypeAsync(RoomType roomType)
        {

            try
            {
                var result = await _httpClient.GetFromJsonAsync<ApiResponse<List<Booking>>>($"api/booking/roomType/{roomType}");
                if (result.Status == "error")
                    return null;
                return result.Data;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
