using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.As2.CreateMdnResponse.Definitions;

/// <summary>
/// Options for customizing the MDN text only.
/// </summary>
public class Options
{
    /// <summary>
    /// MDN disposition status.
    /// </summary>
    /// <example>processed</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string DispositionStatus { get; set; } = "processed";

    /// <summary>
    /// Custom text message in MDN.
    /// </summary>
    /// <example>Message processed successfully</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string MdnText { get; set; }

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