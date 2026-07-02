namespace ZnodePublishUtility.Utilities.Helpers;

public static class ValidationHelper
{
    public static void ValidateString(string? value, string fieldName, int minLength = 1, int maxLength = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} cannot be empty");

        if (value.Length < minLength)
            throw new ArgumentException($"{fieldName} must be at least {minLength} characters");

        if (value.Length > maxLength)
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters");
    }

    public static void ValidateId(string? id, string fieldName = "ID")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException($"{fieldName} cannot be empty");
    }

    public static void ValidatePositiveNumber(int value, string fieldName)
    {
        if (value <= 0)
            throw new ArgumentException($"{fieldName} must be greater than 0");
    }
}
