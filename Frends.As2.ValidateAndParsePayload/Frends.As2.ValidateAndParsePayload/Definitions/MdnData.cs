namespace Frends.As2.ValidateAndParsePayload.Definitions;

/// <summary>
/// Contains MDN (Message Disposition Notification) data generated after processing an AS2 message.
/// The MDN serves as a receipt confirmation that the AS2 message was received and processed successfully.
/// </summary>
public class MdnData
{
    /// <summary>
    /// MDN headers returned by the AS2Receiver.
    /// </summary>
    /// <example>"Content-Type: multipart/signed; protocol=..."</example>
    public string Headers { get; set; }

    /// <summary>
    /// Human-readable MDN message part.
    /// </summary>
    /// <example>"The message has been received and processed successfully."</example>
    public string Message { get; set; }

    /// <summary>
    /// Raw MDN content (MIME format).
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Message integrity check (MIC) value from the MDN.
    /// </summary>
    /// <example>"abc123xyz==, sha-256"</example>
    public string MicValue { get; set; }
}