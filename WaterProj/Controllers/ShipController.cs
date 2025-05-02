using Glimpse.Core.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class ShipController : Controller
    {
        private readonly IShipService _shipService;
        public readonly IRouteService _routeService;

        public ShipController(IShipService shipService, IRouteService routeService)
        {
            _routeService = routeService;
            _shipService = shipService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateShip()
        {
            ShipCreateDto shipCreateDto = new ShipCreateDto();
            // Получаем ID текущего транспортера из Claims
            int currentTransporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Устанавливаем ID транспортера в ViewBag для передачи в представление
            ViewBag.TransporterId = currentTransporterId;

            // Также можно установить значение непосредственно в DTO
            shipCreateDto.TransporterId = currentTransporterId;

            shipCreateDto.ShipСonveniences = await _shipService.GetAllConveniences();
            shipCreateDto.ShipTypes = await _shipService.GetAllShipTypes();

            return View(shipCreateDto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShipCreateDto dto, IFormFile MainImage, List<IFormFile> AdditionalImages)
        {
           // if (!ModelState.IsValid)
            //{
                // Повторно загружаем списки при ошибке валидации
                //dto.ShipСonveniences = await _shipService.GetAllConveniences();
                //dto.ShipTypes = await _shipService.GetAllShipTypes();
                //return View("CreateShip", dto);
           // }

            // Добавляем файлы в DTO
            dto.MainImage = MainImage;
            dto.AdditionalImages = AdditionalImages ?? new List<IFormFile>();

            // Отправляем запрос на создание корабля
            var result = await _shipService.CreateShip(dto);

            // Обрабатываем результат
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Судно успешно создано!";
                return RedirectToAction("Account", "TransporterAccount");
            }

            // В случае ошибки возвращаем форму с сообщением об ошибке
            ModelState.AddModelError("", result.ErrorMessage);
            dto.ShipСonveniences = await _shipService.GetAllConveniences();
            dto.ShipTypes = await _shipService.GetAllShipTypes();
            return View("CreateShip", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdvantage([FromBody] ConvenienceCreateDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Название удобства не может быть пустым" });
            }

            try
            {
                // Создаем новое удобство
                var convenience = new Сonvenience
                {
                    Name = model.Name,
                    Description = model.Description
                };

                // Добавляем через сервис (необходимо реализовать этот метод в IShipService)
                var result = await _shipService.CreateConvenience(convenience);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Удобство успешно создано",
                        advantageId = convenience.ShipСonvenienceId, // ID созданного удобства
                        name = convenience.Name
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Метод для получения смвязнных с кораблем маршрутов
        [HttpGet]
        public async Task<IActionResult> GetShipRoutes(int shipId)
        {
            try
            {
                // Получаем маршруты, связанные с этим кораблем
                var routes = await _routeService.GetRoutesByShip(shipId);

                return Json(new { success = true, routes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int shipId, int status)
        {
            // Проверяем, принадлежит ли корабль текущему перевозчику
            int transporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            try
            {
                ShipStatus newStatus = (ShipStatus)status;
                bool willDeactivateRoutes = (newStatus == ShipStatus.Inactive || newStatus == ShipStatus.Maintenance);

                var serviceResult = await _shipService.ChangeShipStatus(shipId, status, transporterId);

                if (!serviceResult.Success)
                {
                    TempData["ErrorMessage"] = serviceResult.ErrorMessage;
                    return RedirectToAction("Account", "TransporterAccount");
                }

                if (willDeactivateRoutes)
                {
                    // Получаем количество деактивированных маршрутов
                    var routes = await _routeService.GetRoutesByShip(shipId);
                    int affectedRoutesCount = routes.Count(r => !r.IsActive);

                    if (affectedRoutesCount > 0)
                    {
                        TempData["SuccessMessage"] = $"Статус корабля изменён. {affectedRoutesCount} маршрутов были деактивированы.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Статус корабля успешно изменён.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Статус корабля успешно изменён.";
                }

                return RedirectToAction("Account", "TransporterAccount");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при изменении статуса: {ex.Message}";
                return RedirectToAction("Account", "TransporterAccount");
            }
        }

    }
}
