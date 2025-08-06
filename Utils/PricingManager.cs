using HotelBooking.ChatBot.API.Utils.Constants;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.API.Utilities
{
    public static class PricingManager
    {
        public static float CalculateSeasonalMultiplier(DateTime date)
        {
            int month = date.Month;

            if (month == 12) return 1.25f;                      // Holidays (December)
            if (month >= 1 && month <= 3) return 1.2f;          // Peak season (Jan-March)
            if (month == 8) return 1.15f;                       // Mid season (August)
            if (month == 5 || month == 6) return 0.9f;          // Rainy season
            return 1.0f;                                        // Default/low season
        }

        public static float CalculateRoomRate(RoomType type, DateTime date, bool isResident)
        {
            float guestMultiplier = isResident ? 0.8f : 1.0f;
            float seasonalMultiplier = CalculateSeasonalMultiplier(date);

            return RoomBaseRates.GetRoomRateByType(type) * guestMultiplier * seasonalMultiplier;
        }

        
    }
}
