using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class TransporterService : ITransporterService
    {
        private readonly ApplicationDbContext _context;

        public TransporterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Transporter> GetByIdAsync(int id)
        {
            return await _context.Transporters.FindAsync(id);
        }

        public async Task<ServiceResult> UpdateTransporterAsync(int userId, Transporter model)
        {
            var transporter = await _context.Transporters.FindAsync(userId);
            if (transporter == null)
                return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

            transporter.Login = model.Login;
            transporter.Name = model.Name;

            _context.Transporters.Update(transporter);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
    }


}
