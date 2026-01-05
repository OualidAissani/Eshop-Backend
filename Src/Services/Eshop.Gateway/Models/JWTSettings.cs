namespace MicroservicesTest.AuthenticationApi.Models;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// 
/// These settings should be loaded from appsettings.json using the Options pattern.
/// This ensures configuration is validated at application startup rather than
/// runtime, and allows for easy configuration changes without recompiling.
/// 
/// Usage in Program.cs:
/// <code>
/// builder.Services.Configure&lt;JwtSettings&gt;(
///     builder.Configuration.GetSection("JwtSettings")
/// );
/// </code>
/// 
/// Usage in a service:
/// <code>
/// public MyService(IOptions&lt;JwtSettings&gt; jwtOptions)
/// {
///     var settings = jwtOptions.Value;
///     var secretKey = settings.SecretKey;
/// }
/// </code>
/// 
/// Configuration hierarchy:
/// 1. appsettings.json (development defaults)
/// 2. appsettings.{Environment}.json (environment-specific)
/// 3. Environment variables (override all)
/// 4. Azure Key Vault (production secrets)
/// </summary>
public class JWTSettings
{
    /// <summary>
    /// The secret key used to sign and validate JWT tokens.
    /// 
    /// Security Requirements:
    /// - MUST be at least 256 bits (32 characters) for HS256 algorithm
    /// - MUST be cryptographically random
    /// - MUST NOT be stored in source code
    /// - MUST be different for dev, staging, and production
    /// - SHOULD be rotated periodically (e.g., monthly)
    /// - SHOULD be stored in secure location (Azure Key Vault, AWS Secrets Manager)
    /// 
    /// Generation:
    /// You can generate a secure key using:
    /// <code>
    /// using System.Security.Cryptography;
    /// var key = new byte[32];
    /// RandomNumberGenerator.Fill(key);
    /// var base64Key = Convert.ToBase64String(key);
    /// Console.WriteLine(base64Key); // Use this value
    /// </code>
    /// 
    /// Or online: https://generate-random.org/
    /// Requirement: At least 32 characters for HS256
    /// 
    /// Example (for development only):
    /// "your-256-bit-secret-key-must-be-long-enough"
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer (creator) of the token.
    /// 
    /// This value is embedded in every token generated and is verified when validating tokens.
    /// It allows you to distinguish tokens from different services.
    /// 
    /// In a microservices architecture, all services use the same issuer to indicate
    /// they all came from the same authorization server.
    /// 
    /// Example: "library-auth-service"
    /// 
    /// RFC 7519 (JWT specification) defines this as the "iss" (issuer) claim.
    /// </summary>
    public string Issuer { get; set; } = "library-auth-service";

    /// <summary>
    /// The audience (intended recipient) of the token.
    /// 
    /// This identifies who the token is intended for. In a microservices architecture,
    /// all API services typically share the same audience.
    /// 
    /// This value is embedded in the token and verified during validation,
    /// ensuring tokens issued for one audience can't be used for another.
    /// 
    /// Example: "library-api"
    /// 
    /// RFC 7519 defines this as the "aud" (audience) claim.
    /// </summary>
    public string Audience { get; set; } = "library-api";

    /// <summary>
    /// How many minutes an access token remains valid before expiring.
    /// 
    /// This is a security/usability trade-off:
    /// - Shorter expiry (5-15 min): More secure, but requires frequent token refresh
    /// - Longer expiry (60+ min): More convenient, but less secure if token is stolen
    /// 
    /// Recommended values:
    /// - Development: 60 minutes (less refresh during development)
    /// - Production: 15 minutes (better security)
    /// - Mobile apps: 30 minutes (balance between security and battery/data usage)
    /// 
    /// After token expires:
    /// 1. Client receives 401 Unauthorized response
    /// 2. Client uses refresh token to get new access token
    /// 3. Client retries original request
    /// 4. User is NOT logged out, just needs new access token
    /// 
    /// Example: 15 (tokens valid for 15 minutes)
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// The cryptographic algorithm used to sign tokens.
    /// 
    /// Supported values:
    /// - "HS256": HMAC with SHA-256 (symmetric - uses shared secret)
    ///   Pros: Simple, fast, good for internal services
    ///   Cons: Requires sharing secret with all services
    /// 
    /// - "RS256": RSA Signature with SHA-256 (asymmetric - uses public/private keys)
    ///   Pros: Only Auth Service has private key, services only need public key
    ///   Cons: Slower, more complex setup
    /// 
    /// For this implementation, we use HS256 because:
    /// - All services are internal (within same network)
    /// - Faster performance
    /// - Simpler to manage keys with Aspire orchestration
    /// - Can upgrade to RS256 later if needed
    /// 
    /// Default: "HS256"
    /// </summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Optional: How many days a refresh token remains valid.
    /// 
    /// This is stored separately from the access token settings because
    /// refresh tokens have a much longer lifetime.
    /// 
    /// Recommended value: 7 days
    /// 
    /// After refresh token expires:
    /// - User must log in again
    /// - All refresh tokens for that user can be revoked
    /// 
    /// This value is typically used in the AuthenticationService,
    /// not directly by JwtTokenService.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;

    /// <summary>
    /// Validates that all required settings are properly configured.
    /// 
    /// This method should be called during application startup to catch
    /// configuration errors early rather than at runtime.
    /// 
    /// Example usage in Program.cs:
    /// <code>
    /// var jwtSettings = new JwtSettings();
    /// builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
    /// jwtSettings.Validate(); // Throws if invalid
    /// </code>
    /// 
    /// Throws ArgumentException if:
    /// - SecretKey is empty or null
    /// - SecretKey is less than 32 characters
    /// - Issuer is empty or null
    /// - Audience is empty or null
    /// - AccessTokenExpiryMinutes is 0 or negative
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when configuration is invalid
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new ArgumentException("JWT SecretKey is required and cannot be empty", nameof(SecretKey));

        if (SecretKey.Length < 32)
            throw new ArgumentException(
                $"JWT SecretKey must be at least 32 characters long (current length: {SecretKey.Length})",
                nameof(SecretKey));

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new ArgumentException("JWT Issuer is required and cannot be empty", nameof(Issuer));

        if (string.IsNullOrWhiteSpace(Audience))
            throw new ArgumentException("JWT Audience is required and cannot be empty", nameof(Audience));

        if (AccessTokenExpiryMinutes <= 0)
            throw new ArgumentException(
                "JWT AccessTokenExpiryMinutes must be greater than 0",
                nameof(AccessTokenExpiryMinutes));

        if (RefreshTokenExpiryDays <= 0)
            throw new ArgumentException(
                "JWT RefreshTokenExpiryDays must be greater than 0",
                nameof(RefreshTokenExpiryDays));
    }
}
