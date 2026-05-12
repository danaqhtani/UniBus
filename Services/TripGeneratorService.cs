using UniBusApp.Data;
using UniBusApp.Models;

public class TripGeneratorService
{
    private readonly UniBusDbContext _context;

    public TripGeneratorService(UniBusDbContext context)
    {
        _context = context;
    }

    public void EnsureTodayTripsGenerated()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var day = DateTime.Today.DayOfWeek;

        if (day == DayOfWeek.Friday || day == DayOfWeek.Saturday)
            return;

        var todayTrips = _context.ShuttleTrip
     .Where(t => t.trip_date == today)
     .ToList();

        var activeDriversCount = _context.Driver.Count(d => d.is_active);

        // If the number of trips is less than the number of drivers → Regenerate
        if (todayTrips.Count < activeDriversCount)
        {
            _context.ShuttleTrip.RemoveRange(todayTrips);
            _context.SaveChanges();
        }
        else if (todayTrips.Any())
        {
            return;
        }
        var metro = _context.MetroStation.FirstOrDefault();
        if (metro == null) return;

        var drivers = _context.Driver
            .Where(d => d.is_active)
            .OrderBy(d => d.driver_id)
            .ToList();

        if (!drivers.Any()) return;

        const int totalSeats = 20;

        var tripsToAdd = new List<ShuttleTrip>();

        int driverIndex = 0;

        //  start from 7 to 5 - to the metro
        var time = new TimeOnly(7, 0);
        var endTime = new TimeOnly(17, 0);

        while (time <= endTime)
        {
            // from metro to uni  from 7-4
            if (time >= new TimeOnly(7, 0) && time <= new TimeOnly(16, 0))
            {
                tripsToAdd.Add(new ShuttleTrip
                {
                    trip_date = today,
                    departure_time = time,
                    arrival_time = time.AddMinutes(20),
                    total_seats = totalSeats,
                    available_seats = totalSeats,
                    driver_id = drivers[driverIndex].driver_id,
                    status = "Scheduled",
                    metro_id = metro.metro_id,
                    direction_id = 1
                });

                driverIndex = (driverIndex + 1) % drivers.Count;
            }

            // from uni to metro  from 8-5
            if (time >= new TimeOnly(8, 0) && time <= new TimeOnly(17, 0))
            {
                tripsToAdd.Add(new ShuttleTrip
                {
                    trip_date = today,
                    departure_time = time,
                    arrival_time = time.AddMinutes(20),
                    total_seats = totalSeats,
                    available_seats = totalSeats,
                    driver_id = drivers[driverIndex].driver_id,
                    status = "Scheduled",
                    metro_id = metro.metro_id,
                    direction_id = 2
                });

                driverIndex = (driverIndex + 1) % drivers.Count;
            }

            time = time.AddMinutes(20);
        }

        _context.ShuttleTrip.AddRange(tripsToAdd);
        _context.SaveChanges();
    }

    private List<ShuttleTrip> GenerateTripsForDirection(
    DateOnly tripDate,
    int directionId,
    TimeOnly startTime,
    TimeOnly endTime,
    int intervalMinutes,
    int tripDurationMinutes,
    int metroId,
    int totalSeats,
    List<Driver> drivers,
    ref int driverIndex
)
    {
        var trips = new List<ShuttleTrip>();
        var currentDeparture = startTime;

        while (currentDeparture <= endTime)
        {
            var arrival = currentDeparture.AddMinutes(tripDurationMinutes);

            var assignedDriverId = drivers[driverIndex].driver_id;

            trips.Add(new ShuttleTrip
            {
                trip_date = tripDate,
                departure_time = currentDeparture,
                arrival_time = arrival,
                total_seats = totalSeats,
                available_seats = totalSeats,
                driver_id = assignedDriverId,
                status = "Scheduled",
                metro_id = metroId,
                direction_id = directionId
            });

            
            driverIndex = (driverIndex + 1) % drivers.Count;

            currentDeparture = currentDeparture.AddMinutes(intervalMinutes);
        }

        return trips;
    }
}
