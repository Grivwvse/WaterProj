using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IFeedbackService
    {
        Task<ServiceResult> CheckIsFeedbackExist(int orderID);
        Task<List<Advantage>> GetAvailableRouteAdvantages();
        Task<ServiceResult> SaveFeedback(FeedbackDto model, IFormFileCollection photos, int consumerId);
    }
}
