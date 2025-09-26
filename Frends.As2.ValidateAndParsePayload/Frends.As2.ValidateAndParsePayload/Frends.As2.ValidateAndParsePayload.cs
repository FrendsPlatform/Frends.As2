using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var as2 = new AS2Receiver();

            var headersString = ConvertHeadersToString(input.Headers);
            as2.RequestHeadersString = headersString;

            using var ms = new MemoryStream(input.Body);
            await as2.SetRequestStream(ms, cancellationToken);

            if (connection.RequireSigned)
            {
                as2.SignerCert = new Certificate(connection.PartnerCertificatePath);
            }

            if (connection.RequireSigned || connection.RequireEncrypted)
            {
                as2.Certificate = new Certificate(
                    CertStoreTypes.cstPFXFile,
                    connection.OwnCertificatePath,
                    connection.OwnCertificatePassword,
                    "*");
            }

            await as2.ProcessRequest(cancellationToken);

            return new Result
            {
                Success = true,
                As2From = as2.AS2From,
                As2To = as2.AS2To,
                MessageId = as2.MessageId,
                Payload = as2.EDIData?.Data,
                MdnReceipt = new MdnData
                {
                    Headers = as2.MDNReceipt.Headers,
                    Message = as2.MDNReceipt.Message,
                    Content = as2.MDNReceipt.Content,
                    MicValue = as2.MDNReceipt.MICValue,
                },
            };
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }

    private static string ConvertHeadersToString(Dictionary<string, string> headers)
    {
        var sb = new StringBuilder();
        foreach (var kvp in headers)
        {
            sb.AppendLine($"{kvp.Key}:{kvp.Value}");
        }

        return sb.ToString();
    }
}