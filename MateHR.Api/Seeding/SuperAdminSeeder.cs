using System.Security.Cryptography;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Enums;

namespace MateHR.Api.Seeding
{
    /// <summary>
    /// Crea el primer SuperAdmin. No puede existir por HTTP porque el endpoint de alta
    /// exige justamente un token de SuperAdmin: sin esta tarea el sistema no tiene
    /// por donde empezar.
    ///
    /// Uso:
    ///   dotnet run --project MateHR.Api -- --seed-superadmin --email root@matehr.com
    ///
    /// Si no se pasa <c>--password</c> se genera una aleatoria y se imprime una sola vez.
    /// </summary>
    public static class SuperAdminSeeder
    {
        public const string CommandName = "--seed-superadmin";

        public static bool IsRequested(string[] args)
        {
            return args.Any(arg => string.Equals(arg, CommandName, StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<int> RunAsync(IServiceProvider services, string[] args)
        {
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Seed");

            var email = ReadOption(args, "--email") ?? "root@matehr.com";
            var firstName = ReadOption(args, "--first-name") ?? "Root";
            var lastName = ReadOption(args, "--last-name") ?? "MateHR";

            var generated = string.IsNullOrWhiteSpace(ReadOption(args, "--password"));
            var password = ReadOption(args, "--password") ?? GeneratePassword();

            var provisioning = services.GetRequiredService<IUserProvisioningService>();

            try
            {
                var user = await provisioning.CreateAsync(
                    firstName,
                    lastName,
                    email,
                    password,
                    UserRole.SuperAdmin,
                    tenantId: null);

                logger.LogInformation("SuperAdmin creado. Id: {UserId}", user.Id);
                Console.WriteLine();
                Console.WriteLine("  SuperAdmin creado");
                Console.WriteLine($"  Id    : {user.Id}");
                Console.WriteLine($"  Email : {user.Email}");

                if (generated)
                {
                    Console.WriteLine($"  Pass  : {password}");
                    Console.WriteLine("  (generada ahora; cambiala en el primer login)");
                }

                Console.WriteLine();

                return 0;
            }
            catch (EmailAlreadyExistsException)
            {
                logger.LogWarning("El email {Email} ya existe; no se creo nada", email);
                Console.Error.WriteLine($"El email '{email}' ya esta registrado. No se creo nada.");
                return 1;
            }
        }

        private static string? ReadOption(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string GeneratePassword()
        {
            const string alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%";

            var chars = new char[24];

            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }

            return new string(chars);
        }
    }
}