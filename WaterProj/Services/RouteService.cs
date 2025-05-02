using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using SixLabors.ImageSharp.Processing;

using SixLabors.ImageSharp;
using WaterProj.Models.Services;

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

        public async Task<List<Models.Route>> GetRoutesByIdsAsync(List<int> routeIds)
        {
            return await _context.Routes
                .Where(r => routeIds.Contains(r.RouteId))
                .ToListAsync();
        }

        public async Task<List<Models.Route>> GetRoutesByShip(int shipId)
        {
            // Возвращаем полные объекты маршрутов, а не только id и name
            return await _context.Routes
                .Where(r => r.ShipId == shipId)
                .ToListAsync();
        }

        public async Task<ServiceResult> UpdateRoute(Models.Route route)
        {
            try
            {
                _context.Routes.Update(route);
                return new ServiceResult
                {
                    Success = await _context.SaveChangesAsync() > 0
                };
            }
            catch (Exception)
            {
                return new ServiceResult
                {
                    Success = false
                };
            }

        }

        /// <summary>
        /// Получение всех остановок из базы данных
        /// </summary>
        /// <returns>Список всех остановок</returns>
        public async Task<List<Stop>> GetAllStopsAsync()
        {
            try
            {
                // Находим ID всех активных и неблокированных маршрутов
                var activeRouteIds = await _context.Routes
                    .Where(r => r.IsActive && !r.IsBlocked)
                    .Select(r => r.RouteId)
                    .ToListAsync();

                // Получаем ID остановок, которые относятся к активным маршрутам
                var activeStopIds = await _context.RouteStop
                    .Where(rs => activeRouteIds.Contains(rs.RouteId))
                    .Select(rs => rs.StopId)
                    .Distinct()
                    .ToListAsync();

                // Получаем данные об остановках
                var stops = await _context.Stops
                    .Where(s => activeStopIds.Contains(s.StopId))
                    .AsNoTracking()
                    .ToListAsync();

                return stops;
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
                throw new NotFoundException("Транспортёр или маршрут не найден");
            }
            var ship = await _context.Ships
                .Where(r => r.ShipId == route.ShipId)
                .Include(r => r.ShipImages)
                .Include(r => r.ShipType)
                .Include(r => r.ShipСonveniences)
                .FirstOrDefaultAsync();

            var shipConveniences = await _context.Сonveniences
                .Where(c => c.ShipСonveniences.Any(sc => sc.ShipId == ship.ShipId))
                .ToListAsync();

            var routeRating = await _context.RouteRatings
                .Where(r => r.RouteId == id)
                .Include(rr => rr.RouteRatingAdvantages)
                .Include(r => r.Consumer)
                .Include(r => r.ReviewImages)
                .ToListAsync();


            var advantages = routeRating
                .Where(rr => rr.RouteRatingAdvantages != null) 
                .SelectMany(rr => rr.RouteRatingAdvantages) 
                .Where(rra => rra.Advantage != null) 
                .Select(rra => rra.Advantage) 
                .ToList(); 

            var images = await _context.Images
                .Where(img => img.EntityType == "Route" && img.EntityId == id)
                .ToListAsync();


            var dto = new RouteDetailsDto
            {
                Route = route,
                Ship = ship,
                Image = images,
                Transporter = route.Transporter,
                RouteAdvantages = advantages,
                RouteRatings = routeRating,
                ShipConveniences = shipConveniences
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
                .Where(r => r.IsActive && !r.IsBlocked);

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
                TransporterRating = r.Transporter.Rating,
                Rating = r.Rating
            }).ToList();
        }

        public async Task<IEnumerable<Stop>> GetAvailableStops(int startStopId)
        {
            // Находим все активные и неблокированные маршруты, в которых встречается данная остановка
            var routesWithStartStop = await _context.RouteStop
                .Where(rs => rs.StopId == startStopId)
                .Join(_context.Routes,
                    rs => rs.RouteId,
                    r => r.RouteId,
                    (rs, r) => new { RouteStop = rs, Route = r })
                .Where(x => x.Route.IsActive && !x.Route.IsBlocked)
                .Select(x => new { x.RouteStop.RouteId, x.RouteStop.StopOrder })
                .ToListAsync();

            if (!routesWithStartStop.Any())
                return new List<Stop>();

            // Список для хранения доступных остановок
            var availableStopIds = new HashSet<int>();

            // Для каждого маршрута находим остановки, которые идут ПОСЛЕ выбранной
            foreach (var routeInfo in routesWithStartStop)
            {
                // Получаем порядок выбранной остановки в текущем маршруте
                int startStopOrder = routeInfo.StopOrder;

                // Находим все остановки в этом маршруте, которые идут ПОСЛЕ выбранной (имеют больший порядковый номер)
                var nextStops = await _context.RouteStop
                    .Where(rs => rs.RouteId == routeInfo.RouteId && rs.StopOrder > startStopOrder)
                    .Select(rs => rs.StopId)
                    .ToListAsync();

                // Добавляем их в набор доступных остановок
                foreach (var stopId in nextStops)
                {
                    availableStopIds.Add(stopId);
                }
            }

            // Получаем полную информацию о доступных остановках
            if (availableStopIds.Any())
            {
                return await _context.Stops
                    .Where(s => availableStopIds.Contains(s.StopId))
                    .ToListAsync();
            }

            return new List<Stop>();
        }

        /// <summary>
        /// Поиск маршрутов, проходящих через указанные начальные и конечные остановки
        /// </summary>
        /// <param name="startStopIds">Список ID начальных остановок. Маршрут должен проходить через хотя бы одну из них.</param>
        /// <param name="endStopIds">Список ID конечных остановок. Маршрут должен проходить через хотя бы одну из них.</param>
        /// <returns>Список DTO результатов поиска маршрутов с основной информацией и первым изображением для каждого маршрута</returns>
        /// <remarks>
        /// Метод выполняет следующие действия:
        /// 1. Находит все маршруты, которые проходят через любую из указанных начальных остановок
        /// 2. Находит все маршруты, которые проходят через любую из указанных конечных остановок
        /// 3. Определяет порядок остановок в каждом маршруте (для проверки направления)
        /// 4. Отбирает только те маршруты, где начальная остановка идёт раньше конечной (по порядковому номеру)
        /// 5. Загружает детали отфильтрованных маршрутов и их первые изображения
        /// 6. Формирует и возвращает список DTO объектов с информацией о подходящих маршрутах
        /// 
        /// Особенности:
        /// - Для каждого маршрута выбирается только первое изображение с типом "Route"
        /// - Если у маршрута несколько начальных или конечных остановок, выбирается остановка с наименьшим порядковым номером
        /// - Для оптимизации запросов используется группировка и кэширование промежуточных результатов
        /// </remarks>
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

                // Загружаем только активные и неблокированные маршруты
                var routes = await _context.Routes
                    .Include(r => r.Transporter)
                    .Where(r => validRouteIds.Contains(r.RouteId) && r.IsActive && !r.IsBlocked)
                    .ToListAsync();

                // Получаем только ID активных и неблокированных маршрутов
                var activeRouteIds = routes.Select(r => r.RouteId).ToList();

                // Загружаем первые изображения только для активных маршрутов
                var routeImages = await _context.Images
                    .Where(img => img.EntityType == "Route" && activeRouteIds.Contains(img.EntityId))
                    .GroupBy(img => img.EntityId)
                    .Select(group => group.FirstOrDefault())
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
                    TransporterRating = r.Transporter.Rating,
                    Rating = r.Rating,
                    Image = routeImages.FirstOrDefault(img => img.EntityId == r.RouteId)?.ImagePath
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



        public async Task<(Stop? StartStop, Stop? EndStop)> GetRouteEndpointsAsync(int routeId)
        {
            // Загружаем RouteStops для указанного маршрута, включая связанные Stop
            var routeStops = await _context.RouteStop
                .Where(rs => rs.RouteId == routeId)
                .Include(rs => rs.Stop) 
                .OrderBy(rs => rs.StopOrder)
                .ToListAsync();

            
            if (!routeStops.Any())
            {
                return (null, null);
            }

            // Определяем начальную и конечную остановки
            var startStop = routeStops.First().Stop;
            var endStop = routeStops.Last().Stop;

            return (startStop, endStop);
        }

    }
}
