using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WaterProj.Services;
using Microsoft.AspNetCore.Identity;
using WaterProj.Models;

namespace WaterProj.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdministratorController : Controller
    {
        private readonly IAdministratorService _adminService;
        private readonly ITransporterService _transporterService;
        private readonly Services.IAuthorizationService _authorizationService;

        public AdministratorController(
            IAdministratorService adminService,
            Services.IAuthorizationService authorizationService,
            ITransporterService transporterService)
        {
            _adminService = adminService;
            _authorizationService = authorizationService;
            _transporterService = transporterService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("/Admin")]
        public async Task<IActionResult> Admin(string login, string password)
        {
            // Попытка аутентификации как администратор
            var serviceResult = await _authorizationService.CommonAuth(login, password, "Admin", HttpContext);

            if (serviceResult.Success)
            {
                // Перенаправляем на панель администратора
                return RedirectToAction("AdminPannel");
            }
            else
            {
                // Проверяем, есть ли администраторы в системе
                bool hasAdmins = await _adminService.HasAdminsAsync();
                ViewBag.HasAdmins = hasAdmins;

                ViewBag.ErrorMessage = serviceResult.ErrorMessage ?? "Неверные учетные данные администратора";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockTransporter(int transporterId, string blockReason)
        {
            if (string.IsNullOrWhiteSpace(blockReason))
            {
                TempData["ErrorMessage"] = "Причина блокировки должна быть указана";
                return RedirectToAction("ManageUsers");
            }

            var result = await _adminService.BlockTransporterAsync(transporterId, blockReason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Перевозчик успешно заблокирован";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Ошибка при блокировке перевозчика";
            }

            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public async Task<IActionResult> UnblockTransporter(int transporterId)
        {
            var result = await _adminService.UnblockTransporterAsync(transporterId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Перевозчик успешно разблокирован";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Ошибка при разблокировке перевозчика";
            }

            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public async Task<IActionResult> TransporterDetails(int transporterId)
        {
            var transporter = await _transporterService.GetTransporterInfoByIdAsync(transporterId);

            if (transporter == null)
            {
                TempData["ErrorMessage"] = "Перевозчик не найден";
                return RedirectToAction("ManageUsers");
            }

            return View(transporter);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/Admin")]
        public async Task<IActionResult> Admin()
        {
            // Если пользователь уже авторизован как админ, перенаправляем на панель
            if (User.Identity.IsAuthenticated && User.IsInRole("admin"))
            {
                return RedirectToAction("AdminPannel");
            }

            // Проверяем, есть ли администраторы в системе
            bool hasAdmins = await _adminService.HasAdminsAsync();
            ViewBag.HasAdmins = hasAdmins;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AdminPannel()
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var admin = await _adminService.GetByIdAsync(adminId);

            return View(admin);
        }

        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var (consumers, transporters) = await _adminService.GetAllUsersAsync();

            ViewBag.Consumers = consumers;
            ViewBag.Transporters = transporters;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoutes()
        {
            var routes = await _adminService.GetAllRoutesAsync();
            return View(routes);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Authorization");
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("/Admin/Register")]
        public async Task<IActionResult> RegisterFirstAdmin(string name, string login, string password, string confirmPassword)
        {
            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                ViewBag.RegErrorMessage = "Пароли не совпадают";
                ViewBag.HasAdmins = false;
                return View("Admin");
            }

            // Создание администратора через сервис
            var result = await _adminService.CreateFirstAdminAsync(name, login, password);

            if (!result.Success)
            {
                ViewBag.RegErrorMessage = result.ErrorMessage;
                ViewBag.HasAdmins = await _adminService.HasAdminsAsync();
                return View("Admin");
            }

            // Автоматическая авторизация
            var serviceResult = await _authorizationService.CommonAuth(login, password, "Admin", HttpContext);

            if (serviceResult.Success)
            {
                return RedirectToAction("AdminPannel");
            }
            else
            {
                // На случай, если авторизация не прошла
                return RedirectToAction("Admin");
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockRoute(int routeId, string blockReason)
        {
            if (string.IsNullOrWhiteSpace(blockReason))
            {
                TempData["ErrorMessage"] = "Причина блокировки должна быть указана";
                return RedirectToAction("ManageRoutes");
            }

            var result = await _adminService.ToggleRouteBlockStatusAsync(routeId, true, blockReason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Маршрут успешно заблокирован";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Ошибка при блокировке маршрута";
            }

            return RedirectToAction("ManageRoutes");
        }

        [HttpGet]
        public async Task<IActionResult> UnblockRoute(int routeId)
        {
            var result = await _adminService.ToggleRouteBlockStatusAsync(routeId, false);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Маршрут успешно разблокирован";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Ошибка при разблокировке маршрута";
            }

            return RedirectToAction("ManageRoutes");
        }
    }
}