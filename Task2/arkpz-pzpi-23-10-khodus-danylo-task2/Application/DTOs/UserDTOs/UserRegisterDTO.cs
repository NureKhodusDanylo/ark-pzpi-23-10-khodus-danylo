using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserDTOs
{
    public class UserRegisterDTO
    {
        public string? Name { get; set; }

        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? googleJwtToken { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
