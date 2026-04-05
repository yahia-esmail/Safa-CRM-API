using PhoneNumbers;

namespace Infrastructure.Services;

public static class PhoneValidationService
{
    private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Validates a phone number string (E.164 or local with region).
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    public static string? Validate(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null; // Phone is optional unless required by caller

        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            if (!_phoneUtil.IsValidNumber(parsed))
                return $"'{phoneNumber}' is not a valid phone number.";

            return null;
        }
        catch (NumberParseException ex)
        {
            return $"Phone parse error: {ex.Message}";
        }
    }

    /// <summary>
    /// Formats a phone number to E.164 format (e.g. +966501234567).
    /// Returns null if the number cannot be parsed.
    /// </summary>
    public static string? ToE164(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            return _phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch
        {
            return phoneNumber; // Return as-is if unparseable
        }
    }
}
