using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WaterProj.DB;
using Microsoft.AspNetCore.Authorization;
using WaterProj.Services;
using WaterProj.Models;
using WaterProj.DTOs;

namespace WaterProj.Controllers
{
    [Authorize]
    public class TransporterAccountController : Controller
    {
        private readonly ITransporterService _transporterService;

        public TransporterAccountController(ITransporterService transporterService)
        {
            _transporterService = transporterService;
        }


        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Новый пароль и подтверждение пароля не совпадают.";
                return RedirectToAction("Account");
            }

            int currentTransporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _transporterService.ChangePasswordAsync(currentTransporterId, model.CurrentPassword, model.NewPassword);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Пароль успешно изменен!";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Произошла ошибка при смене пароля.";
            }

            return RedirectToAction("Account");
        }

        [HttpPost]
        public async Task<IActionResult> ActivateRoute(int routeId)
        {
            Console.WriteLine($"Активация маршрута: routeId={routeId}");
            return await ChangeRouteStatus(routeId, true);
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateRoute(int routeId)
        {
            Console.WriteLine($"Деактивация маршрута: routeId={routeId}");
            return await ChangeRouteStatus(routeId, false);
        }

        private async Task<IActionResult> ChangeRouteStatus(int routeId, bool isActive)
        {
            try
            {
                int transporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine($"ChangeRouteStatus: routeId={routeId}, isActive={isActive}, transporterId={transporterId}");

                var result = await _transporterService.ToggleRouteActivityAsync(routeId, transporterId, isActive);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = isActive
                        ? "Маршрут успешно активирован."
                        : "Маршрут успешно деактивирован.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                ModelState.Clear();
                return RedirectToAction("Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
                return RedirectToAction("Account");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Account()
        {

            try
            {
                int currentTransporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
                TransporterAccountDto accountDto = await _transporterService.GetAllAccountInfo(currentTransporterId);
                return View(accountDto);
            }
            catch (NotFoundException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(ViewBag);
            }


            //// Получаем имя пользователя из аутентификационных данных  
            //var currentTransporterId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
            //var transporter = await _transporterService.GetByIdAsync(currentTransporterId);

            
        }

        // Редактирование данных пользователя
        [HttpPost]
        public async Task<IActionResult> Edit(Transporter model)
        {
            Console.WriteLine("ModelState.IsValid");
            if (true) // Валидация только нужных нам полей
            {
                Console.WriteLine("ModelState.IsValid: " + ModelState.IsValid);
                var serviceResult = await _transporterService.UpdateTransporterAsync(Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier)), model);
                if (serviceResult.Success)
                {
                    TempData["SuccessMessage"] = "Данные успешно обновлены!";
                }
                else
                {
                    TempData["ErrorMessage"] = serviceResult.ErrorMessage ?? "Произошла ошибка при обновлении данных.";
                }
            }

            return RedirectToAction("Account");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Authorization");
        }
    }
}
