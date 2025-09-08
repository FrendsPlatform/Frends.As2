using nsoftware.async.IPWorksEDI;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.As2.ValidateAndParsePayload.Tests.Helpers
{

    internal class SendMessage
    {
        public static async Task<SenderResult> TestSendMessage(SenderInput input, SenderConnection connection, SenderOptions options, CancellationToken cancellationToken)
        {
            try
            {
                AS2Sender as2 = new AS2Sender();
                as2.AS2From = input.SenderAs2Id;
                as2.AS2To = input.ReceiverAs2Id;
                as2.Subject = input.Subject;
                as2.URL = connection.As2EndpointUrl;
                as2.MDNTo = connection.MdnReceiver;
                as2.MessageId = Guid.NewGuid().ToString();
                if (options.SignMessage)
                {
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
                    return new SenderResult { Success = true, MessageId = as2.MessageId };
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
                throw new Exception($"Error in SendMessage: {e.Message}", e);
            }
        }
    }

    public class SenderInput
    {
        public string SenderAs2Id { get; set; }
        public string ReceiverAs2Id { get; set; }
        public string Subject { get; set; }
        public string MessageFilePath { get; set; }
    }

    public class SenderConnection
    {
        public string As2EndpointUrl { get; set; }
        public string SenderCertificatePath { get; set; }
        public string SenderCertificatePassword { get; set; }
        public string ReceiverCertificatePath { get; set; }
        public string ContentTypeHeader { get; set; }
        public string MdnReceiver { get; set; }
    }

    public class SenderOptions
    {
        public bool ThrowErrorOnFailure { get; set; }
        public bool SignMessage { get; set; }
        public bool EncryptMessage { get; set; }
    }

    public class SenderResult
    {
        /// <summary>
        /// Indicates if the task completed successfully.
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }

        /// <summary>
        /// ID of a sent message.
        /// </summary>
        /// <example>123</example>
        public string MessageId { get; set; }
    }


}
