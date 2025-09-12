using System.Collections.Generic;
using System.Security.Cryptography;

namespace Frends.As2.CreateMdnResponse.Definitions;

/// <summary>
/// Input parameters for creating MDN response.
/// </summary>
/// <summary>
/// Input parameters for customizing existing MDN.
/// </summary>
public class Input
{
    /// <summary>
    /// The raw binary data of the full MDN (Message Disposition Notification) content.
    /// Provide the complete, unprocessed data of the receipt as you received it.
    /// </summary>
    /// <example>Base64 encoded MDN content</example>
    public byte[] MdnContentB { get; set; }

    /// <summary>
    /// The collection of metadata headers for the MDN.
    /// </summary>
    /// <example>{"Content-Type": "multipart/report"}</example>
    public Dictionary<string, string> MdnHeaders { get; set; }
}