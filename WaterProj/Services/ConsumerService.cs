using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class ConsumerService : IConsumerService
    {
        private readonly ApplicationDbContext _context;

        public ConsumerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Consumer> GetByIdAsync(int id)
        {
            return await _context.Consumers.FindAsync(id);
        }

        public async Task<ServiceResult> AddCounsumerAsync(Consumer model)
        {
            try
            {
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

        public async Task<ServiceResult> UpdateConsumerAsync(int userId, Consumer model)
        {
            var consumer = await _context.Consumers.FindAsync(userId);
            if (consumer == null)
                return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

            consumer.Login = model.Login;
            consumer.Name = model.Name;

            _context.Consumers.Update(consumer);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
    }


}
