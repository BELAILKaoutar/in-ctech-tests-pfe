using System.Text.Json.Serialization;

namespace in_ctech_management_backend.Application.Authentication.DTOs
{
    public record AuthResponseDto(
        [property: JsonPropertyName("token")]
        string AccessToken,
        DateTime ExpiresAt,
        UserDto User
    );

    public record UserDto(
        string Id,
        string Email,
        string UserName,
        IList<string> Roles,
        string? FirstName,
        string? LastName
    );
}