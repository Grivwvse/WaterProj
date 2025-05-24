using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using SixLabors.ImageSharp.Processing;

using SixLabors.ImageSharp;
using WaterProj.Models.Services;
using Microsoft.AspNetCore.Routing;
using Glimpse.Core.Extensibility;

namespace WaterProj.Services
{
    public class RouteService : IRouteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        public RouteService(ApplicationDbContext context, IOrderService orderService)
        {
            _orderService = orderService;
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

            var  routeOrderStats = await _orderService.GetOrderCountForRouteAsync(id, false);


            var dto = new RouteDetailsDto
            {
                Route = route,
                Ship = ship,
                Image = images,
                Transporter = route.Transporter,
                RouteAdvantages = advantages,
                RouteRatings = routeRating,
                ShipConveniences = shipConveniences,
                RouteOrderStats = routeOrderStats,
                RouteDays = route.RouteDays?.Select(rd => rd.DayOfWeek).ToList() ?? new List<DayOfWeek>()

            };

            return dto;
        }

        public async Task<ServiceResult> EditRoute(int routeId, int shipId, string schedule, List<DayOfWeek> routeDays)
        {
            try
            {
                var routetmp = await GetByIdAsync(routeId);
                if (routetmp == null)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Маршрут не найден"
                    };
                }
                routetmp.ShipId = shipId;
                routetmp.Schedule = schedule;

                // Удаляем старые дни маршрута
                if (routetmp.RouteDays != null)
                {
                    _context.RouteDays.RemoveRange(routetmp.RouteDays);
                }

                // Затем добавляем новые
                routetmp.RouteDays = routeDays.Select(day => new RouteDay
                {
                    RouteId = routeId,
                    DayOfWeek = day
                }).ToList();

                await UpdateRoute(routetmp);

                return new ServiceResult
                {
                    Success = true
                };

            }
            catch (Exception)
            {

                throw;
            }
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
                .Include(r => r.RouteDays)
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
                .Include(r => r.RouteDays)
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
            var routeDtos = routes.Select(r => new RouteSearchResultDto
            {
                RouteId = r.RouteId,
                Name = r.Name,
                Description = r.Description ?? "",
                Schedule = r.Schedule ?? "",
                TransporterId = r.TransporterId,
                TransporterName = r.Transporter.Name,
                TransporterRating = r.Transporter.Rating,
                Price = r.Price,
                Rating = r.Rating,
                RouteDays = r.RouteDays?.Select(rd => rd.DayOfWeek).ToList() ?? new List<DayOfWeek>()
            }).ToList();

            var routeIds = routeDtos.Select(r => r.RouteId).ToList();
            var orderCounts = await _orderService.GetOrderCountsForRoutesAsync(routeIds, false);
            foreach (var dto in routeDtos)
            {
                dto.RouteOrderStats = orderCounts.GetValueOrDefault(dto.RouteId, 0);
            }


            return routeDtos;
        }

        public async Task<List<Stop>> GetAvailableStops(int startStopId)
        {
            try
            {
                // Находим все маршруты, содержащие эту остановку
                var routesWithStop = await _context.RouteStop
                    .Where(rs => rs.StopId == startStopId)
                    .Select(rs => rs.RouteId)
                    .Distinct()
                    .ToListAsync();

                // Если таких маршрутов нет, возвращаем пустой список
                if (!routesWithStop.Any())
                {
                    return new List<Stop>();
                }

                // Дополнительно фильтруем маршруты - исключаем те, где startStopId является последней остановкой
                var validRouteIds = new List<int>();
                foreach (var routeId in routesWithStop)
                {
                    // Получаем все остановки маршрута в порядке следования
                    var routeStops = await _context.RouteStop
                        .Where(rs => rs.RouteId == routeId)
                        .OrderBy(rs => rs.StopOrder)
                        .ToListAsync();

                    // Находим позицию выбранной остановки
                    var startStopPosition = routeStops
                        .FirstOrDefault(rs => rs.StopId == startStopId)?.StopOrder;

                    // Если это не последняя остановка, добавляем маршрут в список валидных
                    if (startStopPosition.HasValue && startStopPosition.Value < routeStops.Last().StopOrder)
                    {
                        validRouteIds.Add(routeId);
                    }
                }

                // Используем только валидные маршруты
                return await GetAvailableStopsForRoutes(startStopId, validRouteIds);
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка в GetAvailableStops: {ex.Message}");
                // Возвращаем пустой список в случае ошибки
                return new List<Stop>();
            }
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
                    .Include(r => r.RouteDays)
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
                    Price = r.Price,
                    Rating = r.Rating,
                    Image = routeImages.FirstOrDefault(img => img.EntityId == r.RouteId)?.ImagePath,
                    RouteDays = r.RouteDays?.Select(rd => rd.DayOfWeek).ToList() ?? new List<DayOfWeek>()
                }).ToList();


                var routeIds = routeDtos.Select(r => r.RouteId).ToList();
                var orderCounts = await _orderService.GetOrderCountsForRoutesAsync(routeIds, false);
                foreach (var dto in routeDtos)
                {
                    dto.RouteOrderStats = orderCounts.GetValueOrDefault(dto.RouteId, 0);
                }

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

        public async Task<List<Stop>> GetAvailableStopsForRoutes(int startStopId, List<int> routeIds)
        {
            try
            {
                if (routeIds == null || !routeIds.Any())
                {
                    return new List<Stop>();
                }

                Console.WriteLine($"Поиск доступных остановок для начальной точки {startStopId} в маршрутах: {string.Join(", ", routeIds)}");

                // 1. Находим маршруты, в которых startStopId не является конечной точкой
                var routeStops = await _context.RouteStop
                    .Where(rs => routeIds.Contains(rs.RouteId))
                    .ToListAsync();

                // Группируем остановки по маршрутам для определения их порядка
                var routeStopGroups = routeStops
                    .GroupBy(rs => rs.RouteId)
                    .ToList();

                // 2. Отфильтровываем маршруты, где startStopId является последней остановкой
                var validRouteIds = new List<int>();
                foreach (var routeGroup in routeStopGroups)
                {
                    var routeId = routeGroup.Key;
                    var stops1 = routeGroup.OrderBy(rs => rs.StopOrder).ToList();

                    // Проверяем, содержит ли маршрут выбранную начальную остановку
                    var startStopEntry = stops1.FirstOrDefault(rs => rs.StopId == startStopId);
                    if (startStopEntry == null)
                    {
                        // Этот маршрут не содержит выбранную начальную остановку
                        continue;
                    }

                    // Проверяем, не является ли startStopId последней остановкой в маршруте
                    if (startStopEntry.StopOrder < stops1.Max(rs => rs.StopOrder))
                    {
                        validRouteIds.Add(routeId);
                    }
                }

                if (!validRouteIds.Any())
                {
                    Console.WriteLine("Нет подходящих маршрутов с выбранной начальной точкой");
                    return new List<Stop>();
                }

                // 3. Получаем порядковые номера стартовой остановки в каждом маршруте
                var routeStopsWithStartStop = routeStops
                    .Where(rs => rs.StopId == startStopId && validRouteIds.Contains(rs.RouteId))
                    .ToList();

                if (!routeStopsWithStartStop.Any())
                {
                    return new List<Stop>();
                }

                // Создаем словарь [RouteId] -> [StopOrder]
                var routeStartOrders = routeStopsWithStartStop
                    .ToDictionary(rs => rs.RouteId, rs => rs.StopOrder);

                // 4. Получаем ID остановок, которые идут ПОСЛЕ выбранной начальной остановки
                var availableStopIds = routeStops
                    .Where(rs => routeStartOrders.ContainsKey(rs.RouteId) &&
                              rs.StopOrder > routeStartOrders[rs.RouteId])
                    .Select(rs => rs.StopId)
                    .Distinct()
                    .ToList();

                // Если нет доступных остановок, возвращаем пустой список
                if (!availableStopIds.Any())
                {
                    return new List<Stop>();
                }

                // 5. Загружаем сами остановки по найденным ID
                var stops = await _context.Stops
                    .Where(s => availableStopIds.Contains(s.StopId))
                    .ToListAsync();

                Console.WriteLine($"Найдено {stops.Count} доступных остановок");
                return stops;
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка в GetAvailableStopsForRoutes: {ex.Message}");
                // Возвращаем пустой список в случае ошибки
                return new List<Stop>();
            }
        }
    }
}
