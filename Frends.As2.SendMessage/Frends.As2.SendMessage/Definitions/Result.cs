namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// The transport/MDN response from the AS2 partner:
    /// - Sync mode: full multipart/signed MDN content
    /// - Async mode: the immediate response from the POST (e.g., "200 OK")
    /// </summary>
    /// <example>200 OK</example>
    public string PartnerResponse { get; set; }

    /// <summary>
    /// ID of a sent message.
    /// </summary>
    /// <example>123</example>
    public string MessageId { get; set; }

    /// <summary>
    /// Status of the MDN response.
    /// </summary>
    /// <example>ReceivedValid</example>
    public string MdnStatus { get; set; }

    /// <summary>
    /// Human-readable message included in the MDN.
    /// </summary>
    /// <example>The message was successfully received and processed.</example>
    public string MdnMessage { get; set; }

    /// <summary>
    /// Message Integrity Check (MIC) value reported in the MDN.
    /// </summary>
    /// <example>
    /// "7v7F+fQbH4lD8bKGJTbXzWWcUlI=, sha1"
    /// </example>
    public string MdnIntegrityCheck { get; set; }

    /// <summary>
    /// Indicates whether the MDN delivery is pending (true for async MDN mode, false for sync MDN mode).
    /// When true, the MDN will be delivered separately to the AsyncMdnUrl endpoint.
    /// </summary>
    /// <example>false</example>
    public bool IsMdnPending { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, object { Exception Exception } AdditionalInfo }</example>
    public Error Error { get; set; }
}