using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniBusApp.Data;
using UniBusApp.Models;

namespace UniBusApp.Services;

public class TripTrackingService
{
    private const string DefaultGoogleMapsApiKey = "AIzaSyCJxROuarobnKCcMQ8dE90UwPghldU0hhY";
    private const double EarthRadiusMeters = 6371000;
    private const double OffRouteThresholdMeters = 1000;
    private const double StopArrivalThresholdMeters = 75;
    private const double FallbackSpeedKmH = 30;
    private const int MaxDirectionsWaypoints = 23;
    private const int MaxDirectionsAttempts = 2;
    private static readonly TimeSpan LiveLocationMaxAge = TimeSpan.FromMinutes(2);

    private readonly UniBusDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _googleMapsApiKey;
    private readonly ILogger<TripTrackingService> _logger;
    private readonly ILogger<RouteOptimizer> _routeOptimizerLogger;

    public TripTrackingService(
        UniBusDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _googleMapsApiKey = configuration["GoogleMaps:ApiKey"] ?? DefaultGoogleMapsApiKey;
        _logger = loggerFactory.CreateLogger<TripTrackingService>();
        _routeOptimizerLogger = loggerFactory.CreateLogger<RouteOptimizer>();
    }

    public string GoogleMapsApiKey => _googleMapsApiKey;

    public async Task<TripTrackingSnapshot?> BuildTrackingSnapshotAsync(int tripId)
    {
        var routeContext = await LoadRouteContextAsync(tripId);
        if (routeContext == null)
        {
            return null;
        }

        return await BuildTrackingSnapshotAsync(routeContext);
    }

    public async Task<StudentStopEta?> BuildStudentStopEtaAsync(int tripId, int studentBuildingId)
    {
        var routeContext = await LoadRouteContextAsync(tripId);
        if (routeContext == null)
        {
            return null;
        }

        var algorithmRoute = await BuildAlgorithmRouteAsync(routeContext.Trip, routeContext.RequestedBuildings);
        var liveBusLocation = IsTripStarted(routeContext.Trip.status)
            ? await GetLiveBusLocationAsync(routeContext.Trip.trip_id)
            : null;

        return await BuildStudentStopEtaAsync(routeContext.Trip, algorithmRoute, studentBuildingId, liveBusLocation);
    }

    public async Task<LiveBusLocation?> GetLiveBusLocationAsync(int tripId)
    {
        var location = await GetLatestLocationEntityAsync(tripId);

        if (location == null)
        {
            return null;
        }

        return ToLiveBusLocation(location);
    }

    private async Task<TripTrackingSnapshot> BuildTrackingSnapshotAsync(TripRouteContext routeContext)
    {
        var algorithmRoute = await BuildAlgorithmRouteAsync(routeContext.Trip, routeContext.RequestedBuildings);
        var googleRoute = await BuildGoogleRouteAsync(algorithmRoute.OptimizedRoute);
        var liveBusLocation = await GetLiveBusLocationAsync(routeContext.Trip.trip_id);
        await PopulateRouteEtaMetadataAsync(routeContext.Trip, algorithmRoute, liveBusLocation);

        if (!googleRoute.HasRoadPolyline)
        {
            _logger.LogWarning(
                "Trip {TripId} has no Google road polyline. Markers and live tracking will continue without drawing straight fallback lines. Reason: {Reason}",
                routeContext.Trip.trip_id,
                googleRoute.FailureReason ?? "Unknown Google Directions failure");
        }

        _logger.LogInformation(
            "Trip {TripId} tracking snapshot polyline length: {PolylineLength}. Route order source remains RouteOptimizer.",
            routeContext.Trip.trip_id,
            googleRoute.Polyline?.Length ?? 0);

        return new TripTrackingSnapshot
        {
            TripId = routeContext.Trip.trip_id,
            Status = routeContext.Trip.status,
            DirectionName = routeContext.Trip.direction_id == 1 ? "Metro to University" : "University to Metro",
            DepartureTime = routeContext.Trip.departure_time.ToString(@"HH\:mm"),
            DriverName = routeContext.Trip.Driver?.driver_name ?? "No driver assigned",
            BusPlate = routeContext.Trip.Driver?.bus_plate ?? "N/A",
            Passengers = routeContext.Trip.total_seats - routeContext.Trip.available_seats,
            Capacity = routeContext.Trip.total_seats,
            StartLocation = algorithmRoute.StartLocation,
            EndLocation = algorithmRoute.EndLocation,
            Stops = algorithmRoute.Stops,
            OptimizedRoute = algorithmRoute.OptimizedRoute,
            EstimatedArrivalTime = algorithmRoute.EndLocation?.EstimatedArrivalTime ?? algorithmRoute.EstimatedArrivalTime,
            Distance = googleRoute.Distance,
            Duration = googleRoute.Duration,
            HasRoadPolyline = googleRoute.HasRoadPolyline,
            PolylineFailureReason = googleRoute.FailureReason,
            Polyline = googleRoute.Polyline,
            LiveBusLocation = liveBusLocation
        };
    }

    private async Task<TripRouteContext?> LoadRouteContextAsync(int tripId)
    {
        var trip = await _db.ShuttleTrip
            .Include(t => t.Metro)
            .Include(t => t.Driver)
            .Include(t => t.Bookings)
                .ThenInclude(b => b.Student)
                    .ThenInclude(s => s.Building)
            .FirstOrDefaultAsync(t => t.trip_id == tripId);

        if (trip == null)
        {
            return null;
        }

        var requestedBuildings = trip.Bookings
            .Where(b => string.Equals(b.booking_status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(b.booking_status, "Booked", StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Student?.Building)
            .Where(b => b != null)
            .GroupBy(b => b!.building_id)
            .Select(g => g.First()!)
            .OrderBy(b => b.building_id)
            .ToList();

        return new TripRouteContext
        {
            Trip = trip,
            RequestedBuildings = requestedBuildings
        };
    }

    private async Task<AlgorithmRouteSummary> BuildAlgorithmRouteAsync(ShuttleTrip trip, List<Building> requestedBuildings)
    {
        _logger.LogInformation(
            "Building optimized route for trip {TripId}. Direction {DirectionId}. Requested pickup buildings: {BuildingCount}.",
            trip.trip_id,
            trip.direction_id,
            requestedBuildings.Count);

        var optimizer = new RouteOptimizer(_googleMapsApiKey, _routeOptimizerLogger);
        var metroPoint = new LocationPoint
        {
            Id = trip.Metro.metro_id,
            Name = trip.Metro.station_name,
            Lat = (double)trip.Metro.latitude,
            Lng = (double)trip.Metro.longitude,
            IsBuilding = false
        };

        if (!requestedBuildings.Any())
        {
            return BuildAlgorithmSummary(trip, new List<AlgorithmRouteNode>
            {
                new()
                {
                    Point = metroPoint,
                    CumulativeDurationSeconds = 0
                }
            });
        }

        var buildingPoints = requestedBuildings
            .Select(b => new LocationPoint
            {
                Id = b.building_id,
                BuildingId = b.building_id,
                Name = b.building_name,
                Lat = (double)b.latitude,
                Lng = (double)b.longitude,
                IsBuilding = true
            })
            .ToList();

        List<LocationPoint> optimizedRoutePoints;

        if (trip.direction_id == 1)
        {
            var allPoints = new List<LocationPoint> { metroPoint };
            allPoints.AddRange(buildingPoints);

            var distanceMatrix = await optimizer.GetDistanceMatrixFromGoogle(allPoints);
            optimizedRoutePoints = optimizer.Optimize(metroPoint, buildingPoints, distanceMatrix);
        }
        else
        {
            List<LocationPoint> optimizedStops;

            if (buildingPoints.Count == 1)
            {
                optimizedStops = new List<LocationPoint> { buildingPoints[0] };
            }
            else
            {
                var buildingMatrix = await optimizer.GetDistanceMatrixFromGoogle(buildingPoints);
                optimizedStops = optimizer.Optimize(buildingPoints[0], buildingPoints.Skip(1).ToList(), buildingMatrix);
            }

            optimizedRoutePoints = new List<LocationPoint>(optimizedStops) { metroPoint };
        }

        var routeMatrix = optimizedRoutePoints.Count > 1
            ? await optimizer.GetDistanceMatrixFromGoogle(optimizedRoutePoints)
            : new double[0, 0];

        var routeNodes = BuildRouteNodes(optimizedRoutePoints, routeMatrix);
        var algorithmSummary = BuildAlgorithmSummary(trip, routeNodes);

        _logger.LogInformation(
            "Trip {TripId} route optimizer order: {RouteOrder}",
            trip.trip_id,
            FormatRouteOrder(algorithmSummary.OptimizedRoute));

        return algorithmSummary;
    }

    private AlgorithmRouteSummary BuildAlgorithmSummary(ShuttleTrip trip, List<AlgorithmRouteNode> routeNodes)
    {
        var optimizedRoute = new List<TrackingPoint>();
        var nextStopOrder = 1;

        for (var index = 0; index < routeNodes.Count; index++)
        {
            var isFinalDestination = index == routeNodes.Count - 1;
            var isPickupStop = !isFinalDestination && routeNodes[index].Point.IsBuilding;
            var markerLabel = isFinalDestination
                ? "E"
                : isPickupStop
                    ? $"S{nextStopOrder}"
                    : null;
            var type = isFinalDestination
                ? "end"
                : isPickupStop
                    ? "stop"
                    : "start";
            var stopOrder = isPickupStop ? (int?)nextStopOrder : null;

            optimizedRoute.Add(ToTrackingPoint(trip, routeNodes[index], type, stopOrder, markerLabel));

            if (isPickupStop)
            {
                nextStopOrder++;
            }
        }

        // Only pickup stops receive S-labels; the final destination alone receives E.
        var startLocation = optimizedRoute.First();
        var endLocation = optimizedRoute.Last();
        var stops = optimizedRoute
            .Where(point => point.StopOrder.HasValue)
            .ToList();

        return new AlgorithmRouteSummary
        {
            StartLocation = startLocation,
            EndLocation = endLocation,
            Stops = stops,
            OptimizedRoute = optimizedRoute,
            EstimatedArrivalTime = endLocation.EstimatedArrivalTime,
            RouteNodes = routeNodes
        };
    }

    private async Task<GoogleRouteSummary> BuildGoogleRouteAsync(List<TrackingPoint> optimizedRoute)
    {
        if (optimizedRoute.Count < 2 || string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            return new GoogleRouteSummary
            {
                FailureReason = optimizedRoute.Count < 2
                    ? "Directions skipped because the route has fewer than 2 points."
                    : "Directions skipped because the Google Maps API key is missing."
            };
        }

        var origin = optimizedRoute.First();
        var destination = optimizedRoute.Last();
        var waypoints = optimizedRoute.Skip(1).Take(Math.Max(optimizedRoute.Count - 2, 0)).ToList();
        string? originValidationError = null;
        string? destinationValidationError = null;

        if (waypoints.Count > MaxDirectionsWaypoints)
        {
            return new GoogleRouteSummary
            {
                FailureReason = $"Directions skipped because the route has {waypoints.Count} waypoints, exceeding Google's limit of {MaxDirectionsWaypoints}."
            };
        }

        if (!TryValidateTrackingPoint(origin, out originValidationError)
            || !TryValidateTrackingPoint(destination, out destinationValidationError)
            || waypoints.Any(point => !TryValidateTrackingPoint(point, out _)))
        {
            return new GoogleRouteSummary
            {
                FailureReason = originValidationError
                    ?? destinationValidationError
                    ?? "Directions skipped because one or more waypoint coordinates are invalid."
            };
        }

        var orderedRoute = new List<TrackingPoint> { origin };
        orderedRoute.AddRange(waypoints);
        orderedRoute.Add(destination);
        var routeOrder = FormatRouteOrder(orderedRoute);
        var url = BuildDirectionsUrl(origin, destination, waypoints);
        var sanitizedUrl = SanitizeGoogleDirectionsUrl(url);
        string failureReason = "Google Directions did not return a usable road polyline.";

        for (var attempt = 1; attempt <= MaxDirectionsAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Google Directions request attempt {Attempt}/{MaxAttempts}. Route order: {RouteOrder}. Request URL: {RequestUrl}",
                    attempt,
                    MaxDirectionsAttempts,
                    routeOrder,
                    sanitizedUrl);

                using var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    failureReason = $"Google Directions HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
                    _logger.LogWarning(
                        "Google Directions failed for trip route order {RouteOrder}. Attempt {Attempt}. {FailureReason}",
                        routeOrder,
                        attempt,
                        failureReason);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);
                var status = document.RootElement.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString()
                    : null;
                var routesCount = document.RootElement.TryGetProperty("routes", out var rawRoutesElement)
                    ? rawRoutesElement.GetArrayLength()
                    : 0;

                if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    var errorMessage = document.RootElement.TryGetProperty("error_message", out var errorMessageElement)
                        ? errorMessageElement.GetString()
                        : null;
                    failureReason = string.IsNullOrWhiteSpace(errorMessage)
                        ? $"Google Directions returned status '{status ?? "Unknown"}'."
                        : $"Google Directions returned status '{status ?? "Unknown"}': {errorMessage}";

                    _logger.LogWarning(
                        "Google Directions status failure for route order {RouteOrder}. Attempt {Attempt}. Status {Status}. Routes {RoutesCount}. {FailureReason}",
                        routeOrder,
                        attempt,
                        status ?? "Unknown",
                        routesCount,
                        failureReason);
                    continue;
                }

                if (!document.RootElement.TryGetProperty("routes", out var routesElement)
                    || routesCount == 0)
                {
                    failureReason = "Google Directions returned no routes.";
                    _logger.LogWarning(
                        "Google Directions returned no routes for route order {RouteOrder}. Attempt {Attempt}. Status {Status}.",
                        routeOrder,
                        attempt,
                        status ?? "Unknown");
                    continue;
                }

                var firstRoute = routesElement[0];
                var polyline = firstRoute.TryGetProperty("overview_polyline", out var overviewPolylineElement)
                    && overviewPolylineElement.TryGetProperty("points", out var pointsElement)
                    ? pointsElement.GetString()
                    : null;
                var hasOverviewPolyline = !string.IsNullOrWhiteSpace(polyline);

                _logger.LogInformation(
                    "Google Directions response parsed for route order {RouteOrder}. Status {Status}. Routes {RoutesCount}. Overview polyline present: {HasOverviewPolyline}.",
                    routeOrder,
                    status ?? "Unknown",
                    routesCount,
                    hasOverviewPolyline);

                if (!hasOverviewPolyline)
                {
                    failureReason = "Google Directions returned a route without an overview polyline.";
                    _logger.LogWarning(
                        "Google Directions returned no overview polyline for route order {RouteOrder}. Attempt {Attempt}. Status {Status}. Routes {RoutesCount}.",
                        routeOrder,
                        attempt,
                        status ?? "Unknown",
                        routesCount);
                    continue;
                }

                var totalDistance = 0;
                var totalDuration = 0;

                foreach (var leg in firstRoute.GetProperty("legs").EnumerateArray())
                {
                    if (leg.TryGetProperty("distance", out var distanceElement)
                        && distanceElement.TryGetProperty("value", out var distanceValueElement))
                    {
                        totalDistance += distanceValueElement.GetInt32();
                    }

                    if (leg.TryGetProperty("duration", out var durationElement)
                        && durationElement.TryGetProperty("value", out var durationValueElement))
                    {
                        totalDuration += durationValueElement.GetInt32();
                    }
                }

                _logger.LogInformation(
                    "Google Directions polyline generated successfully for route order {RouteOrder}. Distance {DistanceMeters}m, duration {DurationSeconds}s. Polyline length: {PolylineLength}.",
                    routeOrder,
                    totalDistance,
                    totalDuration,
                    polyline!.Length);

                return new GoogleRouteSummary
                {
                    Polyline = polyline,
                    Distance = new GoogleRouteMetric
                    {
                        Text = FormatDistance(totalDistance),
                        Value = totalDistance
                    },
                    Duration = new GoogleRouteMetric
                    {
                        Text = FormatDuration(totalDuration),
                        Value = totalDuration
                    }
                };
            }
            catch (Exception ex)
            {
                failureReason = $"Google Directions request failed: {ex.Message}";
                _logger.LogWarning(
                    ex,
                    "Google Directions request failed for route order {RouteOrder}. Attempt {Attempt}.",
                    routeOrder,
                    attempt);
            }
        }

        return new GoogleRouteSummary
        {
            FailureReason = failureReason
        };
    }

    private static List<AlgorithmRouteNode> BuildRouteNodes(List<LocationPoint> routePoints, double[,] matrix)
    {
        var routeNodes = new List<AlgorithmRouteNode>();
        var cumulativeDurationSeconds = 0;

        for (var index = 0; index < routePoints.Count; index++)
        {
            if (index > 0 && matrix.Length > 0)
            {
                cumulativeDurationSeconds += Math.Max(0, (int)Math.Round(matrix[index - 1, index]));
            }

            routeNodes.Add(new AlgorithmRouteNode
            {
                Point = routePoints[index],
                CumulativeDurationSeconds = cumulativeDurationSeconds
            });
        }

        return routeNodes;
    }

    private async Task PopulateRouteEtaMetadataAsync(
        ShuttleTrip trip,
        AlgorithmRouteSummary algorithmRoute,
        LiveBusLocation? liveBusLocation)
    {
        for (var index = 0; index < algorithmRoute.RouteNodes.Count; index++)
        {
            var routePointEta = await ResolveRoutePointEtaAsync(
                trip,
                algorithmRoute.RouteNodes,
                index,
                liveBusLocation);

            var point = algorithmRoute.OptimizedRoute[index];
            point.EstimatedArrivalTime = routePointEta.EstimatedArrivalTime;
            point.EtaMinutes = routePointEta.RemainingMinutes;
            point.IsArrivingSoon = routePointEta.IsArrivingSoon;
        }

        algorithmRoute.EstimatedArrivalTime = algorithmRoute.EndLocation?.EstimatedArrivalTime;
    }

    private async Task<StudentStopEta?> BuildStudentStopEtaAsync(
        ShuttleTrip trip,
        AlgorithmRouteSummary algorithmRoute,
        int studentBuildingId,
        LiveBusLocation? liveBusLocation)
    {
        var studentRouteIndex = FindStudentRouteIndex(algorithmRoute.RouteNodes, studentBuildingId);
        if (studentRouteIndex < 0)
        {
            return null;
        }

        var studentPoint = algorithmRoute.OptimizedRoute[studentRouteIndex];
        var routePointEta = await ResolveRoutePointEtaAsync(
            trip,
            algorithmRoute.RouteNodes,
            studentRouteIndex,
            liveBusLocation);

        return new StudentStopEta
        {
            BuildingId = studentBuildingId,
            StopLabel = studentPoint.MarkerLabel,
            StopName = studentPoint.Name,
            EstimatedArrivalTime = routePointEta.EstimatedArrivalTime,
            RemainingMinutes = routePointEta.RemainingMinutes,
            IsArrivingSoon = routePointEta.IsArrivingSoon
        };
    }

    private async Task<Location?> GetLatestLocationEntityAsync(int tripId)
    {
        var location = await _db.Location
            .Where(l => l.trip_id == tripId && l.latitude.HasValue && l.longitude.HasValue)
            .OrderByDescending(l => l.timestamp ?? DateTime.MinValue)
            .ThenByDescending(l => l.location_id)
            .FirstOrDefaultAsync();

        if (location?.timestamp == null)
        {
            return null;
        }

        return IsLocationFresh(location.timestamp.Value)
            ? location
            : null;
    }

    private static LiveBusLocation ToLiveBusLocation(Location location)
    {
        return new LiveBusLocation
        {
            Lat = (double)location.latitude!.Value,
            Lng = (double)location.longitude!.Value,
            Timestamp = location.timestamp.HasValue
                ? ToIsoString(location.timestamp.Value)
                : null
        };
    }

    private static bool IsLocationFresh(DateTime timestamp)
    {
        return DateTime.Now - timestamp <= LiveLocationMaxAge;
    }

    private static bool IsTripStarted(string? status)
    {
        return string.Equals(status, "Started", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int?> EstimateTravelDurationSecondsAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        if (!string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            var originText = FormatCoordinate(originLat, originLng);
            var destinationText = FormatCoordinate(destinationLat, destinationLng);
            var url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={originText}&destinations={destinationText}&mode=driving&key={_googleMapsApiKey}";

            try
            {
                using var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    using var document = await JsonDocument.ParseAsync(stream);

                    if (document.RootElement.TryGetProperty("status", out var statusElement)
                        && string.Equals(statusElement.GetString(), "OK", StringComparison.OrdinalIgnoreCase)
                        && document.RootElement.TryGetProperty("rows", out var rowsElement)
                        && rowsElement.GetArrayLength() > 0
                        && rowsElement[0].TryGetProperty("elements", out var elementsElement)
                        && TryGetDurationSeconds(elementsElement, 0, out var durationSeconds))
                    {
                        return Math.Max(60, (int)Math.Ceiling(durationSeconds));
                    }
                }
            }
            catch
            {
                // Fall back to a local estimate when Google APIs are unavailable.
            }
        }

        return EstimateFallbackTravelDurationSeconds(originLat, originLng, destinationLat, destinationLng);
    }

    private static int FindStudentRouteIndex(List<AlgorithmRouteNode> routeNodes, int studentBuildingId)
    {
        return routeNodes.FindIndex(node =>
            node.Point.IsBuilding
            && node.Point.BuildingId == studentBuildingId);
    }

    private async Task<RoutePointEta> ResolveRoutePointEtaAsync(
        ShuttleTrip trip,
        List<AlgorithmRouteNode> routeNodes,
        int destinationIndex,
        LiveBusLocation? liveBusLocation)
    {
        var destinationNode = routeNodes[destinationIndex];

        // When live GPS is available, project the bus onto the optimized route first so
        // every stop keeps an ETA relative to its actual position in the route order.
        if (liveBusLocation != null && IsTripStarted(trip.status))
        {
            var remainingSeconds = EstimateRemainingDurationSecondsFromLiveLocation(
                liveBusLocation,
                routeNodes,
                destinationIndex);

            if (remainingSeconds.HasValue)
            {
                var isNearDestination = IsNearRoutePoint(liveBusLocation, destinationNode.Point);
                if (remainingSeconds.Value > 0 || isNearDestination)
                {
                    return BuildLiveRoutePointEta(liveBusLocation, destinationNode.Point, remainingSeconds.Value);
                }
            }

            var directTravelSeconds = await EstimateTravelDurationSecondsAsync(
                liveBusLocation.Lat,
                liveBusLocation.Lng,
                destinationNode.Point.Lat,
                destinationNode.Point.Lng);

            if (directTravelSeconds.HasValue)
            {
                return BuildLiveRoutePointEta(liveBusLocation, destinationNode.Point, directTravelSeconds.Value);
            }
        }

        return BuildScheduledRoutePointEta(trip, destinationNode.CumulativeDurationSeconds);
    }

    private static RoutePointEta BuildLiveRoutePointEta(
        LiveBusLocation liveBusLocation,
        LocationPoint destinationPoint,
        int remainingSeconds)
    {
        var isArrivingSoon = IsNearRoutePoint(liveBusLocation, destinationPoint);
        var safeRemainingSeconds = Math.Max(0, remainingSeconds);

        if (!isArrivingSoon && safeRemainingSeconds == 0)
        {
            safeRemainingSeconds = 60;
        }

        var estimatedArrival = DateTime.Now.AddSeconds(safeRemainingSeconds);

        return new RoutePointEta
        {
            EstimatedArrivalTime = ToIsoString(estimatedArrival),
            RemainingMinutes = isArrivingSoon
                ? 0
                : Math.Max(1, (int)Math.Ceiling(safeRemainingSeconds / 60d)),
            IsArrivingSoon = isArrivingSoon
        };
    }

    private static RoutePointEta BuildScheduledRoutePointEta(
        ShuttleTrip trip,
        int cumulativeDurationSeconds)
    {
        var estimatedArrival = CalculateEstimatedArrivalDateTime(trip, cumulativeDurationSeconds);

        return new RoutePointEta
        {
            EstimatedArrivalTime = ToIsoString(estimatedArrival),
            RemainingMinutes = CalculateRemainingMinutes(estimatedArrival),
            IsArrivingSoon = false
        };
    }

    private static bool IsNearRoutePoint(LiveBusLocation liveBusLocation, LocationPoint destinationPoint)
    {
        var distanceToStopMeters = CalculateDistanceMeters(
            liveBusLocation.Lat,
            liveBusLocation.Lng,
            destinationPoint.Lat,
            destinationPoint.Lng);

        return distanceToStopMeters <= StopArrivalThresholdMeters;
    }

    private static int? EstimateRemainingDurationSecondsFromLiveLocation(
        LiveBusLocation liveBusLocation,
        List<AlgorithmRouteNode> routeNodes,
        int destinationIndex)
    {
        if (destinationIndex < 0)
        {
            return null;
        }

        if (routeNodes.Count == 1)
        {
            return 0;
        }

        var projection = ProjectOntoRoute(liveBusLocation, routeNodes);
        if (projection == null || projection.DistanceToRouteMeters > OffRouteThresholdMeters)
        {
            return null;
        }

        var destinationSeconds = routeNodes[destinationIndex].CumulativeDurationSeconds;
        var remainingSeconds = destinationSeconds - projection.ProgressSeconds;
        return Math.Max(0, (int)Math.Ceiling(remainingSeconds));
    }

    private static int EstimateFallbackTravelDurationSeconds(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        var distanceMeters = CalculateDistanceMeters(originLat, originLng, destinationLat, destinationLng);
        if (distanceMeters <= 0)
        {
            return 60;
        }

        var metersPerSecond = (FallbackSpeedKmH * 1000) / 3600d;
        var durationSeconds = distanceMeters / metersPerSecond;
        return Math.Max(60, (int)Math.Ceiling(durationSeconds));
    }

    private static bool TryGetDurationSeconds(JsonElement elements, int index, out double durationSeconds)
    {
        durationSeconds = 0;

        if (index >= elements.GetArrayLength())
        {
            return false;
        }

        var element = elements[index];

        if (!element.TryGetProperty("status", out var statusElement)
            || !string.Equals(statusElement.GetString(), "OK", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!element.TryGetProperty("duration", out var durationElement)
            || !durationElement.TryGetProperty("value", out var valueElement))
        {
            return false;
        }

        durationSeconds = valueElement.GetDouble();
        return durationSeconds >= 0;
    }

    private static RouteProjection? ProjectOntoRoute(
        LiveBusLocation liveBusLocation,
        List<AlgorithmRouteNode> routeNodes)
    {
        if (routeNodes.Count == 0)
        {
            return null;
        }

        if (routeNodes.Count == 1)
        {
            var onlyPoint = routeNodes[0].Point;

            return new RouteProjection
            {
                ProgressSeconds = 0,
                DistanceToRouteMeters = CalculateDistanceMeters(
                    liveBusLocation.Lat,
                    liveBusLocation.Lng,
                    onlyPoint.Lat,
                    onlyPoint.Lng)
            };
        }

        RouteProjection? bestProjection = null;

        for (var index = 0; index < routeNodes.Count - 1; index++)
        {
            var startNode = routeNodes[index];
            var endNode = routeNodes[index + 1];
            var segmentProjection = ProjectPointOntoSegment(
                liveBusLocation.Lat,
                liveBusLocation.Lng,
                startNode.Point.Lat,
                startNode.Point.Lng,
                endNode.Point.Lat,
                endNode.Point.Lng);

            var segmentDuration = Math.Max(0, endNode.CumulativeDurationSeconds - startNode.CumulativeDurationSeconds);
            var progressSeconds = startNode.CumulativeDurationSeconds + (segmentProjection.PositionRatio * segmentDuration);
            var candidate = new RouteProjection
            {
                ProgressSeconds = progressSeconds,
                DistanceToRouteMeters = segmentProjection.DistanceToSegmentMeters
            };

            if (bestProjection == null || candidate.DistanceToRouteMeters < bestProjection.DistanceToRouteMeters)
            {
                bestProjection = candidate;
            }
        }

        return bestProjection;
    }

    private static SegmentProjection ProjectPointOntoSegment(
        double pointLat,
        double pointLng,
        double startLat,
        double startLng,
        double endLat,
        double endLng)
    {
        var referenceLat = (pointLat + startLat + endLat) / 3d;
        var point = ToPlanarPoint(pointLat, pointLng, referenceLat);
        var start = ToPlanarPoint(startLat, startLng, referenceLat);
        var end = ToPlanarPoint(endLat, endLng, referenceLat);

        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var segmentLengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

        if (segmentLengthSquared <= 0)
        {
            return new SegmentProjection
            {
                PositionRatio = 0,
                DistanceToSegmentMeters = CalculateDistanceMeters(pointLat, pointLng, startLat, startLng)
            };
        }

        var projectedRatio = ((point.X - start.X) * deltaX + (point.Y - start.Y) * deltaY) / segmentLengthSquared;
        projectedRatio = Math.Clamp(projectedRatio, 0, 1);

        var projectedX = start.X + (projectedRatio * deltaX);
        var projectedY = start.Y + (projectedRatio * deltaY);
        var distanceToSegmentMeters = Math.Sqrt(
            Math.Pow(point.X - projectedX, 2) + Math.Pow(point.Y - projectedY, 2));

        return new SegmentProjection
        {
            PositionRatio = projectedRatio,
            DistanceToSegmentMeters = distanceToSegmentMeters
        };
    }

    private static PlanarPoint ToPlanarPoint(double lat, double lng, double referenceLat)
    {
        var referenceLatRadians = ToRadians(referenceLat);

        return new PlanarPoint
        {
            X = EarthRadiusMeters * ToRadians(lng) * Math.Cos(referenceLatRadians),
            Y = EarthRadiusMeters * ToRadians(lat)
        };
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double angle)
    {
        return angle * Math.PI / 180;
    }

    private static TrackingPoint ToTrackingPoint(
        ShuttleTrip trip,
        AlgorithmRouteNode routeNode,
        string type,
        int? stopOrder = null,
        string? markerLabel = null)
    {
        return new TrackingPoint
        {
            Id = routeNode.Point.Id,
            BuildingId = routeNode.Point.BuildingId,
            Name = routeNode.Point.Name,
            Lat = routeNode.Point.Lat,
            Lng = routeNode.Point.Lng,
            StopOrder = stopOrder,
            MarkerLabel = markerLabel,
            Type = type,
            EstimatedArrivalTime = BuildEstimatedArrivalTime(trip, routeNode.CumulativeDurationSeconds)
        };
    }

    private static string? BuildEstimatedArrivalTime(ShuttleTrip trip, int durationSeconds)
    {
        return ToIsoString(CalculateEstimatedArrivalDateTime(trip, durationSeconds));
    }

    private static DateTime CalculateEstimatedArrivalDateTime(ShuttleTrip trip, int durationSeconds)
    {
        var baseDateTime = trip.started_at ?? trip.trip_date.ToDateTime(trip.departure_time);
        return baseDateTime.AddSeconds(durationSeconds);
    }

    private static int CalculateRemainingMinutes(DateTime estimatedArrival)
    {
        var remaining = estimatedArrival - DateTime.Now;
        return Math.Max(0, (int)Math.Ceiling(remaining.TotalMinutes));
    }

    private string BuildDirectionsUrl(TrackingPoint origin, TrackingPoint destination, List<TrackingPoint> waypoints)
    {
        var originText = FormatCoordinate(origin.Lat, origin.Lng);
        var destinationText = FormatCoordinate(destination.Lat, destination.Lng);
        var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={originText}&destination={destinationText}&mode=driving&key={_googleMapsApiKey}";

        if (waypoints.Count > 0)
        {
            var waypointText = string.Join("|", waypoints.Select(w => FormatCoordinate(w.Lat, w.Lng)));
            url += $"&waypoints={Uri.EscapeDataString(waypointText)}";
        }

        return url;
    }

    private static string SanitizeGoogleDirectionsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var keyIndex = url.IndexOf("&key=", StringComparison.OrdinalIgnoreCase);
        return keyIndex >= 0
            ? $"{url[..(keyIndex + 5)]}REDACTED"
            : url;
    }

    private static bool TryValidateTrackingPoint(TrackingPoint point, out string? validationError)
    {
        validationError = null;

        if (!IsValidCoordinate(point.Lat, point.Lng))
        {
            validationError = $"Directions skipped because '{point.Name}' has invalid coordinates ({point.Lat}, {point.Lng}).";
            return false;
        }

        return true;
    }

    private static string FormatCoordinate(double lat, double lng)
    {
        return $"{lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string FormatRouteOrder(IEnumerable<TrackingPoint> route)
    {
        return string.Join(" -> ", route.Select(point =>
        {
            if (!string.IsNullOrWhiteSpace(point.MarkerLabel))
            {
                return $"{point.MarkerLabel}:{point.Name}";
            }

            return $"{point.Type}:{point.Name}";
        }));
    }

    private static string FormatDistance(int meters)
    {
        return meters >= 1000
            ? $"{meters / 1000d:0.0} km"
            : $"{meters} m";
    }

    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);

        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours} hr {duration.Minutes} min";
        }

        return $"{Math.Max(1, duration.Minutes)} min";
    }

    private static string ToIsoString(DateTime dateTime)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
        return new DateTimeOffset(dateTime, offset).ToString("O");
    }

    private static bool IsValidCoordinate(double lat, double lng)
    {
        return !double.IsNaN(lat)
            && !double.IsInfinity(lat)
            && !double.IsNaN(lng)
            && !double.IsInfinity(lng)
            && lat >= -90
            && lat <= 90
            && lng >= -180
            && lng <= 180;
    }

    private sealed class TripRouteContext
    {
        public ShuttleTrip Trip { get; set; } = null!;

        public List<Building> RequestedBuildings { get; set; } = new();
    }

    private sealed class AlgorithmRouteNode
    {
        public LocationPoint Point { get; set; } = null!;

        public int CumulativeDurationSeconds { get; set; }
    }

    private sealed class AlgorithmRouteSummary
    {
        public TrackingPoint? StartLocation { get; set; }

        public TrackingPoint? EndLocation { get; set; }

        public List<TrackingPoint> Stops { get; set; } = new();

        public List<TrackingPoint> OptimizedRoute { get; set; } = new();

        public string? EstimatedArrivalTime { get; set; }

        public List<AlgorithmRouteNode> RouteNodes { get; set; } = new();
    }

    private sealed class GoogleRouteSummary
    {
        public GoogleRouteMetric? Distance { get; set; }

        public GoogleRouteMetric? Duration { get; set; }

        public bool HasRoadPolyline => !string.IsNullOrWhiteSpace(Polyline);

        public string? FailureReason { get; set; }

        public string? Polyline { get; set; }
    }

    private sealed class RoutePointEta
    {
        public string? EstimatedArrivalTime { get; set; }

        public int RemainingMinutes { get; set; }

        public bool IsArrivingSoon { get; set; }
    }

    private sealed class RouteProjection
    {
        public double ProgressSeconds { get; set; }

        public double DistanceToRouteMeters { get; set; }
    }

    private sealed class SegmentProjection
    {
        public double PositionRatio { get; set; }

        public double DistanceToSegmentMeters { get; set; }
    }

    private sealed class PlanarPoint
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}
