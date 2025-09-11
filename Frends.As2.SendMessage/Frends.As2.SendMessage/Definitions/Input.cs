using System.ComponentModel.DataAnnotations;

namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Id of the company that will send the message.
    /// </summary>
    /// <example>MyCompany</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string SenderAs2Id { get; set; }

    /// <summary>
    /// Id of the company that will receive the message.
    /// </summary>
    /// <example>YourCompany</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ReceiverAs2Id { get; set; }

    /// <summary>
    /// Subject of the AS2 message.
    /// </summary>
    /// <example>Subject of the message</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string Subject { get; set; }

    /// <summary>
    /// Path to the file that will be sent in the message.
    /// </summary>
    /// <example>C:\Document\message.txt</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string MessageFilePath { get; set; }

    /// <summary>
    /// Optional additional HTTP headers to send with the AS2 message.
    /// </summary>
    /// <example>[{ "Name": "X-ApiKey", "Value": "my-api-key" }]</example>
    public Header[] AdditionalHeaders { get; set; }
}
