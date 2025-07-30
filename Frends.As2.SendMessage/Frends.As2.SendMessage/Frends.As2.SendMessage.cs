using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using Frends.As2.SendMessage.Helpers;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;

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
    /// <returns>object { bool Success, string Output, object Error { string Message, dynamic AdditionalInfo } }</returns>
    public static async Task<Result> SendMessage(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            var signingCert =
                new X509Certificate2(connection.SenderCertificatePath, connection.SenderCertificatePassword);

            var receiverCertBytes = await File.ReadAllBytesAsync(connection.ReceiverCertificatePath, cancellationToken);
            var receiverCert = new X509CertificateParser().ReadCertificate(receiverCertBytes);

            HttpClient httpClient = new();
            var data = await File.ReadAllBytesAsync(input.MessageFilePath, cancellationToken);

            if (options.SignMessage) data = data.Sign(signingCert, CmsSignedGenerator.DigestSha512);
            if (options.EncryptMessage) data = data.Encrypt(receiverCert, CmsEnvelopedGenerator.Aes256Cbc);

            var contentType = connection.ContentTypeHeader;
            if (options.SignMessage && options.EncryptMessage)
                contentType = "application/pkcs7-mime; smime-type=enveloped-data; name=\"smime.p7m\"";
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
                "signed-receipt-protocol=optional, pkcs7-signature; signed-receipt-micalg=optional, sha512");

            var response = await httpClient.PostAsync(connection.As2EndpointUrl, content, cancellationToken);
            return new Result { Success = response.IsSuccessStatusCode, Error = null, MessageId = messageId };
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}