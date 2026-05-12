using System.ComponentModel.DataAnnotations;

namespace UniBusApp.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "University email is required")]
        public string EmailPrefix { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}