using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using NUnit.Framework;

namespace Frends.As2.SendMessage.Tests;

// Start the OpenAS2 Docker environment first: docker-compose up -d.
[TestFixture]
public class IntegrationTests
{
    private readonly string _testDataDir = Path.Combine(AppContext.BaseDirectory, "testData");
    private string _testFilePath;
    private string _currentTestId;

    [SetUp]
    public void Setup()
    {
        Directory.CreateDirectory(_testDataDir);
        _currentTestId = Guid.NewGuid().ToString("N");
        _testFilePath = Path.Combine(_testDataDir, $"message_{_currentTestId}.txt");

        File.WriteAllText(
            _testFilePath,
            $"AS2_TEST_{_currentTestId}: This is a unique test message at {DateTime.UtcNow:O}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDataDir))
            Directory.Delete(_testDataDir, true);
    }

    [Test]
    public async Task ShouldSendPlainMessage()
    {
        var result = await As2.SendMessage(Input(), Connection(), Options(), CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedMessage()
    {
        var con = Connection();
        con.SignMessage = true;

        var result = await As2.SendMessage(Input(), con, Options(), CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendEncryptedMessage()
    {
        var con = Connection();
        con.EncryptMessage = true;

        var result = await As2.SendMessage(Input(), con, Options(), CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedAndEncryptedMessage()
    {
        var con = Connection();
        con.SignMessage = true;
        con.EncryptMessage = true;

        var opt = Options();
        opt.MdnMode = MdnMode.Sync;
        opt.ThrowErrorOnFailure = true;

        var result = await As2.SendMessage(Input(), con, opt, CancellationToken.None);
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.False);
    }

    [Test]
    public async Task ShouldSendMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var mdnTask = StartMdnReceiver(cts.Token);

        var opt = Options();
        opt.MdnMode = MdnMode.Async;

        var result = await As2.SendMessage(Input(), Connection(), opt, CancellationToken.None);

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

        var opt = Options();
        opt.MdnMode = MdnMode.Async;

        var con = Connection();
        con.SignMessage = true;

        var result = await As2.SendMessage(Input(), con, opt, CancellationToken.None);

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

        var opt = Options();
        opt.MdnMode = MdnMode.Async;

        var con = Connection();
        con.EncryptMessage = true;

        var result = await As2.SendMessage(Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True);

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty);
        Assert.That(rawMdn, Does.Contain("Disposition:"));
    }

    [Test]
    public async Task ShouldFailWithInvalidEndpointUrl()
    {
        var con = Connection();
        con.As2EndpointUrl = "http://invalid-endpoint:9999";

        var opt = Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("No such host is known"));
    }

    [Test]
    public async Task ShouldFailWithInvalidCertificatePath()
    {
        var con = Connection();
        con.SenderCertificatePath = "invalid/path/sender.pfx";
        con.SignMessage = true;

        var opt = Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(
            result.Error.Message,
            Does.Contain("Cannot open certificate store: The system cannot find the file specified"));
    }

    [Test]
    public async Task ShouldFailWithInvalidMessageFilePath()
    {
        var input = Input();
        input.MessageFilePath = "invalid/path/message.txt";

        var opt = Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(input, Connection(), opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Could not find a part of the path"));
    }

    [Test]
    public async Task ShouldFailWhenAsyncMdnUrlIsMissing()
    {
        var opt = Options();
        opt.MdnMode = MdnMode.Async;
        opt.AsyncMdnUrl = null;
        opt.ThrowErrorOnFailure = false;
        var result = await As2.SendMessage(Input(), Connection(), opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("AsyncMdnUrl must be provided when MdnMode is set to Async"));
    }

    private static Connection Connection() =>
        new()
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

    private static Options Options() => new()
    {
        ThrowErrorOnFailure = false,
        ErrorMessageOnFailure = null,
        AsyncMdnUrl = "http://host.docker.internal:9090/mdn-receiver/",
    };

    private static async Task<string> StartMdnReceiver(CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://+:9090/mdn-receiver/");
        listener.Start();

        try
        {
            var context = await GetContextAsync(listener, token);
            if (context == null) return null;

            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            var rawMdn = await reader.ReadToEndAsync(token);

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync("OK"u8.ToArray(), token);
            context.Response.Close();

            return rawMdn;
        }
        finally
        {
            listener.Stop();
            listener.Close();
        }
    }

    private static async Task<HttpListenerContext> GetContextAsync(HttpListener listener, CancellationToken token)
    {
        var contextTask = listener.GetContextAsync();

        var tcs = new TaskCompletionSource<HttpListenerContext>();

        await using (token.Register(() => tcs.TrySetResult(null)))
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

    private Input Input() => new()
    {
        SenderAs2Id = "Sender",
        ReceiverAs2Id = "Receiver",
        Subject = "Test Connection",
        MessageFilePath = _testFilePath,
    };
}