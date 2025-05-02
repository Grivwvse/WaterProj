using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IShipService
    {
        Task<List<Ship>> GetActiveShipsForTransporter(int transporterId);
        Task<ServiceResult> CreateShip(ShipCreateDto dto);
        Task<ServiceResult> CreateConvenience(Сonvenience convenience);
        Task<List<Сonvenience>> GetAllConveniences();
        Task<Ship> GetShipByIdAsync(int id);

        Task<List<Ship>> GetAllShipsAsync();
        Task<bool> UpdateShipAsync(Ship ship);
        Task<bool> DeleteShipAsync(int id);
        Task<List<ShipType>> GetAllShipTypes();
        Task<List<ShipImage>> GetAllImagesByShipIdAsync(int shipId);
        Task<bool> AddImageToShipAsync(ShipImage shipImage);
        Task<bool> DeleteImageFromShipAsync(int imageId);
        Task<ServiceResult> ChangeShipStatus(int shipId, int status, int transporterId);
    }
}
