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
        private readonly PasswordHasher<Administrator> _passwordHasherA;
        private readonly PasswordHasher<Transporter> _passwordHasherT;
        public AuthorizationService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Consumer>();
            _passwordHasherT = new PasswordHasher<Transporter>();
            _passwordHasherA = new PasswordHasher<Administrator>();
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
                if (transporter == null)
                {
                    return new ServiceResult { Success = false, ErrorMessage = "Неверный логин или пароль" };
                }
                if (transporter.IsBlocked)
                {
                    string blockReason = string.IsNullOrEmpty(transporter.BlockReason)
                        ? "Учетная запись заблокирована администратором."
                        : $"Учетная запись заблокирована администратором. Причина: {transporter.BlockReason}";

                    return new ServiceResult { Success = false, ErrorMessage = blockReason };
                }
                bool isCached = await CasheUser(transporter, httpContext); // Await the Task<bool> result
                if (transporter != null || isCached)
                {
                    return new ServiceResult { Success = true };
                }
            }
            else if (userType == "Admin")
            {
                var admin = await AuthAdmin(login, password);
                bool isCached = await CasheUser(admin, httpContext);
                if (admin != null || isCached)
                {
                    return new ServiceResult { Success = true };
                }
            }


            return new ServiceResult { Success = false, ErrorMessage = "Неверный логин или пароль" };
        }

        public async Task<Administrator> AuthAdmin(string login, string password)
        {
            var admin = _context.Administrators.FirstOrDefault(a => a.Login == login);

            if (admin == null)
            {
                return null;
            }

            var passwordVerificationResult = _passwordHasherA.VerifyHashedPassword(admin, admin.PasswordHash, password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return admin;
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
            else if (user is Administrator admin)
            {
                claims.Add(new Claim(ClaimTypes.Name, admin.Login));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
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
