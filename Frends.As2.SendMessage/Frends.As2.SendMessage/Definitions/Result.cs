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
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, object { Exception Exception } AdditionalInfo }</example>
    public Error Error { get; set; }

    /// <summary>
    /// ID of a sent message.
    /// </summary>
    /// <example>123</example>
    public string MessageId { get; set; }

    /// <summary>
    /// Status of the MDN response.
    /// </summary>
    /// <example>ReceivedValid</example>
    public string MDNStatus { get; set; }

    /// <summary>
    /// Raw MDN data returned by the recipient.
    /// </summary>
    /// <example>"Content-Type: multipart/signed; ..."</example>
    public string RawMDN { get; set; }

    /// <summary>
    /// Human-readable message included in the MDN.
    /// </summary>
    /// <example>The message was successfully received and processed.</example>
    public string MDNMessage { get; set; }

    /// <summary>
    /// Message Integrity Check (MIC) value reported in the MDN.
    /// </summary>
    public string MDNIntegrityCheck { get; set; }
}