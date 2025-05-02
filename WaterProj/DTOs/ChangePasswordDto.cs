namespace WaterProj.DTOs
{
    public class ChangePasswordDto
    {
        public int TransporterId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
