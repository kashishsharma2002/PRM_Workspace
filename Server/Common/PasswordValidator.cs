namespace Server.Common;

public static class PasswordValidator
{
    public static bool IsValid(string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            error = "Password must be at least 8 characters.";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            error = "Password must contain at least one uppercase letter.";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            error = "Password must contain at least one number.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
