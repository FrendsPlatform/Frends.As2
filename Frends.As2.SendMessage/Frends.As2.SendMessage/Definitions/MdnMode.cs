namespace Frends.As2.SendMessage.Definitions
{
    /// <summary>
    /// Specifies how the AS2 MDN (Message Disposition Notification) should be handled.
    /// </summary>
    public enum MdnMode
    {
        /// <summary>
        /// Synchronous MDN mode. The sender waits for the MDN immediately after sending the message.
        /// </summary>
        Sync,

        /// <summary>
        /// Asynchronous MDN mode. The sender requests the recipient to post the MDN to a separate endpoint,
        /// allowing the send operation to complete without waiting for the MDN.
        /// </summary>
        Async,
    }
}
