using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Frends.As2.ValidateAndParsePayload.Definitions;
using NUnit.Framework;
using Frends.As2.ValidateAndParsePayload.Tests.Helpers;

namespace Frends.As2.ValidateAndParsePayload.Tests;

[TestFixture]
public class As2ReceiverIntegrationTests
{
    private HttpListener _httpListener;
    private readonly string _testEndpoint = "http://127.0.0.1:9090/as2";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://127.0.0.1:9090/");
        _httpListener.Start();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
    }

    private async Task<(string rawMessage, Dictionary<string, string> headers)> CaptureIncomingAS2MessageWithTimeout(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var context = await _httpListener.GetContextAsync().WaitAsync(cts.Token);

            var headers = new Dictionary<string, string>();
            foreach (string headerName in context.Request.Headers.AllKeys)
            {
                headers[headerName] = context.Request.Headers[headerName];
            }

            using var reader = new StreamReader(context.Request.InputStream);
            var rawMessage = await reader.ReadToEndAsync();

            var response = context.Response;
            response.StatusCode = 200;
            response.Close();

            return (rawMessage, headers);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"No AS2 message received within {timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error capturing incoming AS2 message: {ex.Message}", ex);
        }
    }

    [Test]
    public async Task ShouldReceiveAndParseBasicAS2Message()
    {
        var senderInput = new SenderInput
        {
            SenderAs2Id = "TestSender",
            ReceiverAs2Id = "TestReceiver",
            Subject = "Integration Test",
            MessageFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt"),
        };

        var senderConnection = new SenderConnection
        {
            As2EndpointUrl = _testEndpoint,
            SenderCertificatePassword = "sender123",
            SenderCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx"),
            ReceiverCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem"),
            ContentTypeHeader = "text/plain",
            MdnReceiver = "test@example.com",
        };

        var senderOptions = new SenderOptions
        {
            ThrowErrorOnFailure = true,
            SignMessage = false,
            EncryptMessage = false,
        };

        var captureTask = CaptureIncomingAS2MessageWithTimeout(TimeSpan.FromSeconds(30));

        await Task.Delay(500);

        var senderResult = await SendMessage.TestSendMessage(senderInput, senderConnection, senderOptions, CancellationToken.None);

        var (rawMessage, headers) = await captureTask;

        var receiverInput = new Input
        {
            RawMessage = rawMessage,
            SenderAs2Id = headers.GetValueOrDefault("AS2-From", senderInput.SenderAs2Id),
            ReceiverAs2Id = headers.GetValueOrDefault("AS2-To", senderInput.ReceiverAs2Id),
            MessageId = headers.GetValueOrDefault("Message-ID", ""),
        };

        var receiverConnection = new Connection
        {
            RequireSigned = false,
            RequireEncrypted = false,
            PartnerCertificatePath = null,
            OwnCertificatePath = null,
            OwnCertificatePassword = "receiver123",
        };

        var receiverOptions = new Options
        {
            ThrowErrorOnFailure = true,
            ErrorMessageOnFailure = null,
        };

        var receiverResult = await As2.ValidateAndParsePayload(receiverInput, receiverConnection, receiverOptions, CancellationToken.None);

        Assert.That(senderResult.Success, Is.True, "Sender should succeed");
        Assert.That(receiverResult.Success, Is.True, "Receiver should succeed");
        Assert.That(receiverResult.As2From, Is.EqualTo("TestSender"));
        Assert.That(receiverResult.As2To, Is.EqualTo("TestReceiver"));
        Assert.That(receiverResult.Payload, Is.Not.Null);
        Assert.That(receiverResult.MdnReceipt, Is.Not.Null);
    }

    //[Test]
    //public async Task ShouldReceiveAndParseSignedAndEncryptedMessage()
    //{
    //    var senderInput = new SenderInput
    //    {
    //        SenderAs2Id = "TestSender",
    //        ReceiverAs2Id = "TestReceiver",
    //        Subject = "Integration Test - Signed & Encrypted",
    //        MessageFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt"),
    //    };

    //    var senderConnection = new SenderConnection
    //    {
    //        As2EndpointUrl = _testEndpoint,
    //        SenderCertificatePassword = "sender123",
    //        SenderCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx"),
    //        ReceiverCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem"),
    //        ContentTypeHeader = "text/plain",
    //        MdnReceiver = "test@example.com",
    //    };

    //    var senderOptions = new SenderOptions
    //    {
    //        ThrowErrorOnFailure = true,
    //        SignMessage = true,
    //        EncryptMessage = true,
    //    };

    //    var captureTask = CaptureIncomingAS2Message();
    //    var senderResult = await SendMessage.TestSendMessage(senderInput, senderConnection, senderOptions, CancellationToken.None);
    //    var (rawMessage, headers) = await captureTask;

    //    var receiverInput = new Input
    //    {
    //        RawMessage = rawMessage,
    //        SenderAs2Id = headers.GetValueOrDefault("AS2-From", senderInput.SenderAs2Id),
    //        ReceiverAs2Id = headers.GetValueOrDefault("AS2-To", senderInput.ReceiverAs2Id),
    //        MessageId = headers.GetValueOrDefault("Message-ID", ""),
    //    };

    //    var receiverConnection = new Connection
    //    {
    //        RequireSigned = true,
    //        RequireEncrypted = true,
    //        PartnerCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pem"),
    //        OwnCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pfx"),
    //        OwnCertificatePassword = "receiver123",
    //    };

    //    var receiverOptions = new Options
    //    {
    //        ThrowErrorOnFailure = true,
    //    };

    //    var receiverResult = await As2.ValidateAndParsePayload(receiverInput, receiverConnection, receiverOptions, CancellationToken.None);

    //    Assert.That(senderResult.Success, Is.True);
    //    Assert.That(receiverResult.Success, Is.True);
    //    Assert.That(receiverResult.Payload, Is.Not.Null);
    //    Assert.That(receiverResult.MdnReceipt, Is.Not.Null);
    //}
}
