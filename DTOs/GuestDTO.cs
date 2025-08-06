namespace HotelBooking.Chatbot.API.DTOs
{
    public class GuestDTO
    {
        public int GuestId { get; set; }
        public string Name { get; set; }
        public string NIC { get; set; }
        public string Address { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public bool IsResident { get; set; }
    }
}
