using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
using System.Linq;

namespace WaterProj.Services
{
    public class TransporterService : ITransporterService 
    {
        private readonly ApplicationDbContext _context;

        public TransporterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TransporterAccountDto> GetAllAccountInfo(int transporterId)
        {
            //Берем перевозчика по ID из БД
            var transporter = await _context.Transporters
                .FirstOrDefaultAsync(t => t.TransporterId == transporterId);

            if (transporter == null)
                throw new NotFoundException("Транспортёр не найден");

            //Беерем маршруты по ID перевозчика
            var routes = await _context.Routes
                .Include(r => r.Ship)
                .Where(r => r.TransporterId == transporterId)
                .ToListAsync();

            //Беерем Корабли по ID маршрута 
            var ships = routes
                .Select(r => r.Ship)
                .Where(ship => ship != null)
                .DistinctBy(s => s.ShipId)
                .ToList();

            var dto = new TransporterAccountDto
            {
                Transporter = transporter,
                Routes = routes,
                Ships = ships
            };

            return dto;
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

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
