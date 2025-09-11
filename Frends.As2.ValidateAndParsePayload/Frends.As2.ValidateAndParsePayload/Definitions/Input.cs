namespace Frends.As2.ValidateAndParsePayload.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Raw AS2 message (MIME format) from HTTP POST body.
    /// </summary>
    /// <example>MIME-Version: 1.0\r\nContent-Type: application/pkcs7-mime; ...</example>
    public string RawMessage { get; set; }

    /// <summary>
    /// AS2 sender identifier (maps to AS2-From).
    /// </summary>
    /// <example>PartnerAS2</example>
    public string SenderAs2Id { get; set; }

    /// <summary>
    /// AS2 receiver identifier (maps to AS2-To).
    /// </summary>
    /// <example>MyCompanyAS2</example>
    public string ReceiverAs2Id { get; set; }

    /// <summary>
    /// Message-ID from HTTP request (optional).
    /// </summary>
    /// <example>user@example.com</example>
    public string MessageId { get; set; }
}
