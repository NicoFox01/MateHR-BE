namespace MateHR.Domain.Exceptions
{
    public class EmailAlreadyExistsException : Exception
    {
        public EmailAlreadyExistsException(string email)
            : base($"El email '{email}' ya esta registrado.")
        {
            Email = email;
        }

        public string Email { get; }
    }
}