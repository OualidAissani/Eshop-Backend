namespace MicroservicesTest.AuthenticationApi.Models;

public class JWTSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "library-auth-service";
    public string Audience { get; set; } = "library-api";
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    ///  "HS256": HMAC with SHA-256 (symmetric - uses shared secret)

    /// "RS256": RSA Signature with SHA-256 (asymmetric - uses public/private keys)


    public string Algorithm { get; set; } = "HS256";
    public int RefreshTokenExpiryDays { get; set; } = 7;
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
