namespace in_ctech_management_backend.Application.Exceptions
{
    public class UserAlreadyExistsException : AuthenticationException
    {
        public UserAlreadyExistsException() : base("User with this email already exists")
        {
        }

        public UserAlreadyExistsException(string message) : base(message)
        {
        }
    }
}
