namespace Void.Packer;

/// <summary>
/// Represents an error that occurs during pack operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackException"/> class carries a <see cref="PackError"/>
/// code that identifies the specific error condition. This allows callers
/// to handle specific failure cases without relying on exception message
/// parsing.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// try
/// {
///     var reader = new SolidPackReader("assets.pack", key);
/// }
/// catch (PackException ex)
/// {
///     switch (ex.Error)
///     {
///         case PackError.InvalidKey:
///             Console.WriteLine("Wrong key provided.");
///             break;
///         case PackError.UnsupportedVersion:
///             Console.WriteLine("Pack was created with a newer version.");
///             break;
///         default:
///             Console.WriteLine($"Pack error: {ex.Error}");
///             break;
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable after construction and safe for use from multiple threads.
/// </para>
/// </remarks>
public sealed class PackException : Exception
{
    /// <summary>
    /// Gets the specific error code associated with this exception.
    /// </summary>
    /// <value>
    /// A <see cref="PackError"/> value identifying the error condition.
    /// </value>
    public PackError Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackException"/> class.
    /// </summary>
    /// <param name="error">The specific error code.</param>
    /// <param name="message">The error message describing what went wrong.</param>
    public PackException(PackError error, string message) : base(message)
    {
        Error = error;
    }
}