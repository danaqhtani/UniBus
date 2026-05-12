using System.ComponentModel.DataAnnotations;

namespace UniBusApp.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "University email is required")]
        public string EmailPrefix { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters")]
        public string Password { get; set; } = string.Empty;
    }
}