using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    /// As2es the input string the specified number of times.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-As2-ValidateAndParsePayload)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Output, object Error { string Message, dynamic AdditionalInfo } }</returns>
    public static async Task<Result> ValidateAndParsePayload(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
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

            string requestHeaders = $"AS2-From: {input.SenderAs2Id}\r\n";
            requestHeaders += $"AS2-To: {input.ReceiverAs2Id}\r\n";
            requestHeaders += $"Message-ID: {input.MessageId}\r\n";

            as2.RequestHeadersString = requestHeaders;

            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input.RawMessage)))
            {
                await as2.SetRequestStream(ms, cancellationToken);

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

                await as2.ProcessRequest(cancellationToken);

                if (!as2.AS2To.Equals(input.ReceiverAs2Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"The EDI message is meant for '{as2.AS2To}' not for us '{input.ReceiverAs2Id}'");
                }

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
