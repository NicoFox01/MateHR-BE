namespace MateHR.Domain.Exceptions
{
    public class SlugAlreadyExistsException : Exception
    {
        public SlugAlreadyExistsException(string slug)
            : base($"El slug '{slug}' ya esta en uso por otro tenant.")
        {
            Slug = slug;
        }
        public string Slug { get; }
    }
}
