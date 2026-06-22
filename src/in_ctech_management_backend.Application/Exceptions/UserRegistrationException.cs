namespace in_ctech_management_backend.Application.Exceptions
{
    public class UserRegistrationException : AuthenticationException
    {
        public UserRegistrationException(string message) : base(message)
        {
        }

        public UserRegistrationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
