using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using Frends.As2.SendMessage.Helpers;
using nsoftware.async.IPWorksEDI;
using Org.BouncyCastle.Cms;

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
    /// <returns>object { bool Success, string MessageId, object Error { string Message, dynamic AdditionalInfo } }</returns>
    public static async Task<Result> SendMessage0(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient httpClient = new();
            var data = Helpers.Helpers.BuildMimeBody(input.MessageFilePath);

            if (options.SignMessage)
            {
                data = data.Sign(
                    connection.SenderCertificatePath,
                    connection.SenderCertificatePassword,
                    CmsSignedGenerator.DigestSha512);
            }

            var precalculatedMic = Helpers.Helpers.ComputeMic(data);

            if (options.EncryptMessage)
            {
                data = await data.Encrypt(
                    connection.ReceiverCertificatePath,
                    CmsEnvelopedGenerator.Aes256Cbc,
                    cancellationToken);
            }

            var contentType = connection.ContentTypeHeader;
            if (options.SignMessage && options.EncryptMessage)
                contentType = "application/pkcs7-mime; smime-type=enveloped-data";
            else if (options.SignMessage)
                contentType = "application/pkcs7-mime; smime-type=signed-data";
            else if (options.EncryptMessage)
                contentType = "application/pkcs7-mime; smime-type=enveloped-data";

            var messageId = Guid.NewGuid().ToString();
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Headers.Add("AS2-Version", "1.2");
            content.Headers.Add("AS2-From", input.SenderAs2Id);
            content.Headers.Add("AS2-To", input.ReceiverAs2Id);
            content.Headers.Add("Message-ID", messageId);
            content.Headers.Add("Subject", input.Subject);
            content.Headers.Add("Content-Transfer-Encoding", "binary");
            content.Headers.Add("Disposition-Notification-To", connection.MdnReceiver);
            content.Headers.Add(
                "Disposition-Notification-Options",
                "signed-receipt-protocol=required, pkcs7-signature; signed-receipt-micalg=required, sha512");

            var response = await httpClient.PostAsync(connection.As2EndpointUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var micLine = Regex.Match(responseContent, @"^Received-Content-MIC: .*$", RegexOptions.Multiline).Value;
            var returnedMic = micLine.Replace("Received-Content-MIC: ", string.Empty).Trim();

            if (precalculatedMic != returnedMic) throw new Exception("Invalid MIC received from server.");
            return new Result { Success = response.IsSuccessStatusCode, Error = null, MessageId = messageId };
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }


    public static async Task<Result> SendMessage(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            AS2Sender as2 = new AS2Sender();
            //await as2.Config(conf"VerifyReceiptMIC=true", cancellationToken);
            as2.AS2From = input.SenderAs2Id;
            as2.AS2To = input.ReceiverAs2Id;
            as2.Subject = input.Subject;
            as2.URL = connection.As2EndpointUrl;
            as2.MDNTo = connection.MdnReceiver;
            as2.MessageId = Guid.NewGuid().ToString();
            if (options.SignMessage)
            {
                // The default password for the provided .pfx private key cert is 'test'
                string password = connection.SenderCertificatePassword;
                as2.SigningCert = new Certificate(CertStoreTypes.cstAuto, connection.SenderCertificatePath, password, "*");
            }

            if (options.EncryptMessage || !string.IsNullOrEmpty(connection.ReceiverCertificatePath))
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
                return new Result
                {
                    Success = true,
                    MessageId = as2.MessageId,
                    MDNStatus = mdn.MDN.Split("\r\n")
                    .FirstOrDefault(l => l.StartsWith("Disposition:", StringComparison.OrdinalIgnoreCase))?.Trim(),
                    MDNMessage = mdn.Message,
                    RawMDN = mdn.Content,
                    MDNIntegrityCheck = mdn.MICValue,
                };
            }
            catch (IPWorksEDIException e)
            {
                Console.WriteLine("Sending failed.");
                Console.WriteLine("Reason: " + e.Message);
                throw;
            }
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}