using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace WaterProj.Controllers
{
    /// <summary>
    /// Контроллер для кадрирования изображений
    /// </summary>
    public class ImageController : Controller
    {
        [HttpGet("image/resize")]
        public IActionResult ResizeImage(string path, int width, int height, string mode = "max")
        {
            if (string.IsNullOrEmpty(path))
                return NotFound();

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            using (var image = SixLabors.ImageSharp.Image.Load(fullPath))
            {
                // Определяем режим изменения размера на основе параметра
                var resizeMode = mode.ToLower() switch
                {
                    "stretch" => ResizeMode.Stretch, // Растягивает изображение до указанных размеров
                    "pad" => ResizeMode.Pad,         // Сохраняет пропорции и добавляет отступы
                    "boxpad" => ResizeMode.BoxPad,   // Аналогично Pad, но с минимальными отступами
                    "min" => ResizeMode.Min,         // Уменьшает до минимального размера, сохраняя пропорции
                    "crop" => ResizeMode.Crop,       // Кадрирует для соответствия пропорциям, сохраняя максимум контента
                    _ => ResizeMode.Max              // По умолчанию: изменяет до максимального размера, сохраняя пропорции
                };

                // Создаем опции изменения размера
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = resizeMode
                };

                // Изменяем размер изображения
                image.Mutate(x => x.Resize(resizeOptions));

                // Сохраняем результат в поток
                var memoryStream = new MemoryStream();
                var extension = Path.GetExtension(fullPath).ToLowerInvariant();

                if (extension == ".png")
                {
                    image.Save(memoryStream, SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
                    memoryStream.Position = 0;
                    return File(memoryStream, "image/png");
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    image.Save(memoryStream, SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance);
                    memoryStream.Position = 0;
                    return File(memoryStream, "image/jpeg");
                }
                else
                {
                    image.Save(memoryStream, SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
                    memoryStream.Position = 0;
                    return File(memoryStream, "image/png");
                }
            }
        }
    }
}
