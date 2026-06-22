namespace in_ctech_management_backend.Application.Exceptions
{
    public class CustomerNotFoundApplicationException : ApplicationException
    {
        public CustomerNotFoundApplicationException(Guid id)
            : base($"Customer of id: '{id}' has not been found")
        {
        }
    }
}
