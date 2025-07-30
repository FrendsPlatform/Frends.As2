using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using NUnit.Framework;

namespace Frends.As2.SendMessage.Tests;

[TestFixture]
public class UnitTests
{
    [Test]
    public async Task ShouldRepeatContentWithDelimiter()
    {
        var input = new Input
        {
            SenderAs2Id = "SenderAS2",
            ReceiverAs2Id = "SignedAndEncryptedReceiver",
            Subject = "Test Connection",
            MessageFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt"),
        };

        var connection = new Connection
        {
            As2EndpointUrl = "http://localhost:4080",
            SenderCertificatePassword = "sender123",
            SenderCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx"),
            ReceiverCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem"),
            ContentTypeHeader = "text/plain",
            MdnReceiver = "user@example.com",
        };

        var options = new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = null,
            SignMessage = true,
            EncryptMessage = true,
        };

        var result = await As2.SendMessage(input, connection, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }
}