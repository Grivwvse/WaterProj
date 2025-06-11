// WaterProj/Services/ApiKeyService.cs
using Microsoft.Extensions.Configuration;

namespace WaterProj.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IConfiguration _configuration;

        public ApiKeyService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetYandexMapsApiKey()
        {
            return _configuration["ApiKeys:YandexMaps"] ?? string.Empty;
        }
    }
}