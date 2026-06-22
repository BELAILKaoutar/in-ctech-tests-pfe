namespace in_ctech_management_backend.Application.Exceptions
{
    public class PasswordMismatchException : AuthenticationException
    {
        public PasswordMismatchException() : base("Passwords do not match")
        {
        }

        public PasswordMismatchException(string message) : base(message)
        {
        }
    }
}
