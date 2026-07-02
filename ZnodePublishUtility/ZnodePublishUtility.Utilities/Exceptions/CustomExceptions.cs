namespace ZnodePublishUtility.Utilities.Exceptions;

public class ZnodePublishUtilityException : Exception
{
    public ZnodePublishUtilityException(string message) : base(message) { }
    public ZnodePublishUtilityException(string message, Exception innerException) : base(message, innerException) { }
}

public class ResourceNotFoundException : ZnodePublishUtilityException
{
    public ResourceNotFoundException(string resourceName, string resourceId) 
        : base($"{resourceName} with ID '{resourceId}' not found") { }
}

public class InvalidOperationException : ZnodePublishUtilityException
{
    public InvalidOperationException(string message) : base(message) { }
}

public class ValidationException : ZnodePublishUtilityException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(string message, List<string>? validationErrors = null) 
        : base(message)
    {
        ValidationErrors = validationErrors ?? new List<string>();
    }
}
