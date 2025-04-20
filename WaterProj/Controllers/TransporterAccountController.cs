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
