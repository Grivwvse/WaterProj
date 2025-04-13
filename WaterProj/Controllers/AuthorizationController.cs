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
                ViewBag.AuthData = "Успешная авторизация!";
            }
            else
            {
                ViewBag.AuthData = serviceResult.ErrorMessage;
            }
            return View();
            //var consumer = await _authorizationService.AuthConsumer(login, password);
            //if (consumer == null)
            //{
            //    ViewBag.AuthData = "Пользователь не найден.";
            //    return View();
            //}

            //// Здесь можно добавить проверку хешированного пароля
            //if (consumer.PasswordHash == password)
            //{
            //    // Формируем клаймы и выполняем вход
            //    var claims = new List<Claim>
            //    {
            //        new Claim(ClaimTypes.Name, consumer.Login), //Имя 
            //        new Claim(ClaimTypes.NameIdentifier, consumer.ConsumerId.ToString()), // ID

            //    };

            //    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //    var authProperties = new AuthenticationProperties
            //    {
            //        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // 30 минут время кеширования входа
            //    };

            //    await HttpContext.SignInAsync(
            //        CookieAuthenticationDefaults.AuthenticationScheme,
            //        new ClaimsPrincipal(claimsIdentity),
            //        authProperties);

            //    // Перенаправляем на страницу аккаунта
            //    ViewBag.AuthData = "Успешная автьоризация.";
            //}
            //else
            //{
            //    ViewBag.AuthData = "Неверный пароль.";
            //}

            //return View();
        }
    }
}
