using AutoMapper;
using HotelBooking.API.Utilities;
using HotelBooking.Chatbot.API;
using HotelBooking.Chatbot.API.Services.Clients;
using HotelBooking.ChatBot.API.Utils.Constants;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static HotelBooking.ChatBot.API.Enums.RoomTypes;

namespace HotelBooking.ChatBot.API.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly ChatbotBookingApiClient _apiClient;

        public ChatbotService(ChatbotBookingApiClient apiClient)
        {
            _apiClient = apiClient;
        }
        public async Task<string> HandleUserMessageAsync(string input)
        {
            input = input.ToLower().Trim();

            // Remove common punctuation and normalize
            input = Regex.Replace(input, @"[^\w\s]", " ");
            input = Regex.Replace(input, @"\s+", " ");

            // === ROOM TYPE DETECTION WITH SYNONYMS ===
            RoomType roomTypeForCalculations = DetectRoomType(input) ?? RoomType.Standard;


            // === GUEST TYPE DETECTION ===
            bool isResident = DetectGuestType(input);

            // === DATE DETECTION ===
            (DateTime checkIn, DateTime checkOut) = DetectDates(input);

            // === ROOM QUANTITY DETECTION ===
            int roomQuantity = DetectRoomQuantity(input);

            // === INTENT DETECTION ===
            string intent = DetectIntent(input);

            // === RESPONSE GENERATION BASED ON INTENT ===
            switch (intent)
            {
                case "price":
                    return await GeneratePriceResponseAsync(roomTypeForCalculations, checkIn, checkOut, isResident, roomQuantity);

                case "availability":
                    return await GenerateAvailabilityResponseAsync(roomTypeForCalculations, checkIn, checkOut, roomQuantity);

                case "room_info":
                    return GenerateRoomInfoResponse(DetectRoomType(input)); 

                case "booking_help":
                    return GenerateBookingHelpResponse(); 

                default:
                    return GenerateDefaultResponse(); 
            }
        }

        private RoomType? DetectRoomType(string input)
        {
            // Room type synonyms dictionary
            var roomTypeSynonyms = new Dictionary<RoomType, string[]>
            {
                [RoomType.Standard] = new string[] { "standard", "basic", "regular", "normal", "simple", "economy", "budget" },
                [RoomType.Deluxe] = new string[] { "deluxe", "premium", "upgraded", "superior", "enhanced", "better", "luxurious" },
                [RoomType.Suite] = new string[] { "suite", "luxury", "presidential", "executive", "penthouse", "vip", "royal" },
                [RoomType.Family] = new string[] { "family", "group", "large", "big", "spacious", "kids", "children", "multiple bed" }
            };

            foreach (var roomType in roomTypeSynonyms)
            {
                if (roomType.Value.Any(synonym => input.Contains(synonym)))
                {
                    return roomType.Key;
                }
            }

            return null; // default 
        }

        private bool DetectGuestType(string input)
        {
            // Resident synonyms
            string[] residentSynonyms = { "resident", "local", "citizen", "national", "domestic", "sri lankan" };

            // Non-resident synonyms
            string[] nonResidentSynonyms = { "non-resident", "foreigner", "tourist", "visitor", "international", "overseas", "foreign" };

            if (residentSynonyms.Any(synonym => input.Contains(synonym)))
                return true;

            if (nonResidentSynonyms.Any(synonym => input.Contains(synonym)))
                return false;

            return false; // default foreigner
        }

        private (DateTime checkIn, DateTime checkOut) DetectDates(string input)
        {
            DateTime today = DateTime.Today;
            DateTime checkIn = today.AddDays(7);  // default: next week
            DateTime checkOut = checkIn.AddDays(1);

            // Time period synonyms
            if (ContainsAny(input, "tonight", "today"))
            {
                checkIn = today;
                checkOut = today.AddDays(1);
            }
            else if (ContainsAny(input, "tomorrow"))
            {
                checkIn = today.AddDays(1);
                checkOut = today.AddDays(2);
            }
            else if (ContainsAny(input, "this weekend", "weekend"))
            {
                int daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilFriday == 0) daysUntilFriday = 7; // if today is Friday, get next Friday
                checkIn = today.AddDays(daysUntilFriday);
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "next weekend"))
            {
                int daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
                checkIn = today.AddDays(daysUntilFriday + 7);
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "next week"))
            {
                checkIn = today.AddDays(7);
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "next month"))
            {
                checkIn = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(4);
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "christmas", "december"))
            {
                checkIn = new DateTime(today.Year, 12, 23);
                if (checkIn < today) checkIn = checkIn.AddYears(1);
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "new year"))
            {
                checkIn = new DateTime(today.Year + 1, 1, 1);
                checkOut = checkIn.AddDays(2);
            }

            // Duration detection
            if (ContainsAny(input, "one night", "1 night"))
            {
                checkOut = checkIn.AddDays(1);
            }
            else if (ContainsAny(input, "two nights", "2 nights"))
            {
                checkOut = checkIn.AddDays(2);
            }
            else if (ContainsAny(input, "three nights", "3 nights"))
            {
                checkOut = checkIn.AddDays(3);
            }
            else if (ContainsAny(input, "a week", "7 days"))
            {
                checkOut = checkIn.AddDays(7);
            }

            return (checkIn, checkOut);
        }

        private int DetectRoomQuantity(string input)
        {
            // Extract numbers from input
            var numbers = Regex.Matches(input, @"\b\d+\b");

            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int quantity) && quantity > 0 && quantity <= 10)
                {
                    return quantity;
                }
            }

            // Word-based quantity detection
            if (ContainsAny(input, "one", "single", "a room"))
                return 1;
            else if (ContainsAny(input, "two", "double"))
                return 2;
            else if (ContainsAny(input, "three"))
                return 3;
            else if (ContainsAny(input, "four"))
                return 4;
            else if (ContainsAny(input, "five"))
                return 5;

            return 1; // default
        }

        private string DetectIntent(string input)
        {
            // Price-related synonyms
            string[] priceKeywords = { "price", "cost", "rate", "charge", "fee", "how much", "expensive", "cheap", "pricing", "tariff", "amount" };

            // Availability-related synonyms
            string[] availabilityKeywords = { "available", "availability", "free", "vacant", "reserve", "open", "empty" };

            // Room information synonyms
            string[] roomInfoKeywords = { "room types", "what rooms", "describe", "facilities", "amenities", "features", "about rooms" };

            // Booking help synonyms
            string[] bookingHelpKeywords = { "how to book", "booking process", "reservation", "summary", "booking details", "help" };

            if (priceKeywords.Any(keyword => input.Contains(keyword)))
                return "price";

            if (availabilityKeywords.Any(keyword => input.Contains(keyword)))
                return "availability";

            if (roomInfoKeywords.Any(keyword => input.Contains(keyword)))
                return "room_info";

            if (bookingHelpKeywords.Any(keyword => input.Contains(keyword)))
                return "booking_help";

            return "unknown";
        }

        private async Task<float> PredictRoomRate(RoomType roomType, DateTime checkIn, bool isResident)
        {
            float baseRate = RoomBaseRates.GetRoomRateByType(roomType);

            // Analyze historical booking data for pricing patterns
            var historicalBookings = await _apiClient.GetBookingsByRoomTypeAsync(roomType);

            if (historicalBookings != null && historicalBookings.Any())
            {
                // Calculate average rate from historical data
                var avgHistoricalRate = historicalBookings
                    .Where(b => b.TotalPrice > 0 && b.RoomInfo.Any(r => r.Key == roomType))
                    .Select(b => new
                    {
                        Nights = (b.CheckOutDate - b.CheckInDate).Days,
                        RoomCount = b.RoomInfo.Where(r => r.Key == roomType).Sum(r => r.Value),
                        TotalPrice = b.TotalPrice
                    })
                    .Where(x => x.Nights > 0 && x.RoomCount > 0)
                    .Select(x => x.TotalPrice / (x.Nights * x.RoomCount)) // Price per room per night
                    .DefaultIfEmpty(baseRate)
                    .Average();

                // Use historical average if available, otherwise use base rate
                baseRate = avgHistoricalRate > 0 ? avgHistoricalRate : baseRate;
            }

            // Dynamic pricing based on demand
            float demandMultiplier = await CalculateDemandMultiplier(roomType, checkIn);

            // Seasonal pricing
            float seasonalMultiplier = PricingManager.CalculateSeasonalMultiplier(checkIn);

            // Guest type pricing
            float guestMultiplier = isResident ? 0.8f : 1.0f; // 20% discount for residents

            // Weekend pricing
            float weekendMultiplier = (checkIn.DayOfWeek == DayOfWeek.Friday ||
                                        checkIn.DayOfWeek == DayOfWeek.Saturday) ? 1.05f : 1.0f;

            return baseRate * demandMultiplier * seasonalMultiplier * guestMultiplier * weekendMultiplier;
        }

        // Calculate demand multiplier based on booking density
        private async Task<float> CalculateDemandMultiplier(RoomType roomType, DateTime checkIn)
        {
            var bookings = await _apiClient.GetBookingsByRoomTypeAsync(roomType);

            // Check bookings in the surrounding week
            DateTime weekStart = checkIn.AddDays(-7);
            DateTime weekEnd = checkIn.AddDays(7);

            var nearbyBookings = 1; // default

            if (bookings !=null && bookings.Count > nearbyBookings)
            {
                nearbyBookings = bookings
                    .Where(b => b.CheckInDate >= weekStart && b.CheckInDate <= weekEnd)
                    .SelectMany(b => b.RoomInfo.Where(r => r.Key == roomType))
                    .Sum(r => r.Value);
            }

            int totalRooms = RoomCounts.GetTotalRoomCountByType(roomType);
            float occupancyRate = (float)nearbyBookings / (totalRooms * 14); // 14 days

            // Increase price based on occupancy
            if (occupancyRate > 0.8f) return 1.3f;      // High demand
            if (occupancyRate > 0.6f) return 1.15f;     // Medium demand
            if (occupancyRate > 0.4f) return 1.0f;      // Normal demand
            return 0.9f;                                 // Low demand
        }


        // Availability checking
        private async Task<bool> CheckRoomAvailability(RoomType roomType, DateTime checkIn, DateTime checkOut, int quantity)
        {
            Dictionary<RoomType, int> requestedRooms = new Dictionary<RoomType, int>();
            requestedRooms.Add(roomType, quantity);

            var bookings = await _apiClient.GetBookingsByRoomTypeAsync(roomType);

            return await AreRoomsAvailable(bookings, requestedRooms, checkIn, checkOut);
        }

        private async Task<bool> AreRoomsAvailable(List<Booking> existingBookings, Dictionary<RoomType, int> requestedRooms, DateTime checkIn, DateTime checkOut)
        {
            // Track how many rooms of each type are already booked
            Dictionary<RoomType, int> roomsBooked = new Dictionary<RoomType, int>();

            foreach (var booking in existingBookings)
            {
                // Skip bookings that do not overlap with the requested period
                if (booking.CheckOutDate <= checkIn || booking.CheckInDate >= checkOut)
                    continue;

                if (booking.RoomInfo != null)
                {
                    foreach (var entry in booking.RoomInfo)
                    {
                        RoomType roomType = entry.Key;
                        int count = entry.Value;

                        if (!roomsBooked.ContainsKey(roomType))
                            roomsBooked[roomType] = 0;

                        roomsBooked[roomType] += count;
                    }
                }
            }

            // Check if requested rooms can be accommodated
            foreach (var request in requestedRooms)
            {
                RoomType type = request.Key;
                int requestedQty = request.Value;
                int alreadyBooked = roomsBooked.ContainsKey(type) ? roomsBooked[type] : 0;
                int totalAvailable = RoomCounts.GetTotalRoomCountByType(type);

                if (requestedQty + alreadyBooked > totalAvailable)
                    return false;
            }

            return true;
        }



        // Get occupancy statistics for better availability response
        private async Task<(int occupied, int total, float occupancyRate)> GetOccupancyStats(RoomType roomType, DateTime checkIn, DateTime checkOut)
        {
            int totalRooms = RoomCounts.GetTotalRoomCountByType(roomType);

            var bookings = await _apiClient.GetBookingsByRoomTypeAsync(roomType);

            int overlappingBookedRooms = 0;

            if (bookings != null && bookings.Count > 0)
            {
                overlappingBookedRooms = bookings
                            .Where(b => !(b.CheckOutDate <= checkIn || b.CheckInDate >= checkOut))
                            .Where(b => b.RoomInfo != null && b.RoomInfo.ContainsKey(roomType))
                            .Sum(b => b.RoomInfo[roomType]);
            }

            int occupiedRooms = Math.Min(overlappingBookedRooms, totalRooms);
            float occupancyRate = (float)occupiedRooms / totalRooms;

            return (occupiedRooms, totalRooms, occupancyRate);
        }

        private async Task<string> GeneratePriceResponseAsync(RoomType roomType, DateTime checkIn, DateTime checkOut, bool isResident, int quantity)
        {
            try
            {
                // Calculate total nights
                int nights = (checkOut - checkIn).Days;
                if (nights <= 0) nights = 1;

                float dailyRate = await PredictRoomRate(roomType, checkIn, isResident);
                float totalCost = dailyRate * nights * quantity;

                string guestType = isResident ? "residents" : "non-residents";
                string roomText = quantity == 1 ? "room" : "rooms";
                string nightText = nights == 1 ? "night" : "nights";

                // Add demand information
                var (occupied, total, occupancyRate) = await GetOccupancyStats(roomType, checkIn, checkOut);
                string demandInfo = "";
                if (occupancyRate > 0.7f)
                    demandInfo = " (High demand period - limited availability)";
                else if (occupancyRate > 0.4f)
                    demandInfo = " (Moderate demand)";
                else
                    demandInfo = " (Good availability)";

                return $"Predicted price for {quantity} {roomType} {roomText} for {nights} {nightText} ({checkIn:MMM dd} to {checkOut:MMM dd}) for {guestType}:\n" +
                       $"Rate: Rs. {dailyRate:F2} per room per night{demandInfo}\n" +
                       $"Total Cost: Rs. {totalCost:F2}\n\n" +
                       $"*Price based on historical data and current demand patterns";
            }
            catch (Exception ex)
            {
                return $"Unable to predict price at the moment. An Error occured : {ex}";
            }
        }

        private async Task<string> GenerateAvailabilityResponseAsync(RoomType roomType, DateTime checkIn, DateTime checkOut, int quantity)
        {
            try
            {
                bool available = await CheckRoomAvailability(roomType, checkIn, checkOut, quantity);
                var (occupied, total, occupancyRate) = await GetOccupancyStats(roomType, checkIn, checkOut);

                string roomText = quantity == 1 ? "room" : "rooms";
                int nights = (checkOut - checkIn).Days;
                if (nights <= 0) nights = 1;

                if (available)
                {
                    string availabilityLevel = "";
                    if (occupancyRate > 0.7f)
                        availabilityLevel = "Limited availability";
                    else if (occupancyRate > 0.4f)
                        availabilityLevel = "Good availability";
                    else
                        availabilityLevel = "Excellent availability";

                    return $"{availabilityLevel}!\n" +
                           $"{quantity} {roomType} {roomText} predicted to be available\n" +
                           $"Check-in: {checkIn:MMM dd, yyyy} | Check-out: {checkOut:MMM dd, yyyy} ({nights} nights)\n\n" +
                           $"Current occupancy: {occupied}/{total} rooms ({occupancyRate:P0})\n\n";
                }
                else
                {
                    // Suggest alternatives based on data
                    var alternatives = GetAlternativeSuggestions(roomType, checkIn, checkOut, quantity);

                    return $"✗ {quantity} {roomType} {roomText} predicted to be unavailable from {checkIn:MMM dd} to {checkOut:MMM dd}\n" +
                           $"Current occupancy: {occupied}/{total} rooms ({occupancyRate:P0})\n\n" +
                           $"Based on booking patterns, you might consider:\n{alternatives}";
                }
            }
            catch (Exception)
            {
                return "Unable to check availability at the moment. Please try again with a clearer query.";
            }
        }

        private async Task<string> GetAlternativeSuggestions(RoomType requestedType, DateTime checkIn, DateTime checkOut, int quantity)
        {
            var suggestions = new List<string>();

            // Check other room types
            foreach (RoomType roomType in Enum.GetValues<RoomType>())
            {
                bool isAvailable = await CheckRoomAvailability(roomType, checkIn, checkOut, quantity);
                if (roomType != requestedType && isAvailable)
                {
                    suggestions.Add($"• {roomType} rooms are available");
                }
            }

            // Check nearby dates
            for (int i = 1; i <= 7; i++)
            {
                DateTime altCheckIn = checkIn.AddDays(i);
                DateTime altCheckOut = checkOut.AddDays(i);
                bool isAvailable = await CheckRoomAvailability(requestedType, altCheckIn, altCheckOut, quantity);
                if (isAvailable)
                {
                    suggestions.Add($"• {requestedType} available from {altCheckIn:MMM dd} to {altCheckOut:MMM dd}");
                    break;
                }
            }

            // Check reduced quantity
            if (quantity > 1)
            {
                for (int q = quantity - 1; q >= 1; q--)
                {
                    bool isAvailable = await CheckRoomAvailability(requestedType, checkIn, checkOut, q);
                    if (isAvailable)
                    {
                        suggestions.Add($"• {q} {requestedType} room{(q > 1 ? "s" : "")} available");
                        break;
                    }
                }
            }

            return suggestions.Any() ? string.Join("\n", suggestions) : "• Try different dates or contact reception for more options";
        }

        private string GenerateRoomInfoResponse(RoomType? specificType = null)
        {
            string allRoomTypesInfo = "Our Room Types:\n\n" +
                                      "• Standard - Basic amenities, ideal for solo/couples\n" +
                                      "• Deluxe - Spacious and modern with premium features\n" +
                                      "• Suite - Luxurious stay with city views and executive amenities\n" +
                                      "• Family - Perfect for families (up to 5 guests)\n";

            string specificRoomInfo = string.Empty;

            if (specificType.HasValue)
            {
                switch (specificType.Value)
                {
                    case RoomType.Standard:
                        specificRoomInfo = "\n\nStandard Room:\n• Basic amenities\n• Ideal for solo travelers or couples\n• Air conditioning, TV, Wi-Fi\n• Single or double bed options";
                        break;
                    case RoomType.Deluxe:
                        specificRoomInfo = "\n\nDeluxe Room:\n• Spacious and modern\n• Premium amenities\n• City or garden view\n• Work desk and seating area";
                        break;
                    case RoomType.Suite:
                        specificRoomInfo = "\n\nSuite:\n• Luxurious accommodation\n• Separate living area\n• Premium city views\n• Executive amenities and services";
                        break;
                    case RoomType.Family:
                        specificRoomInfo = "\n\nFamily Room:\n• Perfect for families (up to 5 guests)\n• Multiple beds or sofa bed\n• Extra space and storage\n• Family-friendly amenities";
                        break;
                }
            }
            else
            {
                specificRoomInfo = "\n\nAsk about specific room types for detailed information!";
            }

            return allRoomTypesInfo + specificRoomInfo;
        }


        private string GenerateBookingHelpResponse()
        {
            return "How to Make a Booking:\n\n" +
                   "I can help you with:\n" +
                   "✓ Check room availability\n" +
                   "✓ Get price quotes\n" +
                   "✓ Compare room types\n\n" +
                   "For actual booking, please contact the reception\n";
        }

        private string GenerateDefaultResponse()
        {
            return "I'm here to help with:\n\n" +
                   "Room Prices \n" +
                   "Availability \n" +
                   "Room Information \n" +
                   "Example: 'Is a suite available for 2 nights next week for a tourist?'";
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

    }
}
