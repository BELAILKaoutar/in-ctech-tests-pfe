namespace in_ctech_management_backend.Application.Authentication.DTOs
{
    public record RegisterDto(
        string Email,
        string Password,
        string ConfirmPassword,
        string FirstName,
        string LastName,
        string? UserName = null
    );
}