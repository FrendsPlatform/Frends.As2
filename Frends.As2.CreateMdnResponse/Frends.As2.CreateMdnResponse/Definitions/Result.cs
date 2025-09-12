using System.Collections.Generic;

namespace Frends.As2.CreateMdnResponse.Definitions;

/// <summary>
/// Result of MDN response creation.
/// </summary>
public class Result
{
    /// <summary>
    /// Whether the MDN creation was successful.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP headers for the MDN response.
    /// </summary>
    /// <example>{"Content-Type": "multipart/report"}</example>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// MDN content ready for HTTP response.
    /// </summary>
    /// <example>Byte array with MDN</example>
    public byte[] Content { get; set; }

    /// <summary>
    /// Content-Type for the response.
    /// </summary>
    /// <example>multipart/report</example>
    public string ContentType { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, object { Exception Exception } AdditionalInfo }</example>
    public Error Error { get; set; }
}
