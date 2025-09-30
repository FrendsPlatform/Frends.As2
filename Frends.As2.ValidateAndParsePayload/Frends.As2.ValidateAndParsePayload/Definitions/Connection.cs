using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.ValidateAndParsePayload.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Forces signature validation when enabled.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool RequireSigned { get; set; }

    /// <summary>
    /// Path to partner's public certificate (.cer) file.
    /// </summary>
    /// <example>C:\Certs\partner.cer</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string PartnerCertificatePath { get; set; }

    /// <summary>
    /// Forces decryption when enabled.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool RequireEncrypted { get; set; }

    /// <summary>
    /// Path to your own certificate file in .pfx format.
    /// </summary>
    /// <example>C:\Certs\mycompany.pfx</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string OwnCertificatePath { get; set; }

    /// <summary>
    /// Password for your private key certificate.
    /// </summary>
    /// <example>mySecurePassword123</example>
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string OwnCertificatePassword { get; set; }
}
