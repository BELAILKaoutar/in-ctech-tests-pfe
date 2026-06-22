namespace in_ctech_management_backend.Application.Exceptions
{
    public class AccountLockedException : AuthenticationException
    {
        public AccountLockedException() : base("Votre compte est désactivé, veuillez contacter votre administrateur.")
        {
        }

        public AccountLockedException(string message) : base(message)
        {
        }
    }
}
