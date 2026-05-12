using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UniBusApp.Services
{
    public class LocationPoint
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int? BuildingId { get; set; }
        public bool IsBuilding { get; set; }
    }

    public class RouteOptimizer
    {
        private const double EarthRadiusKm = 6371;
        private const double FallbackSpeedKmH = 30;
        private const int VnsIterations = 60;

        private readonly string _apiKey;
        private readonly ILogger<RouteOptimizer>? _logger;

        public RouteOptimizer(string? apiKey = null, ILogger<RouteOptimizer>? logger = null)
        {
            _apiKey = apiKey ?? string.Empty;
            _logger = logger;
        }

        // 1.Getting the distance matrix from google
        public async Task<double[,]> GetDistanceMatrixFromGoogle(List<LocationPoint> points)
        {
            if (points == null || points.Count < 2) return new double[0, 0];

            var matrix = CreateFallbackMatrix(points);
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger?.LogWarning("RouteOptimizer is using fallback travel-time estimates because the Google API key is missing.");
                return matrix;
            }

            var coords = string.Join("|", points.Select(p =>
                $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lng.ToString(CultureInfo.InvariantCulture)}"));
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={coords}&destinations={coords}&key={_apiKey}";

            try
            {
                _logger?.LogInformation(
                    "Requesting Google Distance Matrix for {PointCount} points.",
                    points.Count);

                using var client = new HttpClient();
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "Google Distance Matrix returned HTTP {StatusCode}. Falling back to local estimates.",
                        (int)response.StatusCode);
                    return matrix;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);

                if (!document.RootElement.TryGetProperty("status", out var statusElement)
                    || !string.Equals(statusElement.GetString(), "OK", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning(
                        "Google Distance Matrix returned status '{Status}'. Falling back to local estimates.",
                        statusElement.GetString());
                    return matrix;
                }

                if (!document.RootElement.TryGetProperty("rows", out var rows))
                {
                    _logger?.LogWarning("Google Distance Matrix response did not include rows. Falling back to local estimates.");
                    return matrix;
                }

                for (var i = 0; i < points.Count; i++)
                {
                    if (i >= rows.GetArrayLength())
                    {
                        continue;
                    }

                    if (!rows[i].TryGetProperty("elements", out var elements))
                    {
                        continue;
                    }

                    for (var j = 0; j < points.Count; j++)
                    {
                        if (i == j)
                        {
                            matrix[i, j] = 0;
                            continue;
                        }

                        if (TryGetDurationSeconds(elements, j, out var durationSeconds))
                        {
                            matrix[i, j] = durationSeconds;
                        }
                    }
                }

                _logger?.LogInformation(
                    "Google Distance Matrix loaded successfully for {PointCount} points.",
                    points.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Google Distance Matrix request failed. Falling back to local estimates.");
                return matrix;
            }

            return matrix;
        }

        // 2.Nearest Neighbor using the distance matrix 
        public List<LocationPoint> NearestNeighbor(LocationPoint start, List<LocationPoint> points, double[,] matrix, List<LocationPoint> allPoints)
        {
            var route = new List<LocationPoint>();
            var unvisited = new List<LocationPoint>(points);

            var current = start;
            route.Add(current);

            while (unvisited.Any())
            {
                var currentIndex = allPoints.IndexOf(current);

                var nearest = unvisited.OrderBy(p =>
                {
                    var pIndex = allPoints.IndexOf(p);
                    return matrix[currentIndex, pIndex];
                }).First();

                route.Add(nearest);
                unvisited.Remove(nearest);
                current = nearest;
            }

            return route;
        }

        // 3.Calculating the total distance based on the Google matrix
        private static double CalculateTotalDistance(List<LocationPoint> route, double[,] matrix, List<LocationPoint> allPoints)
        {
            double total = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                int fromIdx = allPoints.IndexOf(route[i]);
                int toIdx = allPoints.IndexOf(route[i + 1]);
                total += matrix[fromIdx, toIdx];
            }
            return total;
        }

        // 4. Optimizing the path using VNS and the matrix
        public List<LocationPoint> VNS(List<LocationPoint> route, double[,] matrix, List<LocationPoint> allPoints)
        {
            var bestRoute = new List<LocationPoint>(route);
            double bestDistance = CalculateTotalDistance(bestRoute, matrix, allPoints);
            var rand = new Random();

            for (int k = 0; k < VnsIterations; k++)
            {
                var newRoute = new List<LocationPoint>(bestRoute);
                if (newRoute.Count < 3) break;

                if (rand.Next(2) == 0)
                {
                    SwapStops(newRoute, rand);
                }
                else
                {
                    RelocateStop(newRoute, rand);
                }

                newRoute = TwoOpt(newRoute, matrix, allPoints);

                double newDistance = CalculateTotalDistance(newRoute, matrix, allPoints);
                if (newDistance < bestDistance)
                {
                    bestRoute = newRoute;
                    bestDistance = newDistance;
                }
            }
            return bestRoute;
        }

        public List<LocationPoint> Optimize(LocationPoint start, List<LocationPoint> points, double[,] matrix)
        {
            var allPoints = new List<LocationPoint> { start };
            allPoints.AddRange(points);

            // Step 1: Initial Solution
            var initialRoute = NearestNeighbor(start, points, matrix, allPoints);

            // Step 2: Refinement with the existing VNS approach.
            var optimized = VNS(initialRoute, matrix, allPoints);

            // Step 3: A light 2-opt pass improves edge crossings without replacing the heuristic structure.
            optimized = TwoOpt(optimized, matrix, allPoints);

            _logger?.LogInformation(
                "RouteOptimizer finished. Initial cost {InitialCost} sec, optimized cost {OptimizedCost} sec, stop count {StopCount}.",
                CalculateTotalDistance(initialRoute, matrix, allPoints),
                CalculateTotalDistance(optimized, matrix, allPoints),
                optimized.Count);

            return optimized;
        }

        private static void SwapStops(List<LocationPoint> route, Random rand)
        {
            if (route.Count < 3)
            {
                return;
            }

            var i = rand.Next(1, route.Count);
            var j = rand.Next(1, route.Count);

            while (i == j)
            {
                j = rand.Next(1, route.Count);
            }

            (route[i], route[j]) = (route[j], route[i]);
        }

        private static void RelocateStop(List<LocationPoint> route, Random rand)
        {
            if (route.Count < 3)
            {
                return;
            }

            var fromIndex = rand.Next(1, route.Count);
            var toIndex = rand.Next(1, route.Count);

            while (fromIndex == toIndex)
            {
                toIndex = rand.Next(1, route.Count);
            }

            var point = route[fromIndex];
            route.RemoveAt(fromIndex);
            route.Insert(Math.Min(toIndex, route.Count), point);
        }

        private static List<LocationPoint> TwoOpt(List<LocationPoint> route, double[,] matrix, List<LocationPoint> allPoints)
        {
            if (route.Count < 3)
            {
                return new List<LocationPoint>(route);
            }

            var bestRoute = new List<LocationPoint>(route);
            var bestDistance = CalculateTotalDistance(bestRoute, matrix, allPoints);
            var improved = true;

            while (improved)
            {
                improved = false;

                for (var i = 1; i < bestRoute.Count - 1; i++)
                {
                    for (var j = i + 1; j < bestRoute.Count; j++)
                    {
                        var candidate = ReverseSegment(bestRoute, i, j);
                        var candidateDistance = CalculateTotalDistance(candidate, matrix, allPoints);

                        if (candidateDistance >= bestDistance)
                        {
                            continue;
                        }

                        bestRoute = candidate;
                        bestDistance = candidateDistance;
                        improved = true;
                        break;
                    }

                    if (improved)
                    {
                        break;
                    }
                }
            }

            return bestRoute;
        }

        private static List<LocationPoint> ReverseSegment(List<LocationPoint> route, int startIndex, int endIndex)
        {
            var copy = new List<LocationPoint>(route);
            copy.Reverse(startIndex, endIndex - startIndex + 1);
            return copy;
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

        private static double[,] CreateFallbackMatrix(List<LocationPoint> points)
        {
            var matrix = new double[points.Count, points.Count];

            for (var i = 0; i < points.Count; i++)
            {
                for (var j = 0; j < points.Count; j++)
                {
                    matrix[i, j] = i == j
                        ? 0
                        : EstimateFallbackDurationSeconds(points[i], points[j]);
                }
            }

            return matrix;
        }

        private static double EstimateFallbackDurationSeconds(LocationPoint from, LocationPoint to)
        {
            var distanceKm = CalculateDistanceKm(from.Lat, from.Lng, to.Lat, to.Lng);
            if (distanceKm <= 0)
            {
                return 0;
            }

            var durationSeconds = (distanceKm / FallbackSpeedKmH) * 3600;
            return Math.Max(60, Math.Ceiling(durationSeconds));
        }

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }
    }
}
