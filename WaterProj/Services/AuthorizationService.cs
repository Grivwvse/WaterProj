using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WaterProj.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Consumer> _passwordHasher;
        private readonly PasswordHasher<Transporter> _passwordHasherT;
        public AuthorizationService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Consumer>();
            _passwordHasherT = new PasswordHasher<Transporter>();
        }

        public async Task<ServiceResult> CommonAuth(string login, string password, string userType, HttpContext httpContext)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Необходимо ввести и логин и пароль!" };
            }

            if (userType == "Consumer")
            {
                var consumer = await AuthConsumer(login, password);
                bool isCached = await CasheUser(consumer, httpContext); // Await the Task<bool> result
                if (consumer != null || isCached)
                {
                    return new ServiceResult { Success = true };
                }
            }
            else if (userType == "Transporter")
            {
                var transporter = await AuthTransporter(login, password);
                bool isCached = await CasheUser(transporter, httpContext); // Await the Task<bool> result
                if (transporter != null || isCached)
                {
                    return new ServiceResult { Success = true };
                }
            }

            return new ServiceResult { Success = false, ErrorMessage = "Invalid login or password." };
        }


        public async Task<Consumer> AuthConsumer(string login, string password)
        {
            var consumer = _context.Consumers.FirstOrDefault(c => c.Login == login);

            if (consumer == null)
            {
                return null;
            }
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(consumer, consumer.PasswordHash, password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return consumer;
        }

        public async Task<Transporter> AuthTransporter(string login, string password)
        {
            var transporter = _context.Transporters.FirstOrDefault(c => c.Login == login);

            if (transporter == null)
            {
                return null;
            }
            var passwordVerificationResult = _passwordHasherT.VerifyHashedPassword(transporter, transporter.PasswordHash, password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return transporter;
        }


        public async Task<bool> CasheUser(object user, HttpContext httpContext)
            {
            var claims = new List<Claim>();
            if (user is Consumer consumer)
            {
                // Формируем клаймы и выполняем вход
                claims.Add(new Claim(ClaimTypes.Name, consumer.Login));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, consumer.ConsumerId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "consumer"));
            }
            else if (user is Transporter transporter)
            {
                claims.Add(new Claim(ClaimTypes.Name, transporter.Login));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, transporter.TransporterId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "transporter"));
            }
            else
            {
                return false;
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // 30 минут время кеширования входа
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return true;
        }
    }
}
