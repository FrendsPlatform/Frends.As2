using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.ValidateAndParsePayload.Definitions;
using nsoftware.async.IPWorksEDI;

namespace Frends.As2.ValidateAndParsePayload.Tests.Helpers
{
    internal class Helpers
    {
        public static async Task<SenderResult> SendMessage(SenderInput input, SenderConnection connection, SenderOptions options, CancellationToken cancellationToken)
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

                if (input.AdditionalHeaders != null)
                {
                    foreach (var header in input.AdditionalHeaders)
                    {
                        if (header.Key.Equals("AS2-From", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("AS2-To", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Message-ID", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Subject", StringComparison.OrdinalIgnoreCase))
                            continue;

                        await as2.SetRequestHeader(header.Key, header.Value);
                    }
                }

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
                    var mdn = as2.MDNReceipt;
                    return new SenderResult
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
                throw new Exception($"Error in SendMessage: {e.Message}", e);
            }
        }
    }

    internal class SenderInput
    {
        public string SenderAs2Id { get; set; }

        public string ReceiverAs2Id { get; set; }

        public string Subject { get; set; }

        public string MessageFilePath { get; set; }

        [DisplayFormat(DataFormatString = "Text")]
        public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
    }

    internal class SenderConnection
    {
        public string As2EndpointUrl { get; set; }

        public string SenderCertificatePath { get; set; }

        public string SenderCertificatePassword { get; set; }

        public string ReceiverCertificatePath { get; set; }

        public string ContentTypeHeader { get; set; }

        public string MdnReceiver { get; set; }
    }

    internal class SenderOptions
    {
        public bool ThrowErrorOnFailure { get; set; }

        public bool SignMessage { get; set; }

        public bool EncryptMessage { get; set; }
    }

    internal class SenderResult
    {
        public bool Success { get; set; }

        public Error Error { get; set; }

        public string MessageId { get; set; }

        public string MDNStatus { get; set; }

        public string RawMDN { get; set; }

        public string MDNMessage { get; set; }

        public string MDNIntegrityCheck { get; set; }
    }
}
