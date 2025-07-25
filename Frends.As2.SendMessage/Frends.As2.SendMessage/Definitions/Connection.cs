using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.SendMessage.Definitions;

// TODO: Remove this class if the task does not make connections

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Connection string to AS2 server
    /// </summary>
    /// <example>Host=127.0.0.1;Port=5432</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string As2EndpointUrl { get; set; }

    /// <summary>
    /// Password for the sender certificate.
    /// </summary>
    /// <example>Host=127.0.0.1;Port=5432</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    public string SenderCertificatePassword { get; set; }

    /// <summary>
    /// Path to the sender certificate file in .pfx format.
    /// </summary>
    /// <example>C:\Document\sender_cert.pfx</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    public string SenderCertificatePath { get; set; }

    /// <summary>
    /// Path to the signer certificate file in .pfx format.
    /// </summary>
    /// <example>C:\Document\receiver_cert.pfx</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    public string ReceiverCertificatePath { get; set; }
}
