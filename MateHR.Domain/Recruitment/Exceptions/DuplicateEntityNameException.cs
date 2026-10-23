namespace MateHR.Domain.Recruitment.Exceptions
{
    public sealed class DuplicateEntityNameException : Exception
    {
        public DuplicateEntityNameException(string name)
            : base($"Ya existe un registro con el nombre '{name}'.")
        {
            Name = name;
        }

        public string Name { get; }
    }
}
