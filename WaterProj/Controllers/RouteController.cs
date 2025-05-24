using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Json;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Services;
using Route = WaterProj.Models.Route;

namespace WaterProj.Controllers
{
    public class RouteController : Controller
    {


        private readonly IRouteService _routeService;
        private readonly IOrderService _orderService;
        private readonly IShipService _shipService;

        private readonly ApplicationDbContext _context;
        public RouteController(IRouteService routeService, IShipService shipService,  ApplicationDbContext context, IOrderService orderService)
        {
            _shipService = shipService;
            _routeService = routeService;
            _context = context;
            _orderService = orderService;
        }



        //Получение карты для RouteDetails 
        [HttpGet]
        public async Task<IActionResult> GetRouteMapData(int id)
        {
            try
            {
                var route = await _context.Routes.FindAsync(id);
                if (route == null)
                {
                    return NotFound(new { error = "Маршрут не найден" });
                }

                return Json(new
                {
                    success = true,
                    map = route.Map
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditRoute(int id)
        {
            try
            {
                // Проверяем права доступа
                if (User.IsInRole("transporter"))
                {
                    var transporterIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(transporterIdClaim) && int.TryParse(transporterIdClaim, out int transporterId))
                    {
                        var route = await _routeService.GetByIdAsync(id);
                        if (route == null || route.TransporterId != transporterId)
                        {
                            return Forbid();
                        }
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }

                // Получаем данные маршрута
                var routeDetails = await _routeService.GetRouteDetails(id);
                if (routeDetails == null)
                {
                    return NotFound();
                }

                // Если пользователь - перевозчик, получаем список его кораблей
                List<Ship> availableShips = new List<Ship>();
                if (User.IsInRole("transporter"))
                {
                    var transporterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    availableShips = await _shipService.GetActiveShipsForTransporter(transporterId);
                }

                // Создаем модель для редактирования
                var editRouteDto = new EditRouteDto
                {
                    RouteId = routeDetails.Route.RouteId,
                    Name = routeDetails.Route.Name,
                    Description = routeDetails.Route.Description,
                    Schedule = routeDetails.Route.Schedule,
                    Price = routeDetails.Route.Price,
                    ShipId = routeDetails.Route.ShipId,
                    RouteDays = routeDetails.Route.RouteDays?.Select(rd => rd.DayOfWeek).ToList() ?? new List<DayOfWeek>(),
                    AvailableShips = availableShips
                };

                return View(editRouteDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при загрузке данных для редактирования: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRoute([FromBody] EditRouteDto model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Данные модели отсутствуют" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Данные модели недействительны" });
            }

            try
            {
                // Проверяем права доступа (маршрут должен принадлежать текущему перевозчику)
                if (User.IsInRole("transporter"))
                {
                    var transporterIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(transporterIdClaim) && int.TryParse(transporterIdClaim, out int transporterId))
                    {
                        var route = await _routeService.GetByIdAsync(model.RouteId);
                        if (route == null || route.TransporterId != transporterId)
                        {
                            return Forbid();
                        }
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }

                var result = await _routeService.EditRoute(
                    model.RouteId,
                    model.ShipId,
                    model.Schedule,
                    model.RouteDays);

                if (result.Success)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка при обновлении маршрута: {ex.Message}" });
            }
        }

        //Просмотр своих маршрутов
        [HttpGet]
        public async Task<IActionResult> RouteDetails(int routeID)
        {
            try
            {
                // Получаем данные маршрута по ID
                var route = await _routeService.GetRouteDetails(routeID);

                if (route == null)
                {
                    TempData["ErrorMessage"] = "Маршрут не найден";
                    return View();
                }

                // Получаем статус заказа, если пользователь - Consumer
                if (User.Identity.IsAuthenticated && User.IsInRole("consumer"))
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    ViewBag.IsInActiveOrders = await _orderService.IsRouteInActiveOrdersAsync(routeID, userId);
                }

                return View(route);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при загрузке маршрута: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoutesFromStartStop(int startStopId, DateTime? date = null)
        {
            try
            {
                // Отладочное сообщение
                Console.WriteLine($"Запрос маршрутов для начальной точки с ID: {startStopId}, Дата: {date}");

                // Находим маршруты, которые проходят через указанную начальную остановку
                var routesWithStartStop = await _context.RouteStop
                    .Where(rs => rs.StopId == startStopId)
                    .Select(rs => rs.RouteId)
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"Найдено {routesWithStartStop.Count} маршрутов для начальной точки {startStopId}");

                // Базовый запрос для активных и неблокированных маршрутов
                var query = _context.Routes
                    .Where(r => routesWithStartStop.Contains(r.RouteId) && r.IsActive && !r.IsBlocked);

                // Если указана дата, фильтруем маршруты по дням недели
                if (date.HasValue)
                {
                    DayOfWeek dayOfWeek = date.Value.DayOfWeek;
                    // Получаем ID маршрутов, работающих в указанный день недели
                    var routeIdsForDay = await _context.RouteDays
                        .Where(rd => rd.DayOfWeek == dayOfWeek)
                        .Select(rd => rd.RouteId)
                        .ToListAsync();

                    // Добавляем фильтр по дню недели
                    query = query.Where(r => routeIdsForDay.Contains(r.RouteId));
                }

                // Выполняем запрос
                var routes = await query
                    .Select(r => new { r.RouteId, r.Map, r.Name })
                    .ToListAsync();

                // Проверяем данные маршрутов
                foreach (var route in routes)
                {
                    Console.WriteLine($"Маршрут #{route.RouteId}, имя: {route.Name}, карта: {(string.IsNullOrEmpty(route.Map) ? "отсутствует" : "присутствует")}");
                }

                return Json(new { success = true, routes = routes });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetRoutesFromStartStop: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult FindRoutes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateRouteAsync()
        {
            try
            {
                // Получаем ID текущего транспортера из Claims
                int transporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Получаем список активных кораблей для транспортера
                var activeShips = await _shipService.GetActiveShipsForTransporter(transporterId);

                CreateRouteDto createRouteDto = new CreateRouteDto
                {
                    Ships = activeShips
                };

                return View(createRouteDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Не удалось загрузить данные для создания маршрута";
                return View(new List<Ship>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRouteStops(int routeId)
        {
            try
            {
                Console.WriteLine($"Запрос на получение всех остановок для маршрута {routeId}");

                // Получаем все остановки для указанного маршрута, упорядоченные по порядку следования
                var routeStops = await _context.RouteStop
                    .Where(rs => rs.RouteId == routeId)
                    .Include(rs => rs.Stop)
                    .OrderBy(rs => rs.StopOrder)
                    .ToListAsync();

                Console.WriteLine($"Найдено {routeStops.Count} остановок для маршрута {routeId}");

                // Преобразуем в список остановок с нужными свойствами
                var stops = routeStops.Select(rs => new
                {
                    stopId = rs.Stop.StopId,  // Используем именно stopId в нижнем регистре, как ожидает JavaScript
                    name = rs.Stop.Name,
                    latitude = rs.Stop.Latitude,
                    longitude = rs.Stop.Longitude,
                    stopOrder = rs.StopOrder
                }).ToList();

                return Json(new
                {
                    success = true,
                    stops = stops
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении остановок маршрута {routeId}: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return StatusCode(500, new { success = false, error = $"Произошла ошибка при загрузке остановок маршрута: {ex.Message}" });
            }
        }



        [HttpPost]
        public async Task<IActionResult> SaveRoute(IFormCollection form)
        {
            try
            {
                var routeDataJson = form["routeData"].ToString();
                Console.WriteLine($"Received JSON: {routeDataJson}");

                var routeData = JsonConvert.DeserializeObject<RouteDto>(routeDataJson);
                var images = form.Files;
                Console.WriteLine($"Получено {images.Count} файлов изображений");

                Console.WriteLine($"Десериализовано {routeData.Stops?.Count ?? 0} остановок");

                if (routeData.Stops != null)
                {
                    foreach (var stop in routeData.Stops)
                    {
                        Console.WriteLine($"Остановка: {stop.Name}, ExistingStopId: {stop.ExistingStopId}");
                    }
                }

                if (routeData == null || string.IsNullOrEmpty(routeData.Name) || string.IsNullOrEmpty(routeData.Map) || routeData.Stops == null || !routeData.Stops.Any())
                {
                    return Json(new { success = false, message = "Неполные данные маршрута" });
                }

                var transporterIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(transporterIdString, out int transporterId))
                {
                    return Unauthorized("Не удалось определить TransporterId из токена.");
                }

                // Создаём маршрут
                var route = new Route
                {
                    Name = routeData.Name,
                    Description = routeData.Description,
                    Map = routeData.Map,
                    Schedule = routeData.Schedule,
                    Rating = 0,
                    Price = routeData.Price,
                    ShipId = routeData.ShipId,
                    TransporterId = transporterId,
                    RouteStops = new List<RouteStop>()
                };

                // Сохраняем маршрут
                _context.Routes.Add(route);
                await _context.SaveChangesAsync();  // Сохраняем и получаем ID маршрута

                // Добавляем дни недели для маршрута
                foreach (var day in routeData.OperatingDays)
                {
                    var routeDay = new RouteDay
                    {
                        RouteId = route.RouteId,
                        DayOfWeek = day
                    };
                    _context.RouteDays.Add(routeDay);
                }

                // Добавляем остановки и RouteStop записи
                for (int i = 0; i < routeData.Stops.Count; i++)
                {
                    var stopDto = routeData.Stops[i];
                    int stopId;

                    // Проверяем, есть ли существующая остановка
                    if (stopDto.ExistingStopId.HasValue && stopDto.ExistingStopId.Value > 0)
                    {
                        // Используем существующую остановку
                        stopId = stopDto.ExistingStopId.Value;

                        // Проверяем существование остановки в базе данных
                        var existingStop = await _context.Stops.FindAsync(stopId);
                        if (existingStop == null)
                        {
                            // Если остановка не найдена, возвращаем ошибку
                            return Json(new { success = false, message = $"Не найдена существующая остановка с ID {stopId}" });
                        }
                    }
                    else
                    {
                        // Проверяем, существует ли уже остановка с таким именем и координатами
                        var existingStop = await _context.Stops
                            .FirstOrDefaultAsync(s =>
                                s.Name == stopDto.Name &&
                                Math.Abs(s.Latitude - stopDto.Latitude) < 0.0001 &&
                                Math.Abs(s.Longitude - stopDto.Longitude) < 0.0001);

                        if (existingStop != null)
                        {
                            // Используем существующую остановку с таким же именем и координатами
                            stopId = existingStop.StopId;
                            Console.WriteLine($"Найдена существующая остановка с именем {stopDto.Name}, используем её с ID={stopId}");
                        }
                        else
                        {
                            // Создаём новую остановку
                            var stop = new Stop
                            {
                                Name = stopDto.Name,
                                Latitude = stopDto.Latitude,
                                Longitude = stopDto.Longitude,
                                // Дополнительные поля из stopDto, если есть
                                RouteStops = new List<RouteStop>()
                            };

                            _context.Stops.Add(stop);
                            await _context.SaveChangesAsync(); // Чтобы получить StopId
                            stopId = stop.StopId;
                            Console.WriteLine($"Создана новая остановка {stopDto.Name} с ID={stopId}");
                        }
                    }

                    // Привязываем к маршруту
                    var routeStop = new RouteStop
                    {
                        RouteId = route.RouteId,
                        StopId = stopId,
                        StopOrder = i + 1
                    };

                    _context.RouteStop.Add(routeStop);
                }

                // Сохраняем все связи остановок с маршрутом
                await _context.SaveChangesAsync();

                // Обработка и сохранение изображений
                if (images != null && images.Count > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "routes");

                    // Убедимся, что директория существует
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            // Создаем уникальное имя файла
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // Сохраняем файл на диск
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            // Добавляем запись о изображении в БД
                            var routeImage = new Image
                            {
                                EntityType = "Route",
                                EntityId = route.RouteId,
                                ImagePath = $"/images/routes/{uniqueFileName}",
                                Title = Path.GetFileNameWithoutExtension(image.FileName)
                            };

                            _context.Images.Add(routeImage);
                            Console.WriteLine($"Сохранено изображение: {uniqueFileName}");
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, routeId = route.RouteId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке: {ex.Message}");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRouteStops(int routeId, int startStopId, DateTime? date = null)
        {
            try
            {
                Console.WriteLine($"Запрос остановок маршрута {routeId} для начальной точки {startStopId}, Дата: {date}");

                // Если указана дата, проверяем, работает ли маршрут в этот день
                if (date.HasValue)
                {
                    DayOfWeek dayOfWeek = date.Value.DayOfWeek;
                    bool routeOperatesOnDay = await _context.RouteDays
                        .AnyAsync(rd => rd.RouteId == routeId && rd.DayOfWeek == dayOfWeek);

                    if (!routeOperatesOnDay)
                    {
                        Console.WriteLine($"Маршрут {routeId} не работает в выбранный день недели {dayOfWeek}");
                        return Json(new
                        {
                            success = true,
                            isValidDirection = false,
                            hasAvailableStops = false,
                            message = "Маршрут не работает в выбранный день"
                        });
                    }
                }

                // Получаем все остановки для указанного маршрута
                var routeStops = await _context.RouteStop
                    .Where(rs => rs.RouteId == routeId)
                    .OrderBy(rs => rs.StopOrder)
                    .ToListAsync();

                // Находим порядок выбранной остановки в этом маршруте
                var startStop = routeStops.FirstOrDefault(rs => rs.StopId == startStopId);
                if (startStop == null)
                {
                    Console.WriteLine($"Остановка {startStopId} не найдена в маршруте {routeId}");
                    return Json(new { success = false, isValidDirection = false, message = "Остановка не найдена в маршруте" });
                }

                // Проверяем, есть ли остановки после выбранной (в правильном направлении)
                var hasStopsAfter = routeStops.Any(rs => rs.StopOrder > startStop.StopOrder);

                Console.WriteLine($"Маршрут {routeId}: остановка {startStopId} имеет порядок {startStop.StopOrder}, наличие остановок после: {hasStopsAfter}");

                return Json(new
                {
                    success = true,
                    isValidDirection = true,
                    hasAvailableStops = hasStopsAfter
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении остановок маршрута: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableStops(int startStopId, DateTime? date = null)
        {
            try
            {
                Console.WriteLine($"Запрос доступных остановок для точки {startStopId}, дата: {date}");

                // Получаем список маршрутов, работающих в выбранный день (если дата указана)
                List<int> routeIdsForDay = null;
                if (date.HasValue)
                {
                    DayOfWeek dayOfWeek = date.Value.DayOfWeek;
                    routeIdsForDay = await _context.RouteDays
                        .Where(rd => rd.DayOfWeek == dayOfWeek)
                        .Select(rd => rd.RouteId)
                        .ToListAsync();

                    Console.WriteLine($"Найдено {routeIdsForDay.Count} маршрутов для дня недели {dayOfWeek}");
                }

                // Получаем доступные остановки (с учетом даты или без)
                IEnumerable<Stop> availableStops;
                if (routeIdsForDay != null && routeIdsForDay.Any())
                {
                    // Если дата выбрана и есть маршруты, работающие в этот день
                    availableStops = await _routeService.GetAvailableStopsForRoutes(startStopId, routeIdsForDay);
                }
                else
                {
                    // Если дата не выбрана или нет маршрутов на этот день
                    availableStops = await _routeService.GetAvailableStops(startStopId);
                }

                // Для отладки
                var stopIds = availableStops.Select(s => s.StopId).ToList();
                Console.WriteLine($"Доступных остановок: {stopIds.Count}. Список ID: {string.Join(", ", stopIds)}");

                // Применяем Select к результату
                return Json(new
                {
                    success = true,
                    availableStopIds = stopIds
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetAvailableStops: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStopsByOperatingDay(DateTime date)
        {
            try
            {
                DayOfWeek dayOfWeek = date.DayOfWeek;

                // Находим маршруты, которые работают в выбранный день недели
                var activeRouteIds = await _context.RouteDays
                    .Where(rd => rd.DayOfWeek == dayOfWeek)
                    .Select(rd => rd.RouteId)
                    .ToListAsync();

                // Находим только активные и не заблокированные маршруты
                var filteredRouteIds = await _context.Routes
                    .Where(r => activeRouteIds.Contains(r.RouteId) && r.IsActive && !r.IsBlocked)
                    .Select(r => r.RouteId)
                    .ToListAsync();

                // Получаем остановки, принадлежащие этим маршрутам
                var stopIds = await _context.RouteStop
                    .Where(rs => filteredRouteIds.Contains(rs.RouteId))
                    .Select(rs => rs.StopId)
                    .Distinct()
                    .ToListAsync();

                // Загружаем данные остановок
                var stops = await _context.Stops
                    .Where(s => stopIds.Contains(s.StopId))
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    stops = stops,
                    activeRouteCount = filteredRouteIds.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchRoutes([FromQuery] RouteSearchDto searchParams)
        {
            try
            {
                Console.WriteLine($"Начало поиска маршрутов. Параметры: {System.Text.Json.JsonSerializer.Serialize(searchParams)}");

                var name = searchParams.RouteName;
                var startStopId = searchParams.StartStopId;
                var endStopId = searchParams.EndStopId;
                var transporterName = searchParams.TransporterName;
                var departureDate = searchParams.DepartureDate;

                // Если указаны обе остановки, используем специальный метод поиска
                List<RouteSearchResultDto> routes;
                if (startStopId.HasValue && endStopId.HasValue)
                {
                    routes = await _routeService.FindRoutesByStopsAsync(
                        new List<int> { startStopId.Value },
                        new List<int> { endStopId.Value }
                    );

                    ViewBag.SearchType = "ByStops";

                    // Если имена остановок не были переданы, получим их из базы данных
                    if (string.IsNullOrEmpty(ViewBag.StartStopName) || string.IsNullOrEmpty(ViewBag.EndStopName))
                    {
                        var startStop = await _context.Stops.FindAsync(startStopId);
                        var endStop = await _context.Stops.FindAsync(endStopId);
                        ViewBag.StartStopName = startStop?.Name ?? "Неизвестно";
                        ViewBag.EndStopName = endStop?.Name ?? "Неизвестно";
                    }
                }
                else
                {
                    routes = await _routeService.SearchRoutesAsync(searchParams);
                    //Получаем для первого маршрута начальные и конечные точки
                    if (routes.Count > 0)
                    {
                        var Stops = await _routeService.GetRouteEndpointsAsync(routes.FirstOrDefault().RouteId);
                        var startStopName = Stops.StartStop?.Name;
                        var endStopName = Stops.EndStop?.Name;

                        ViewBag.StartStopName = startStopName;
                        ViewBag.EndStopName = endStopName;
                    }

                    ViewBag.SearchType = "ByName";
                }

                // Если указана дата, фильтруем маршруты по этой дате (по дням недели)
                if (departureDate.HasValue)
                {
                    DayOfWeek tripDay = departureDate.Value.DayOfWeek;
                    routes = routes.Where(r => r.RouteDays.Contains(tripDay)).ToList();

                    // Добавляем информацию о выбранной дате в ViewBag
                    ViewBag.DepartureDate = departureDate.Value.ToString("dd.MM.yyyy");
                    ViewBag.DepartureDayOfWeek = departureDate.Value.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
                }

                ViewBag.RouteAmount = routes.Count;
                ViewBag.SearchDepartureDate = departureDate;

                // Вместо возврата JSON передаем модель в представление SearchResults
                return View("SearchResults", routes);
            }
            catch (Exception ex)
            {
                // Записываем ошибку в лог
                Console.WriteLine($"Ошибка при поиске маршрутов: {ex.Message}");

                // Передаем пустую коллекцию и информацию об ошибке
                TempData["ErrorMessage"] = $"Ошибка при поиске маршрутов: {ex.Message}";
                return View("SearchResults", new List<RouteSearchResultDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStops()
        {
            try
            {
                var stops = await _routeService.GetAllStopsAsync();
                return Json(new
                {
                    success = true,
                    stops = stops
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRouteEndpoints(int routeId)
        {
            try
            {
                var (startStop, endStop) = await _routeService.GetRouteEndpointsAsync(routeId);

                if (startStop == null || endStop == null)
                {
                    return Json(new { success = false, error = "Остановки не найдены для данного маршрута" });
                }

                return Json(new
                {
                    success = true,
                    startStopName = startStop.Name,
                    endStopName = endStop.Name
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

    }
}
