using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using NUnit.Framework;

namespace Frends.As2.SendMessage.Tests;

[TestFixture]
public class IntegrationTests
{
    private readonly Input input = new()
    {
        SenderAs2Id = "Sender",
        ReceiverAs2Id = "Receiver",
        Subject = "Test Connection",
        MessageFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt"),
    };

    private readonly Connection connection = new()
    {
        As2EndpointUrl = "http://localhost:4080",
        SenderCertificatePassword = "sender123",
        SenderCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx"),
        ReceiverCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem"),
        ContentTypeHeader = "text/plain",
        MdnReceiver = "usr@example.com",
    };

    private readonly Options options = new()
    {
        ThrowErrorOnFailure = false,
        ErrorMessageOnFailure = null,
        SignMessage = true,
        EncryptMessage = true,
    };

    [Test]
    public async Task ShouldSendMessage()
    {
        var result = await As2.SendMessage(input, connection, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }
}