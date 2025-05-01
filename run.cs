using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


class HotelCapacity
{
    private static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var hotelOperations = GetHotelOperations(guests);
        Array.Sort(hotelOperations, (x, y) => x.date.CompareTo(y.date));

        var currentGuests = 0;
        foreach (var operation in hotelOperations)
        {
            currentGuests += operation.guestsDiff;
            
            if (currentGuests > maxCapacity)
                return false;
        }

        return true;
    }

    private static (DateTime date, int guestsDiff)[] GetHotelOperations(List<Guest> guests)
    {        
        var result = new (DateTime date, int guestsDiff)[2 * guests.Count];

        for (var i = 0; i < guests.Count; i++)
        {
            var guest = guests[i];
            var checkInDate = DateTime.Parse(guest.CheckIn);
            var checkOutDate = DateTime.Parse(guest.CheckOut);
            
            result[2 * i] = (checkInDate, 1);
            result[2 * i + 1] = (checkOutDate, -1);
        }
        
        return result;
    }

    class Guest
    {
        public string Name { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }


    static void Main()
    {
        int maxCapacity = int.Parse(Console.ReadLine());
        int n = int.Parse(Console.ReadLine());


        List<Guest> guests = new List<Guest>();


        for (int i = 0; i < n; i++)
        {
            string line = Console.ReadLine();
            Guest guest = ParseGuest(line);
            guests.Add(guest);
        }


        bool result = CheckCapacity(maxCapacity, guests);


        Console.WriteLine(result ? "True" : "False");
    }


    // Простой парсер JSON-строки для объекта Guest
    static Guest ParseGuest(string json)
    {
        var guest = new Guest();


        // Извлекаем имя
        Match nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;


        // Извлекаем дату заезда
        Match checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;


        // Извлекаем дату выезда
        Match checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;


        return guest;
    }
}