using System.ComponentModel.DataAnnotations;

namespace UniBus.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string EmailPrefix { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        // Campus / Building selection from UI
        public int BuildingId { get; set; }
    }
}