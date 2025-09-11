using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using NUnit.Framework;

namespace Frends.As2.SendMessage.Tests;

/// <summary>
/// Start the OpenAS2 Docker environment first: docker-compose up -d.
/// </summary>
[TestFixture]
public class IntegrationTests
{
    private static Input input = new()
    {
        SenderAs2Id = "Sender",
        ReceiverAs2Id = "Receiver",
        Subject = "Test Connection",
        MessageFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt"),
    };

    private static Connection connection = new()
    {
        As2EndpointUrl = "http://localhost:4080",
        SignMessage = false,
        EncryptMessage = false,
        SenderCertificatePassword = "sender123",
        SenderCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx"),
        ReceiverCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem"),
        ContentTypeHeader = "text/plain",
        MdnReceiver = "usr@example.com",
    };

    private static Options options = new()
    {
        ThrowErrorOnFailure = false,
        ErrorMessageOnFailure = null,
    };

    [Test]
    public async Task ShouldSendPlainMessage()
    {
        var result = await As2.SendMessage(input, connection, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedMessage()
    {
        var con = connection;
        con.SignMessage = true;

        var result = await As2.SendMessage(input, con, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendEncryptedMessage()
    {
        var con = connection;
        con.EncryptMessage = true;

        var result = await As2.SendMessage(input, con, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedAndEncryptedMessage()
    {
        var con = connection;
        con.SignMessage = true;
        con.EncryptMessage = true;

        var opt = options;
        opt.MdnMode = MdnMode.Sync;
        opt.ThrowErrorOnFailure = true;

        var result = await As2.SendMessage(input, con, opt, CancellationToken.None);
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.False);
    }

    [Test]
    public async Task ShouldSendMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var mdnTask = StartMdnReceiver(cts.Token);

        var opt = options;
        opt.MdnMode = MdnMode.Async;
        opt.AsyncMdnUrl = "http://host.docker.internal:9090/mdn-receiver/";

        var result = await As2.SendMessage(input, connection, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True, "Async MDN should be pending");

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty, "Async MDN should have been posted");
        Assert.That(rawMdn, Does.Contain("Disposition:"), "MDN should contain Disposition header");
    }

    [Test]
    public async Task ShouldSendSignedMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var mdnTask = StartMdnReceiver(cts.Token);

        var opt = options;
        opt.MdnMode = MdnMode.Async;
        opt.AsyncMdnUrl = "http://host.docker.internal:9090/mdn-receiver/";

        var con = connection;
        con.SignMessage = true;

        var result = await As2.SendMessage(input, con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True, "Async MDN should be pending");

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty, "Async MDN should have been posted to listener");
        Assert.That(rawMdn, Does.Contain("Disposition:"), "MDN should contain Disposition header");
    }

    [Test]
    public async Task ShouldSendEncryptedMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var mdnTask = StartMdnReceiver(cts.Token);

        var opt = options;
        opt.MdnMode = MdnMode.Async;
        opt.AsyncMdnUrl = "http://host.docker.internal:9090/mdn-receiver/";

        var con = connection;
        con.EncryptMessage = true;

        var result = await As2.SendMessage(input, con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True);

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty);
        Assert.That(rawMdn, Does.Contain("Disposition:"));
    }

    [Test]
    public async Task ShouldFailWithInvalidEndpointUrl()
    {
        var invalidConnection = new Connection
        {
            As2EndpointUrl = "http://invalid-endpoint:9999",
            SenderCertificatePassword = connection.SenderCertificatePassword,
            SenderCertificatePath = connection.SenderCertificatePath,
            ReceiverCertificatePath = connection.ReceiverCertificatePath,
            ContentTypeHeader = connection.ContentTypeHeader,
            MdnReceiver = connection.MdnReceiver,
            SignMessage = false,
            EncryptMessage = false,
        };

        var opt = options;
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(input, invalidConnection, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("No such host is known"));
    }

    [Test]
    public async Task ShouldFailWithInvalidCertificatePath()
    {
        var invalidConnection = new Connection
        {
            As2EndpointUrl = connection.As2EndpointUrl,
            SenderCertificatePassword = connection.SenderCertificatePassword,
            SenderCertificatePath = "invalid/path/sender.pfx",
            ReceiverCertificatePath = connection.ReceiverCertificatePath,
            ContentTypeHeader = connection.ContentTypeHeader,
            MdnReceiver = connection.MdnReceiver,
            SignMessage = true,
        };

        var opt = options;
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(input, invalidConnection, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Cannot open certificate store: The system cannot find the file specified"));
    }

    [Test]
    public async Task ShouldFailWithInvalidMessageFilePath()
    {
        var invalidInput = new Input
        {
            SenderAs2Id = input.SenderAs2Id,
            ReceiverAs2Id = input.ReceiverAs2Id,
            Subject = input.Subject,
            MessageFilePath = "invalid/path/message.txt",
        };

        var opt = options;
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(invalidInput, connection, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Could not find a part of the path"));
    }

    private async Task<string> StartMdnReceiver(CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://+:9090/mdn-receiver/");
        listener.Start();

        try
        {
            var context = await GetContextAsync(listener, token);
            if (context == null) return null;

            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            string rawMdn = await reader.ReadToEndAsync();

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
            context.Response.Close();

            return rawMdn;
        }
        finally
        {
            listener.Stop();
            listener.Close();
        }
    }

    private async Task<HttpListenerContext> GetContextAsync(HttpListener listener, CancellationToken token)
    {
        var contextTask = listener.GetContextAsync();

        var tcs = new TaskCompletionSource<HttpListenerContext>();

        using (token.Register(() => tcs.TrySetResult(null)))
        {
            var completedTask = await Task.WhenAny(contextTask, tcs.Task);

            if (completedTask == tcs.Task)
            {
                return null;
            }

            try
            {
                return await contextTask;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                return null;
            }
        }
    }
}