using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Authentication.DTOs
{
    public record ChangePasswordDto(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    );
}
