using System.ComponentModel.DataAnnotations;

namespace UniBus.Models
{
    public class LoginViewModel
    {
        [Required]
        public string EmailPrefix { get; set; }

        [Required]
        [MinLength(4)]
        public string Password { get; set; }
    }
}