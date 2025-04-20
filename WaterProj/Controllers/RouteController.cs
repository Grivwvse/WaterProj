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

        private readonly ApplicationDbContext _context;
        public RouteController(IRouteService routeService, ApplicationDbContext context, IOrderService orderService)
        {
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
                    ViewBag.ErrorMessage = "Маршрут не найден";
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
                ViewBag.ErrorMessage = $"Ошибка при загрузке маршрута: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult FindRoutes()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateRoute()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SaveRoute(IFormCollection form)
        {
            var routeData = JsonConvert.DeserializeObject<RouteDto>(form["routeData"]);
            var images = form.Files;

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
                ShipId = routeData.ShipId,
                TransporterId = transporterId,
                RouteStops = new List<RouteStop>()
            };

            // Сохраняем маршрут
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();  // Сохраняем и получаем ID маршрута

            // Добавляем остановки и RouteStop записи
            for (int i = 0; i < routeData.Stops.Count; i++)
            {
                var stopDto = routeData.Stops[i];

                // Создаём остановку
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

                // Привязываем к маршруту
                var routeStop = new RouteStop
                {
                    RouteId = route.RouteId,
                    StopId = stop.StopId,
                    StopOrder = i + 1
                };

                _context.RouteStop.Add(routeStop);
            }

            // Сохраняем все связи
            await _context.SaveChangesAsync();

            // Создаём папку для изображений маршрута, если она не существует
            var routeFolder = Path.Combine("wwwroot", "images", "routes", route.RouteId.ToString());
            if (!Directory.Exists(routeFolder))
            {
                Directory.CreateDirectory(routeFolder); // Папка для изображений будет создана автоматически
            }

            // Загружаем изображения, если они есть
            if (images.Count > 0)
            {
                foreach (var file in images)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // Генерация уникального имени файла
                    var filePath = Path.Combine(routeFolder, fileName);

                    // Сохраняем файл на диск
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Создаём запись о фотографии
                    var image = new Image
                    {
                        EntityType = "Route",
                        EntityId = route.RouteId,
                        ImagePath = "/images/routes/" + route.RouteId + "/" + fileName, // Сохраняем путь к файлу
                        Title = file.FileName
                    };

                    _context.Images.Add(image); // Сохраняем информацию о фотографии в базе данных
                }

                await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных
            }

            return Ok(new { success = true, routeId = route.RouteId });
        }


        //[HttpPost]
        //public async Task<IActionResult> SaveRoute([FromBody] RouteDto dto)
        //{
        //    if (dto == null || string.IsNullOrEmpty(dto.Map) || dto.Stops == null || !dto.Stops.Any())
        //    {
        //        return Json(new { success = false, message = "Неполные данные маршрута" });
        //    }




        //    var transporterIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    if (!int.TryParse(transporterIdString, out int transporterId))
        //    {
        //        return Unauthorized("Не удалось определить TransporterId из токена.");
        //    }

        //    var route = new Route
        //    {
        //        Name = dto.Name,
        //        Description = dto.Description,
        //        Map = dto.Map,
        //        Schedule = dto.Schedule,
        //        Rating = 0,
        //        ImagePath = "",
        //        ShipId = dto.ShipId,
        //        TransporterId = transporterId,
        //        RouteStops = new List<RouteStop>()
        //    }; 

        //    for (int i = 0; i < dto.Stops.Count; i++)
        //    {
        //        var stopDto = dto.Stops[i];

        //        // Создаём остановку
        //        var stop = new Stop
        //        {
        //            Name = stopDto.Name,
        //            Latitude = stopDto.Latitude,
        //            Longitude = stopDto.Longitude
        //            // Можно добавить поля Balloon, Hint и т.д., если есть в модели Stop
        //        };

        //        _context.Stops.Add(stop);
        //        await _context.SaveChangesAsync(); // Чтобы получить StopId

        //        // Привязываем к маршруту
        //        var routeStop = new RouteStop
        //        {
        //            StopId = stop.StopId,
        //            StopOrder = i + 1
        //        };

        //        route.RouteStops.Add(routeStop);
        //    }

        //    _context.Routes.Add(route);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { success = true });
        //}

        // Получение всех остановок
        [HttpGet]
        public async Task<IActionResult> SearchRoutes(string name = null, int? startStopId = null, int? endStopId = null)
        {
            try
            {
                Console.WriteLine($"Начало поиска маршрутов. Параметры: name={name}, startStopId={startStopId}, endStopId={endStopId}");

                var searchParams = new RouteSearchDto
                {
                    RouteName = name,
                    StartStopId = startStopId,
                    EndStopId = endStopId
                };

                // Если указаны обе остановки, используем специальный метод поиска
                List<RouteSearchResultDto> routes;
                if (startStopId.HasValue && endStopId.HasValue)
                {
                    routes = await _routeService.FindRoutesByStopsAsync(
                        new List<int> { startStopId.Value },
                        new List<int> { endStopId.Value }
                    );
                }
                else
                {
                    routes = await _routeService.SearchRoutesAsync(searchParams);
                }

                // Вместо возврата JSON передаем модель в представление SearchResults
                return View("SearchResults", routes);
            }
            catch (Exception ex)
            {
                // Записываем ошибку в лог
                Console.WriteLine($"Ошибка при поиске маршрутов: {ex.Message}");

                // Передаем пустую коллекцию и информацию об ошибке
                ViewBag.ErrorMessage = $"Произошла ошибка: {ex.Message}";
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

        [HttpPost]
        public async Task<IActionResult> FindRoutesByStops([FromBody] StopSearchDto searchDto)
        {
            try
            {
                if (searchDto == null || searchDto.StartStopIds == null || searchDto.EndStopIds == null ||
                    !searchDto.StartStopIds.Any() || !searchDto.EndStopIds.Any())
                {
                    return BadRequest(new { success = false, error = "Некорректные данные запроса" });
                }

                var routes = await _routeService.FindRoutesByStopsAsync(
                    searchDto.StartStopIds,
                    searchDto.EndStopIds
                );

                // Вместо JSON возвращаем представление с результатами
                return View("SearchResults", routes);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Произошла ошибка: {ex.Message}";
                return View("SearchResults", new List<RouteSearchResultDto>());
            }
        }
    }
}
