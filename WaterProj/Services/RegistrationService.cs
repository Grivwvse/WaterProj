using WaterProj.DB;
using Microsoft.AspNetCore.Identity;
using WaterProj.Models;
using WaterProj.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace WaterProj.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Consumer> _passwordHasher;
        private readonly PasswordHasher<Transporter> _passwordHasherT;

        public RegistrationService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Consumer>();
            _passwordHasherT = new PasswordHasher<Transporter>();
        }
        public async Task<ServiceResult> RegisterTransporterAsync(Transporter model)
        {
            try
            {
                // Проверка, существует ли пользователь с таким логиноm
                if (IsLoginAvailable(model.Login) != null)
                {
                    return new ServiceResult { Success = false, ErrorMessage = "Пользователь с таким логином уже существует." };
                }

                // Генерация хеша пароля
                model.PasswordHash = _passwordHasherT.HashPassword(model, model.PasswordHash);

                await _context.Transporters.AddAsync(model);
                await _context.SaveChangesAsync();
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ServiceResult> RegisterCounsumerAsync(Consumer model)
        {
            try
            {
                if (!await (IsLoginAvailable(model.Login)))
                {
                    return new ServiceResult { Success = false, ErrorMessage = "Пользователь с таким логином уже существует." };
                }

                // Генерация хеша пароля
                model.PasswordHash = _passwordHasher.HashPassword(model, model.PasswordHash);

                await _context.Consumers.AddAsync(model);
                await _context.SaveChangesAsync();
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> IsLoginAvailable(string login)
        {
            // Проверяем наличие логина у потребителей
            bool existsInConsumers = await _context.Consumers.AnyAsync(c => c.Login == login);

            // Если логин уже существует у потребителей, он недоступен
            if (existsInConsumers)
            {
                return false;
            }

            // Проверяем наличие логина у перевозчиков
            bool existsInTransporters = await _context.Transporters.AnyAsync(t => t.Login == login);

            // Логин доступен только если он не существует ни у потребителей, ни у перевозчиков
            return !existsInTransporters;
        }
    }

}
