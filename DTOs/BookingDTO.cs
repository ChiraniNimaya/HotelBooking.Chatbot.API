using System.ComponentModel.DataAnnotations.Schema;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.API.DTOs
{
    public class BookingDTO
    {
        public int? BookingId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public bool IsRecurring { get; set; }
        public int GuestId { get; set; }

        [NotMapped] 
        public Dictionary<RoomType, int> RoomInfo { get; set; }
        [NotMapped] 
        public string SpecialRequests { get; set; }

        public List<int>? RoomIds { get; set; }
        public float TotalPrice { get; set; }

    }

}
