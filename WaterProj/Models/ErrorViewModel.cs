namespace WaterProj.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // Добавляем свойство для сообщения об ошибке
        public string? ErrorMessage { get; set; }
    }
}
