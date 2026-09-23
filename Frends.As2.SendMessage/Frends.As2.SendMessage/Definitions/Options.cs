using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Specifies whether to request synchronous or asynchronous MDN
    /// </summary>
    /// <example>Sync</example>
    [DefaultValue(MdnMode.Sync)]
    public MdnMode MdnMode { get; set; } = MdnMode.Sync;

    /// <summary>
    /// URL where async MDN should be sent (required when MdnMode is Async)
    /// </summary>
    /// <example>https://mycompany.com/api/as2/mdn-receiver</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(MdnMode), "", MdnMode.Async)]
    public string AsyncMdnUrl { get; set; }

    /// <summary>
    /// Disables server certificate validation for the HTTPS connection to the AS2 endpoint.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool AllowInvalidCertificate { get; set; }

    /// <summary>
    /// Base64 encoded server (or issuing CA) certificate, in DER or PEM format, that should be trusted
    /// for the HTTPS connection to the AS2 endpoint.
    /// </summary>
    /// <example>MIIDXTCCAkWgAwIBAgIJAJC1H...</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AllowInvalidCertificate), "", false)]
    public string TrustedCertificateBase64 { get; set; }

    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; }
}
