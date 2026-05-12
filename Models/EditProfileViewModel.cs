using System.ComponentModel.DataAnnotations;

namespace UniBusApp.Models
{
    public class EditProfileViewModel
    {
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string UniversityEmail { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select your building")]
        public int BuildingId { get; set; }

        public string? CurrentBuildingName { get; set; }
    }
}