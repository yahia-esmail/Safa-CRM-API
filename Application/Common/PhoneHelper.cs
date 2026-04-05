using PhoneNumbers;

namespace Application.Common;

/// <summary>
/// Phone number validation and formatting utility.
/// Uses libphonenumber-csharp — no Infrastructure dependency.
/// </summary>
public static class PhoneHelper
{
    private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

    public static string? Validate(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            return _phoneUtil.IsValidNumber(parsed) ? null : $"'{phoneNumber}' is not a valid phone number.";
        }
        catch (NumberParseException ex) { return $"Phone parse error: {ex.Message}"; }
    }

    public static string? ToE164(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            return _phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch { return phoneNumber; }
    }
}
