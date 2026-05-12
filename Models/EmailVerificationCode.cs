using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class EmailVerificationCode
{
    public int verification_id { get; set; }

    public int student_id { get; set; }

    public string code_hash { get; set; } = null!;

    public DateTime expires_at { get; set; }

    public DateTime created_at { get; set; }

    public int attempts { get; set; }

    public bool is_used { get; set; }

    public virtual Student Student { get; set; } = null!;
}
