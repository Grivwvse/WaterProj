using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;

namespace WaterProj.Services
{
    public class RouteService : IRouteService
    {
        private readonly ApplicationDbContext _context;
        public RouteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Models.Route> GetByIdAsync(int id)
        {
            return await _context.Routes.FindAsync(id);
        }

        /// <summary>
        /// Получение всех остановок из базы данных
        /// </summary>
        /// <returns>Список всех остановок</returns>
        public async Task<List<Stop>> GetAllStopsAsync()
        {
            try
            {
                // Избегаем циклических ссылок при сериализации
                return await _context.Stops
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при получении списка остановок: {ex.Message}");
                throw; // Перебрасываем исключение для обработки в контроллере
            }
        }

        /// <summary>
        /// Метод, возвращающий route и связанный с ним ship
        /// </summary>
        /// <param name="id"> ID Route</param>
        /// <returns>RouteDetailsDto</returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<RouteDetailsDto> GetRouteDetails(int id)
        {
            var route = await GetRouteTransporterAsync(id);
            if (route == null) 
            {
                throw new NotFoundException("Транспортёр не найден");
            }
            var ship = await _context.Ships.FindAsync(route.ShipId);

            var images = await _context.Images
            .Where(img => img.EntityType == "Route" && img.EntityId == id)
            .ToListAsync();


            var dto = new RouteDetailsDto
            { 
                Route = route,
                Ship = ship,
                Image = images,
                Transporter = route.Transporter
            };

            return dto;
        }


        /// <summary>
        /// Получение перевозчика, связанного с маршрутом
        /// </summary>
        /// <param name="id">ID маршрута</param>
        /// <returns>Transporter, связанный с указанным маршрутом</returns>
        /// <exception cref="NotFoundException">Если маршрут не найден</exception>
        public async Task<Models.Route> GetRouteTransporterAsync(int id)
        {
            var route = await _context.Routes
                .Include(r => r.Transporter)
                .FirstOrDefaultAsync(r => r.RouteId == id);

            if (route == null)
            {
                throw new NotFoundException("Маршрут не найден");
            }

            return route;
        }

        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }

        public async Task<List<RouteSearchResultDto>> SearchRoutesAsync(RouteSearchDto searchParams)
        {
            IQueryable<Models.Route> query = _context.Routes
                .Include(r => r.Transporter)
                .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
                .Include(r => r.Images);

            // Добавляем фильтры по условиям поиска
            if (!string.IsNullOrEmpty(searchParams.RouteName))
            {
                query = query.Where(r => r.Name.Contains(searchParams.RouteName) ||
                                        r.Description.Contains(searchParams.RouteName));
            }

            if (!string.IsNullOrEmpty(searchParams.TransporterName))
            {
                query = query.Where(r => r.Transporter.Name.Contains(searchParams.TransporterName));
            }

            // Поиск по начальной и конечной остановке
            if (searchParams.StartStopId.HasValue && searchParams.EndStopId.HasValue)
            {
                // Маршруты, проходящие через указанные остановки
                var startStopId = searchParams.StartStopId.Value;
                var endStopId = searchParams.EndStopId.Value;

                query = query.Where(r =>
                    r.RouteStops.Any(rs => rs.StopId == startStopId) &&
                    r.RouteStops.Any(rs => rs.StopId == endStopId)
                );

                // Проверяем, что начальная остановка идёт раньше конечной
                query = query.Where(r =>
                    r.RouteStops.First(rs => rs.StopId == startStopId).StopOrder <
                    r.RouteStops.First(rs => rs.StopId == endStopId).StopOrder
                );
            }

            // Получаем маршруты и преобразуем в DTO
            var routes = await query.ToListAsync();

            // Проецируем результат в DTO
            return routes.Select(r => new RouteSearchResultDto
            {
                RouteId = r.RouteId,
                Name = r.Name,
                Description = r.Description ?? "",
                Schedule = r.Schedule ?? "",
                TransporterId = r.TransporterId,
                TransporterName = r.Transporter.Name,
                Rating = r.Rating,
                ImageUrl = r.Images?.FirstOrDefault()?.ImagePath
            }).ToList();
        }

        public async Task<List<RouteSearchResultDto>> FindRoutesByStopsAsync(List<int> startStopIds, List<int> endStopIds)
        {
            try
            {
                Console.WriteLine("Начало выполнения FindRoutesByStopsAsync...");

                // Находим маршруты, которые содержат начальные и конечные остановки
                var routesWithStartStops = await _context.RouteStop
                    .Where(rs => startStopIds.Contains(rs.StopId))
                    .Select(rs => new { rs.RouteId, rs.StopOrder })
                    .ToListAsync();

                var routesWithEndStops = await _context.RouteStop
                    .Where(rs => endStopIds.Contains(rs.StopId))
                    .Select(rs => new { rs.RouteId, rs.StopOrder })
                    .ToListAsync();

                var startStopsDict = routesWithStartStops
                    .GroupBy(rs => rs.RouteId)
                    .ToDictionary(g => g.Key, g => g.Min(rs => rs.StopOrder));

                var endStopsDict = routesWithEndStops
                    .GroupBy(rs => rs.RouteId)
                    .ToDictionary(g => g.Key, g => g.Min(rs => rs.StopOrder));

                var validRouteIds = startStopsDict.Keys
                    .Intersect(endStopsDict.Keys)
                    .Where(routeId => startStopsDict[routeId] < endStopsDict[routeId])
                    .ToList();

                // Загружаем маршруты    
                var routes = await _context.Routes
                    .Include(r => r.Transporter)
                    .Where(r => validRouteIds.Contains(r.RouteId))
                    .ToListAsync();

                // Загружаем изображения отдельно
                var routeImages = await _context.Images
                    .Where(img => img.EntityType == "Route" && validRouteIds.Contains(img.EntityId))
                    .ToListAsync();

                // Сопоставляем изображения с маршрутами
                var routeDtos = routes.Select(r => new RouteSearchResultDto
                {
                    RouteId = r.RouteId,
                    Name = r.Name,
                    Description = r.Description,
                    Schedule = r.Schedule,
                    TransporterId = r.TransporterId,
                    TransporterName = r.Transporter.Name,
                    Rating = r.Rating,
                    ImageUrl = routeImages.FirstOrDefault(img => img.EntityId == r.RouteId)?.ImagePath
                }).ToList();

                Console.WriteLine($"Маршрутов найдено: {routeDtos.Count}");
                return routeDtos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в FindRoutesByStopsAsync: {ex.Message}");
                throw;
            }
        }

    }
}
