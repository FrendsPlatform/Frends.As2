using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.ValidateAndParsePayload.Definitions;
using Frends.As2.ValidateAndParsePayload.Helpers;
using nsoftware.async.IPWorksEDI;

namespace Frends.As2.ValidateAndParsePayload;

/// <summary>
/// Task class.
/// </summary>
public static class As2
{
    /// <summary>
    /// Task to validate an incoming AS2 message, extracts the EDI payload, and generates an MDN receipt.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-As2-ValidateAndParsePayload)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Output, object Error { string Message, dynamic AdditionalInfo } }</returns>
    public static async Task<Result> ValidateAndParsePayload(
        Input input,
        Connection connection,
        Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.SenderAs2Id) ||
                string.IsNullOrWhiteSpace(input.ReceiverAs2Id) ||
                string.IsNullOrWhiteSpace(input.MessageId))
            {
                throw new ArgumentException("Missing required AS2 headers.");
            }

            var as2 = new AS2Receiver();

            // Extract headers and body from raw message (like the documentation example)
            var headerEndIndex = input.RawMessage.IndexOf("\r\n\r\n");
            if (headerEndIndex <= 0)
            {
                throw new Exception("Invalid AS2 message format - no header/body separator found");
            }

            // Set headers exactly like the documentation example
            string inputHeaders = input.RawMessage.Substring(0, headerEndIndex);
            as2.RequestHeadersString = inputHeaders;

            // Get just the body content for the stream
            string bodyContent = input.RawMessage.Substring(headerEndIndex + 4);
            var bodyBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(bodyContent);

            using (var ms = new MemoryStream(bodyBytes))
            {
                await as2.SetRequestStream(ms);

                if (connection.RequireSigned && !string.IsNullOrEmpty(connection.PartnerCertificatePath))
                {
                    as2.SignerCert = new Certificate(connection.PartnerCertificatePath);
                }

                if (connection.RequireEncrypted &&
                    !string.IsNullOrEmpty(connection.OwnCertificatePath) &&
                    !string.IsNullOrEmpty(connection.OwnCertificatePassword))
                {
                    as2.Certificate = new Certificate(
                        CertStoreTypes.cstPFXFile,
                        connection.OwnCertificatePath,
                        connection.OwnCertificatePassword,
                        "*");
                }

                await as2.ProcessRequest();

                if (!as2.AS2From.Equals(input.SenderAs2Id, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Sender AS2 ID mismatch: {as2.AS2From} != {input.SenderAs2Id}");

                if (!as2.AS2To.Equals(input.ReceiverAs2Id, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Receiver AS2 ID mismatch: {as2.AS2To} != {input.ReceiverAs2Id}");

                var payload = as2.EDIData?.Data != null
                    ? System.Text.Encoding.UTF8.GetBytes(as2.EDIData.Data)
                    : null;

                return new Result
                {
                    Success = true,
                    As2From = as2.AS2From,
                    As2To = as2.AS2To,
                    MessageId = as2.MessageId,
                    Payload = payload,
                    MdnReceipt = new MdnData
                    {
                        Headers = as2.MDNReceipt.Headers,
                        Message = as2.MDNReceipt.Message,
                        Content = as2.MDNReceipt.Content,
                        ContentB = as2.MDNReceipt.ContentB,
                        MicValue = as2.MDNReceipt.MICValue,
                    },
                };
            }
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}
