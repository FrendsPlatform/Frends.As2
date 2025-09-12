using System.Collections.Generic;

namespace Frends.As2.ValidateAndParsePayload.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// HTTP headers from the AS2 request containing AS2-From, AS2-To, Message-ID and other AS2 metadata.
    /// </summary>
    /// <example>
    /// {
    ///     "AS2-From": "TestSender",
    ///     "AS2-To": "TestReceiver",
    ///     "Message-ID": "&lt;12345@example.com&gt;",
    ///     "Subject": "Test AS2 Message",
    ///     "MIME-Version": "1.0",
    ///     "Content-Type": "application/pkcs7-mime; smime-type=enveloped-data; name=\"smime.p7m\""
    /// }
    /// </example>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Raw body content as byte array containing the AS2 message payload (encrypted/signed data).
    /// </summary>
    /// <example>
    /// Encoding.UTF8.GetBytes("This is a test AS2 message payload")
    /// </example>
    public byte[] Body { get; set; }
}
