using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Connection string to AS2 server
    /// </summary>
    /// <example>https://as2.example.com/as2</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string As2EndpointUrl { get; set; }

    /// <summary>
    /// Defines whether to sign the message.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool SignMessage { get; set; }

    /// <summary>
    /// Defines whether to encrypt the message.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool EncryptMessage { get; set; }

    /// <summary>
    /// Password for the sender certificate.
    /// </summary>
    /// <example>mySecurePassword123</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    [UIHint(nameof(SignMessage), "", true)]
    public string SenderCertificatePassword { get; set; }

    /// <summary>
    /// Path to the sender certificate file in .pfx format.
    /// </summary>
    /// <example>C:\Document\sender_cert.pfx</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(SignMessage), "", true)]
    public string SenderCertificatePath { get; set; }

    /// <summary>
    /// Path to the receiver certificate file in .pfx format.
    /// </summary>
    /// <example>C:\Document\receiver_cert.pfx</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EncryptMessage), "", true)]
    public string ReceiverCertificatePath { get; set; }

    /// <summary>
    /// URL or email where to send the MDN (Message Disposition Notification).
    /// </summary>
    /// <example>user@example.com</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string MdnReceiver { get; set; }

    /// <summary>
    /// Specify content type header for the message if it's neither encrypted nor signed.
    /// </summary>
    /// <example>application/zip</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ContentTypeHeader { get; set; }
}