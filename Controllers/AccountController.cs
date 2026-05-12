using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniBusApp.Data;
using UniBusApp.helper;
using UniBusApp.Models;
using UniBusApp.Services;

namespace UniBusApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UniBusDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(UniBusDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string emailPrefix = model.EmailPrefix?.Trim() ?? "";

            if (emailPrefix.Contains("@"))
            {
                ModelState.AddModelError("EmailPrefix", "Write only the part before @sm.imamu.edu.sa");
                return View(model);
            }

            string fullEmail = emailPrefix + "@sm.imamu.edu.sa";

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.university_email == fullEmail);

            if (student == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            bool isValid = PasswordHelper.VerifyPassword(
                model.Password,
                student.password_salt,
                student.password_hash
            );

            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            if (student.email_verified != true)
            {
                ModelState.AddModelError("", "Please verify your university email first");
                return View(model);
            }

            HttpContext.Session.SetInt32("StudentId", student.student_id);
            HttpContext.Session.SetString("StudentName", student.name);
            HttpContext.Session.SetString("StudentEmail", student.university_email);

            return RedirectToAction("TodayTrips", "Student");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Buildings = await _context.Building.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationCode(RegisterViewModel model)
        {
            ViewBag.Buildings = await _context.Building.ToListAsync();

            string emailPrefix = model.EmailPrefix?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(emailPrefix))
            {
                ModelState.AddModelError("EmailPrefix", "University email is required");
                return View("Register", model);
            }

            if (emailPrefix.Contains("@"))
            {
                ModelState.AddModelError("EmailPrefix", "Write only the part before @sm.imamu.edu.sa");
                return View("Register", model);
            }

            string fullEmail = emailPrefix + "@sm.imamu.edu.sa";

            var existingStudent = await _context.Student
                .FirstOrDefaultAsync(s => s.university_email == fullEmail);

            if (existingStudent != null)
            {
                ModelState.AddModelError("EmailPrefix", "Email already exists");
                return View("Register", model);
            }

            var code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationEmail", fullEmail);

            try
            {
                await _emailSender.SendEmailAsync(
                    fullEmail,
                    "UniBus Verification Code",
                    $@"
Hello,

Your UniBus verification code is: {code}

Please enter this code to complete your registration.

If you did not request this, please ignore this email.

Thanks,
UniBus Team"
                );

                TempData["CodeSent"] = "Verification code sent to your university email";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Email send failed: " + ex.Message);
            }

            return View("Register", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Buildings = await _context.Building.ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            string emailPrefix = model.EmailPrefix?.Trim() ?? "";

            if (emailPrefix.Contains("@"))
            {
                ModelState.AddModelError("EmailPrefix", "Write only the part before @sm.imamu.edu.sa");
                return View(model);
            }

            string fullEmail = emailPrefix + "@sm.imamu.edu.sa";

            var existingStudent = await _context.Student
                .FirstOrDefaultAsync(s => s.university_email == fullEmail);

            if (existingStudent != null)
            {
                ModelState.AddModelError("EmailPrefix", "Email already exists");
                return View(model);
            }

            var sessionCode = HttpContext.Session.GetString("VerificationCode");
            var sessionEmail = HttpContext.Session.GetString("VerificationEmail");

            if (string.IsNullOrWhiteSpace(sessionCode) || string.IsNullOrWhiteSpace(sessionEmail))
            {
                ModelState.AddModelError("", "Please send the verification code first");
                return View(model);
            }

            if (sessionEmail != fullEmail)
            {
                ModelState.AddModelError("", "The verified email does not match the entered email");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Verification code is required");
                return View(model);
            }

            if (model.VerificationCode.Trim() != sessionCode)
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code");
                return View(model);
            }

            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword(model.Password, salt);

            var student = new Student
            {
                name = model.Name,
                phone_number = model.PhoneNumber,
                university_email = fullEmail,
                building_id = model.BuildingId,
                password_salt = salt,
                password_hash = hash,
                email_verified = true,
                email_verified_at = DateTime.Now
            };

            _context.Student.Add(student);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationEmail");

            TempData["Success"] = "Account created successfully!";

            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string emailPrefix = model.EmailPrefix?.Trim() ?? "";

            if (emailPrefix.Contains("@"))
            {
                ModelState.AddModelError("EmailPrefix", "Write only the part before @sm.imamu.edu.sa");
                return View(model);
            }

            string fullEmail = emailPrefix + "@sm.imamu.edu.sa";

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.university_email == fullEmail);

            if (student == null)
            {
                ModelState.AddModelError("", "No account found with this email");
                return View(model);
            }

            var code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ResetEmail", fullEmail);
            HttpContext.Session.SetString("ResetCode", code);

            await _emailSender.SendEmailAsync(
                fullEmail,
                "UniBus Password Reset Code",
                $@"
Hello,

Your UniBus password reset code is: {code}

If you did not request this, please ignore this email.

Thanks,
UniBus Team"
            );

            TempData["Success"] = "Password reset code sent to your university email";

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var sessionEmail = HttpContext.Session.GetString("ResetEmail");
            var sessionCode = HttpContext.Session.GetString("ResetCode");

            if (string.IsNullOrWhiteSpace(sessionEmail) || string.IsNullOrWhiteSpace(sessionCode))
            {
                ModelState.AddModelError("", "Please request a reset code first");
                return View(model);
            }

            if (model.Code.Trim() != sessionCode)
            {
                ModelState.AddModelError("Code", "Invalid verification code");
                return View(model);
            }

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.university_email == sessionEmail);

            if (student == null)
            {
                ModelState.AddModelError("", "Account not found");
                return View(model);
            }

            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword(model.NewPassword, salt);

            student.password_salt = salt;
            student.password_hash = hash;

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("ResetCode");

            TempData["Success"] = "Password updated successfully. Please sign in.";

            return RedirectToAction("Login");
        }
    }
}