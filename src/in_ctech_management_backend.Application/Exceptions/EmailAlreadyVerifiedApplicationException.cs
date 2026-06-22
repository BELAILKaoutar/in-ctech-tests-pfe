namespace in_ctech_management_backend.Application.Exceptions
{
    public class EmailAlreadyVerifiedApplicationException : ApplicationException
    {
        public EmailAlreadyVerifiedApplicationException(string email)
            : base($"The email address '{email}' has already been verified.")
        {
        }
    }
}
