using HotelBooking.ChatBot.API.Utils.Constants;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.ChatBot.API.Utils.Constants
{
    public class RoomBaseRates
    {
        // Room rates
        private const float STANDARD_BASE_PRICE = 8000f;
        private const float DELUXE_BASE_PRICE = 12000f;
        private const float SUITE_BASE_PRICE = 18000f;
        private const float FAMILY_BASE_PRICE = 15000f;

        public static float GetRoomRateByType(RoomType type)
        {
            return type switch
            {
                RoomType.Standard => STANDARD_BASE_PRICE,
                RoomType.Deluxe => DELUXE_BASE_PRICE,
                RoomType.Suite => SUITE_BASE_PRICE,
                RoomType.Family => FAMILY_BASE_PRICE,
                _ => 0
            };
        }
    }
}
