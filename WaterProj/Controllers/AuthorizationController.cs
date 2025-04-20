using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WaterProj.DB;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IAuthorizationService _authorizationService;

        public AuthorizationController(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string login, string password,string userType)
        {
            Console.WriteLine($"Login: {login}, Password: {password}, UserType: {userType}");
            var serviceResult = await _authorizationService.CommonAuth(login, password, userType, HttpContext);
            if (serviceResult.Success)
            {
                TempData["SuccessMessage"] = "Успешная Авторизация!";

                return RedirectToAction("FindRoutes", "");
            }
            else
            {
                TempData["ErrorMessage"] = serviceResult.ErrorMessage ?? "Произошла ошибка при авторизации.";
            }

            return View();

        }
    }
}
