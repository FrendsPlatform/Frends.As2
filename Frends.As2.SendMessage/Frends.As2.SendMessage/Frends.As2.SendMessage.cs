using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using Frends.As2.SendMessage.Helpers;
using nsoftware.async.IPWorksEDI;

namespace Frends.As2.SendMessage;

/// <summary>
/// Task class
/// </summary>
public static class As2
{
    /// <summary>
    /// As2 SendMessage task sends a message to an AS2 server using the provided parameters
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-As2-SendMessage)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string PartnerResponse, string MessageId, bool IsMdnPending, string MdnStatus, string MdnMessage, string MdnIntegrityCheck, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> SendMessage(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (options.MdnMode == MdnMode.Async && string.IsNullOrWhiteSpace(options.AsyncMdnUrl))
            {
                throw new ArgumentException("AsyncMdnUrl must be provided when MdnMode is set to Async");
            }

            // var as2 = NSoftware.Activation.NSoftware.ActivateAs2Sender();
            var as2 = new AS2Sender();
            as2.RuntimeLicense = "42454E4A415A3039313832353330574542545231413100474642594A444C49455042514C54515A00303030303030303000003055535939483147334333560000#IPWORKSEDI#EXPIRING_TRIAL#20251018";
            as2.AS2From = input.SenderAs2Id;
            as2.AS2To = input.ReceiverAs2Id;

            if (input.AdditionalHeaders != null)
            {
                foreach (var header in input.AdditionalHeaders)
                {
                    if (string.IsNullOrWhiteSpace(header?.Name))
                        continue;

                    if (header.Name.Equals("AS2-From", StringComparison.OrdinalIgnoreCase) ||
                        header.Name.Equals("AS2-To", StringComparison.OrdinalIgnoreCase) ||
                        header.Name.Equals("Message-ID", StringComparison.OrdinalIgnoreCase) ||
                        header.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))
                        continue;

                    await as2.SetRequestHeader(header.Name, header.Value, cancellationToken);
                }
            }

            as2.Subject = input.Subject;
            as2.URL = connection.As2EndpointUrl;

            var uri = new Uri(connection.As2EndpointUrl);
            as2.MessageId = $"<{Guid.NewGuid()}@{uri.Host}>";

            if (options.MdnMode == MdnMode.Async)
            {
                as2.MDNDeliveryOption = options.AsyncMdnUrl;
            }

            as2.MDNTo = connection.MdnReceiver;

            if (connection.EncryptMessage || connection.SignMessage)
            {
                as2.RecipientCerts.Add(new Certificate(connection.ReceiverCertificatePath));
            }

            if (connection.SignMessage)
            {
                var password = connection.SenderCertificatePassword;
                as2.SigningCert =
                    new Certificate(CertStoreTypes.cstAuto, connection.SenderCertificatePath, password, "*");
            }
            else
            {
                as2.MDNOptions = string.Empty;
            }

            if (!connection.EncryptMessage)
            {
                as2.EncryptionAlgorithm = string.Empty;
            }

            as2.EDIData = new EDIData();
            as2.EDIData.EDIType = connection.ContentTypeHeader;
            as2.EDIData.Data = await File.ReadAllTextAsync(input.MessageFilePath, cancellationToken);
            as2.LogDirectory = "logs";

            await as2.Post(cancellationToken);

            var mdn = as2.MDNReceipt;

            var result = new Result
            {
                Success = true,
                PartnerResponse = mdn.Content,
                MessageId = as2.MessageId,
                IsMdnPending = options.MdnMode == MdnMode.Async,
                MdnStatus = options.MdnMode == MdnMode.Async
                    ? "Async MDN requested - MDN will be sent to: " + options.AsyncMdnUrl
                    : mdn.MDN.Split("\r\n")
                        .FirstOrDefault(l => l.StartsWith("Disposition:", StringComparison.OrdinalIgnoreCase))?.Trim(),
                MdnMessage = options.MdnMode == MdnMode.Async ? "MDN delivery pending" : mdn.Message,
                MdnIntegrityCheck = options.MdnMode == MdnMode.Async ? null : mdn.MICValue,
            };
            return result;
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}