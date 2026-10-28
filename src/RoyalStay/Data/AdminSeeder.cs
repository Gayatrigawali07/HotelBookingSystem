namespace RoyalStay.Data;

/// <summary>
/// Creates the first admin account ONLY when:
///   - no Admin exists yet, and
///   - AdminSeed:Email and AdminSeed:Password are configured
///     (appsettings.Development.json for the demo, or user-secrets / environment variables).
/// The password is hashed before it is stored; the plain password is never saved in the database.
/// </summary>
public static class AdminSeeder
{
    public static void Seed(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var users = scope.ServiceProvider.GetRequiredService<UserRepository>();

        var email = config["AdminSeed:Email"];
        var password = config["AdminSeed:Password"];
        var name = config["AdminSeed:FullName"] ?? "Hotel Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("AdminSeed not configured - skipping admin creation.");
            return;
        }

        try
        {
            if (users.AnyAdminExists())
            {
                logger.LogInformation("An admin already exists - skipping seed.");
                return;
            }
            users.CreateAdmin(name, email.Trim().ToLowerInvariant(), password);
            logger.LogInformation("Admin account created for {Email}.", email);
        }
        catch (Exception ex)
        {
            // Do not stop the website if the database is not ready yet.
            logger.LogWarning(ex, "Could not seed admin. Is the database created? Run database/HotelBookingDB.sql first.");
        }
    }
}
