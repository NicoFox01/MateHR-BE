namespace MateHR.Domain.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
            : base("Las credenciales son invalidas.")
        {
        }

        public InvalidCredentialsException(string message)
            : base(message)
        {
        }
    }
}