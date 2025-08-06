using HotelBooking.ChatBot.API.Utils.Constants;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.ChatBot.API.Utils.Constants
{
    public class RoomCounts
    {
        // Total room counts in the hotel
        public const int STNDARD_ROOM_COUNT = 20;
        public const int DELUXE_ROOM_COUNT = 10;
        public const int SUITE_ROOM_COUNT = 5;
        public const int FAMILY_ROOM_COUNT = 8;
        public static int GetTotalRoomCountByType(RoomType type)
        {
            return type switch
            {
                RoomType.Standard => STNDARD_ROOM_COUNT,
                RoomType.Deluxe => DELUXE_ROOM_COUNT,
                RoomType.Suite => SUITE_ROOM_COUNT,
                RoomType.Family => FAMILY_ROOM_COUNT,
                _ => 0
            };
        }
    }
}
