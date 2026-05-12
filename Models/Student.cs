using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class Student
{
    public int student_id { get; set; }

    public string name { get; set; } = null!;

    public string phone_number { get; set; } = null!;

    public string university_email { get; set; } = null!;

    public bool? email_verified { get; set; }

    public int building_id { get; set; }

    public byte[]? password_hash { get; set; }

    public byte[]? password_salt { get; set; }

    public DateTime? email_verified_at { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Building Building { get; set; } = null!;

    public virtual ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } = new List<EmailVerificationCode>();

    public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();
}
