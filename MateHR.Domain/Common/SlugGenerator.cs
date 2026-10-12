using System.Text;

namespace MateHR.Domain.Common
{
    public static class SlugGenerator
    {
        public static string Generate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("El valor no puede ser nulo ni vacio.", nameof(name));
            }
            var normalized = name.ToLowerInvariant().Replace(' ', '-');
            var builder = new StringBuilder(normalized.Length);
            foreach(var ch in normalized)
            {
                if (char.IsAsciiLetterOrDigit(ch) || ch == '-')
                {
                    builder.Append(ch);
                }
            }
            var slug = builder.ToString();

            if (string.IsNullOrEmpty(slug))
            {
                throw new ArgumentException(
                    "El nombre no genera un slug valido.", nameof(name));
            }

            while (slug.Contains("--"))
            {
                slug = slug.Replace("--", "-");
            }

            return slug.Trim('-');
        }
    }
}
