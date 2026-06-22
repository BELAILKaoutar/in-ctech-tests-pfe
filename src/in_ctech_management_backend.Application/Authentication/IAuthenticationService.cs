using in_ctech_management_backend.Application.Authentication.DTOs;

namespace in_ctech_management_backend.Application.Authentication
{
    public interface IAuthenticationService
    {
        Task<(AuthResponseDto Response, string RefreshToken)> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
        Task CreateEmployeeUserAsync(string email,
            string password,
            Guid employeeId,
            string firstName,
            string lastName,
            CancellationToken cancellationToken = default);
        Task DeleteEmployeeUserAsync(Guid employeeId, string? email, CancellationToken cancellationToken = default);
        Task DeleteUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> UserExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task ForgotPasswordAsync(string email, string resetBaseUrl, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
        Task EnableUserAsync(string email, CancellationToken cancellationToken = default);
        Task DisableUserAsync(string email, CancellationToken cancellationToken = default);
    }
}