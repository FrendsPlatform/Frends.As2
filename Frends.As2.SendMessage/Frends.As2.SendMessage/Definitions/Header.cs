namespace Frends.As2.SendMessage.Definitions;

/// <summary>
/// Request header.
/// </summary>
public class Header
{
    /// <summary>
    /// Name of header.
    /// </summary>
    /// <example>X-ApiKey</example>
    public string Name { get; set; }

    /// <summary>
    /// Value of header.
    /// </summary>
    /// <example>my-api-key</example>
    public string Value { get; set; }
}