using Microsoft.AspNetCore.Mvc;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class ScriptLoaderController : Controller
    {
        private readonly IApiKeyService _apiKeyService;

        public ScriptLoaderController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        public IActionResult YandexMapsScript()
        {
            string apiKey = _apiKeyService.GetYandexMapsApiKey();
            Console.WriteLine($"Загрузка API Яндекс.Карт с ключом: {apiKey}");
            string scriptUrl = $"https://api-maps.yandex.ru/2.1/?apikey={apiKey}&lang=ru_RU";
            return Content($"console.log('Loading Yandex Maps API from: {scriptUrl}'); window.yandexMapsScriptLoaded = function() {{ console.log('Yandex Maps API loaded successfully'); }}; var script = document.createElement('script'); script.src = '{scriptUrl}'; script.async = true; script.defer = true; script.onload = window.yandexMapsScriptLoaded; document.head.appendChild(script);", "application/javascript");
        }
    }
}