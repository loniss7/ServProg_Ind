using Microsoft.EntityFrameworkCore;
using ServerProg_Ind.Contracts;
using ServerProg_Ind.Domain;
using ServerProg_Ind.Infrastructure.Data;
using ServerProg_Ind.Infrastructure.Security;

namespace ServerProg_Ind.Application;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ILogger<AuthService> logger)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new AppException("display_name_required", "Display name is required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (!UniversityEmailValidator.IsAllowed(email))
        {
            throw new AppException("email_domain_invalid", "Only SFEDU email addresses are allowed.");
        }

        if (request.Password.Length < 8)
        {
            throw new AppException("password_too_short", "Password must be at least 8 characters long.");
        }

        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("email_in_use", "A user with this email already exists.", StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            Handle = await BuildUniqueHandleAsync(email, cancellationToken),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered user {UserId} ({Email})", user.Id, user.Email);

        return jwtTokenService.CreateToken(user, TimeSpan.FromHours(12));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new AppException("invalid_credentials", "Invalid email or password.", StatusCodes.Status401Unauthorized);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("invalid_credentials", "Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        logger.LogInformation("User {UserId} logged in", user.Id);
        return jwtTokenService.CreateToken(user, TimeSpan.FromHours(12));
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("user_not_found", "User not found.", StatusCodes.Status404NotFound);

        return new UserProfileDto(user.Id, user.DisplayName, user.Email, user.Handle);
    }

    private async Task<string> BuildUniqueHandleAsync(string email, CancellationToken cancellationToken)
    {
        var baseHandle = email.Split('@')[0]
            .ToLowerInvariant()
            .Replace('.', '_')
            .Replace('-', '_');

        var handle = baseHandle;
        var counter = 1;
        while (await dbContext.Users.AnyAsync(x => x.Handle == handle, cancellationToken))
        {
            counter += 1;
            handle = $"{baseHandle}{counter}";
        }

        return handle;
    }
}
