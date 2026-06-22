namespace in_ctech_management_backend.Application.Exceptions
{
    public class InvalidCredentialsException : AuthenticationException
    {
        public InvalidCredentialsException() : base("Invalid credentials")
        {
        }

        public InvalidCredentialsException(string message) : base(message)
        {
        }
    }
}
