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
/// Task class.
/// </summary>
public static class As2
{
    /// <summary>
    /// As2 SendMessage task sends a message to an AS2 server using the provided parameters.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-As2-SendMessage)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string MessageId, bool IsMdnPending, string MDNStatus, string MDNMessage, string RawMDN, string MDNIntegrityCheck, object Error { string Message, dynamic AdditionalInfo } }</returns>
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

            AS2Sender as2 = new AS2Sender();

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

                    await as2.SetRequestHeader(header.Name, header.Value);
                }
            }

            as2.Subject = input.Subject;
            as2.URL = connection.As2EndpointUrl;
            as2.MessageId = Guid.NewGuid().ToString();

            if (options.MdnMode == MdnMode.Async)
            {
                as2.MDNDeliveryOption = options.AsyncMdnUrl;
                as2.MDNTo = options.AsyncMdnUrl;
            }
            else
            {
                as2.MDNTo = connection.MdnReceiver;
            }

            if (connection.SignMessage)
            {
                string password = connection.SenderCertificatePassword;
                as2.SigningCert = new Certificate(CertStoreTypes.cstAuto, connection.SenderCertificatePath, password, "*");
            }

            if (connection.EncryptMessage || !string.IsNullOrEmpty(connection.ReceiverCertificatePath))
            {
                as2.RecipientCerts.Add(new Certificate(connection.ReceiverCertificatePath));
            }

            as2.EDIData = new EDIData();
            as2.EDIData.EDIType = "text/plain";
            as2.EDIData.Data = File.ReadAllText(input.MessageFilePath);
            as2.LogDirectory = "logs";
            try
            {
                await as2.Post(cancellationToken);

                var mdn = as2.MDNReceipt;

                if (options.MdnMode == MdnMode.Async)
                {
                    return new Result
                    {
                        Success = true,
                        PartnerResponse = mdn.Content,
                        MessageId = as2.MessageId,
                        IsMdnPending = true,
                        MdnStatus = "Async MDN requested - MDN will be sent to: " + options.AsyncMdnUrl,
                        MdnMessage = "MDN delivery pending",
                        MdnIntegrityCheck = null,
                    };
                }
                else
                {
                    return new Result
                    {
                        Success = true,
                        PartnerResponse = mdn.Content,
                        MessageId = as2.MessageId,
                        IsMdnPending = false,
                        MdnStatus = mdn.MDN.Split("\r\n")
                            .FirstOrDefault(l => l.StartsWith("Disposition:", StringComparison.OrdinalIgnoreCase))?.Trim(),
                        MdnMessage = mdn.Message,
                        MdnIntegrityCheck = mdn.MICValue,
                    };
                }
            }
            catch (IPWorksEDIException)
            {
                throw;
            }
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}