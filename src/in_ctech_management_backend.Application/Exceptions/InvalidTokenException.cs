namespace in_ctech_management_backend.Application.Exceptions
{
    public class InvalidTokenException : AuthenticationException
    {
        public InvalidTokenException() : base("Invalid or expired token")
        {
        }

        public InvalidTokenException(string message) : base(message)
        {
        }
    }
}
