using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; }

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; }

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
}
