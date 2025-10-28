namespace Application.DTOs.UserDTOs
{
    public class UserProfileDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public int SentOrdersCount { get; set; }
        public int ReceivedOrdersCount { get; set; }
    }
}
