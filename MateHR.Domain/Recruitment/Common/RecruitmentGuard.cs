namespace MateHR.Domain.Recruitment.Common
{
    public static class RecruitmentGuard
    {
        public static string RequiredName(
            string? name,
            int maxLength,
            string paramName = "Name"
            )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("El nombre no puede ser nulo ni vacio.", paramName);
            }
            name = name.Trim();
            if (name.Length > maxLength)
            {
                throw new ArgumentException($"El nombre no puede tener mas de {maxLength} caracteres.", paramName);
            }
            return name;
        }
        public static TEnum ValidEnum<TEnum>(TEnum value, string paramName = "value")
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    $"El valor {value} no es un valor valido para el enum {typeof(TEnum).Name}.");
            }

            return value;
        }

        public static Guid NotEmptyGuid(Guid value, string paramName = "value")
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("El valor no puede ser un Guid vacio.", paramName);
            }
            return value;
        }
        public static int NonNegative(int value, string paramName = "value")
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    "El valor no puede ser negativo.");
            }

            return value;
        }
    }
}
