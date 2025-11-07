using System.Runtime.Serialization;

namespace HMI.Platform.Core.Exceptions;

/// <summary>
/// Base exception class for all HMI platform exceptions.
/// </summary>
[Serializable]
public abstract class HMIException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    /// Gets additional context data for this exception.
    /// </summary>
    public Dictionary<string, object> Context { get; } = new();

    protected HMIException()
    {
    }

    protected HMIException(string message) : base(message)
    {
    }

    protected HMIException(string message, Exception innerException) : base(message, innerException)
    {
    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    protected HMIException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Context = (Dictionary<string, object>?)info.GetValue(nameof(Context), typeof(Dictionary<string, object>)) ?? new Dictionary<string, object>();
    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Context), Context);
    }

    /// <summary>
    /// Adds context data to the exception.
    /// </summary>
    public HMIException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple context data to the exception.
    /// </summary>
    public HMIException WithContext(Dictionary<string, object> contextData)
    {
        foreach (var kvp in contextData)
        {
            Context[kvp.Key] = kvp.Value;
        }
        return this;
    }
}
