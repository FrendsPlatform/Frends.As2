using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.ValidateAndParsePayload;
using Frends.As2.ValidateAndParsePayload.Definitions;
using Frends.As2.ValidateAndParsePayload.Tests.Helpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

public class IntegrationTest
{
    private readonly string _certsPath = Path.Combine(AppContext.BaseDirectory, "certs");
    private readonly object _captureLock = new object();
    private readonly string _testDataDir = Path.Combine(AppContext.BaseDirectory, "testData");
    private HttpListener _httpListener;
    private string _testServerUrl = "http://localhost:8081/as2";
    private string _testFilePath;
    private string _currentTestId;
    private CancellationTokenSource _serverCancellationTokenSource;
    private Dictionary<string, string> _lastCapturedHeaders;
    private byte[] _lastCapturedBody;
    private Connection _serverConnection;

    [SetUp]
    public void Setup()
    {
        Directory.CreateDirectory(_testDataDir);
        _currentTestId = Guid.NewGuid().ToString("N");
        _testFilePath = Path.Combine(_testDataDir, $"message_{_currentTestId}.txt");

        var uniqueContent = $"AS2_TEST_{_currentTestId}: This is a unique test message generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        File.WriteAllText(_testFilePath, uniqueContent);

        StartTestAS2Server();
    }

    [TearDown]
    public void TearDown()
    {
        _serverCancellationTokenSource?.Cancel();
        _httpListener?.Stop();
        _httpListener?.Close();
    }

    [Test]
    public async Task Should_Succeed_When_Signed_And_Encrypted_And_Required()
    {
        _serverConnection = BuildServerConnection(requireSigned: true, requireEncrypted: true);
        var senderResult = await SendSuccessfulMessage(true, true);

        Assert.That(senderResult.Success, Is.True, $"Message should succeed with Sign={true}, Encrypt={true}");
        Assert.That(senderResult.MDNStatus, Is.Not.Null.Or.Empty, "MDN status should be returned");
        Assert.That(senderResult.MDNIntegrityCheck, Is.Not.Null.Or.Empty, "MDN integrity check should be present");
    }

    [Test]
    public async Task Should_Fail_When_Message_Not_Encrypted_But_Encryption_Required()
    {
        _serverConnection = BuildServerConnection(requireSigned: false, requireEncrypted: true);

        var senderResult = await SendSuccessfulMessage(sign: false, encrypt: false);

        Assert.That(senderResult.Success, Is.False, "Message should fail because it is not encrypted");
        Assert.That(senderResult.Error, Is.Not.Null);
    }

    [Test]
    public async Task Should_Fail_When_Message_Not_Signed_But_Signature_Required()
    {
        _serverConnection = BuildServerConnection(requireSigned: true, requireEncrypted: false);

        var senderResult = await SendSuccessfulMessage(sign: false, encrypt: false);

        Assert.That(senderResult.Success, Is.False, "Message should fail because it is not signed");
        Assert.That(senderResult.Error, Is.Not.Null);
    }

    [Test]
    public async Task Should_Fail_When_Signature_Does_Not_Match_Receiver_Certificate()
    {
        _serverConnection = BuildServerConnection(requireSigned: true, requireEncrypted: false);
        var senderResult = await SendSuccessfulMessage(sign: true, encrypt: false);
        Assert.That(senderResult.Success, Is.True, "Message should be sent successfully");

        _serverConnection.PartnerCertificatePath = Path.Combine(_certsPath, "wrong_sender.pem");

        var input = new Input
        {
            Headers = _lastCapturedHeaders,
            Body = _lastCapturedBody,
        };

        var options = new Options { ThrowErrorOnFailure = false };
        var receiverResult = await As2.ValidateAndParsePayload(input, _serverConnection, options, CancellationToken.None);

        Assert.That(receiverResult.Success, Is.False, "Signature verification should fail with unmatched certificate");
        Assert.That(receiverResult.Error, Is.Not.Null.And.Not.Empty, "Error should indicate that the certificate did not match");
    }


    [Test]
    public async Task Should_Fail_When_Encrypted_Message_Cannot_Be_Decrypted()
    {
        _serverConnection = BuildServerConnection(requireSigned: false, requireEncrypted: true);
        var senderResult = await SendSuccessfulMessage(sign: false, encrypt: true);
        Assert.That(senderResult.Success, Is.True, "Encrypted message should be sent successfully");

        await Task.Delay(200);

        var receiverResult = await SendMessageWithWrongDecryption();

        Assert.That(receiverResult.Success, Is.False, "Decryption should fail with wrong private key");
        Assert.That(receiverResult.Error, Is.Not.Null, "This certificate cannot be used to decrypt this message");
    }

    //private async Task<Result> SendMessageWithWrongCert()
    //{
    //    if (_lastCapturedHeaders == null || _lastCapturedBody == null)
    //        throw new Exception("No request captured yet.");

    //    var input = new Input
    //    {
    //        Headers = _lastCapturedHeaders,
    //        Body = _lastCapturedBody,
    //    };

    //    var connection = new Connection
    //    {
    //        RequireSigned = true,
    //        RequireEncrypted = false,
    //        PartnerCertificatePath = Path.Combine(_certsPath, "wrong_sender.pem"), // intentionally wrong
    //        OwnCertificatePath = Path.Combine(_certsPath, "receiver.pfx"),
    //        OwnCertificatePassword = "receiver123",
    //    };

    //    var options = new Options { ThrowErrorOnFailure = false };

    //    return await As2.ValidateAndParsePayload(input, connection, options, CancellationToken.None);
    //}

    private async Task<Result> SendMessageWithWrongDecryption()
    {
        if (_lastCapturedHeaders == null || _lastCapturedBody == null)
            throw new Exception("No request captured yet.");

        var input = new Input
        {
            Headers = _lastCapturedHeaders,
            Body = _lastCapturedBody,
        };

        var connection = new Connection
        {
            RequireSigned = false,
            RequireEncrypted = true,
            PartnerCertificatePath = Path.Combine(_certsPath, "sender.pem"),
            OwnCertificatePath = Path.Combine(_certsPath, "wrong_receiver.pfx"),
            OwnCertificatePassword = "wrongpassword",
        };

        var options = new Options { ThrowErrorOnFailure = false };

        return await As2.ValidateAndParsePayload(input, connection, options, CancellationToken.None);
    }

    private async Task<SenderResult> SendSuccessfulMessage(bool sign = true, bool encrypt = true)
    {
        var senderInput = new SenderInput
        {
            SenderAs2Id = "TestSender",
            ReceiverAs2Id = "TestReceiver",
            Subject = "Test AS2 Message",
            MessageFilePath = _testFilePath,
        };

        var senderConnection = new SenderConnection
        {
            As2EndpointUrl = _testServerUrl,
            SenderCertificatePath = Path.Combine(_certsPath, "sender.pfx"),
            SenderCertificatePassword = "sender123",
            ReceiverCertificatePath = Path.Combine(_certsPath, "receiver.pem"),
            ContentTypeHeader = "text/plain",
            MdnReceiver = "usr@example.com",
        };

        var senderOptions = new SenderOptions
        {
            ThrowErrorOnFailure = false,
            SignMessage = sign,
            EncryptMessage = encrypt,
        };

        return await Helpers.SendMessage(senderInput, senderConnection, senderOptions, CancellationToken.None);
    }

    private void StartTestAS2Server()
    {
        _serverCancellationTokenSource = new CancellationTokenSource();
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"{_testServerUrl}/");
        _httpListener.Start();

        Task.Run(async () => await HandleIncomingRequests(_serverCancellationTokenSource.Token));
    }

    private async Task HandleIncomingRequests(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener.IsListening)
        {
            try
            {
                var context = await GetContextAsync(_httpListener, cancellationToken);
                if (context == null) continue;

                await ProcessAS2Request(context);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
            }
        }
    }

    private async Task<HttpListenerContext> GetContextAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        try
        {
            var contextTask = listener.GetContextAsync();
            var tcs = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                var completedTask = await Task.WhenAny(contextTask, tcs.Task);
                if (completedTask == tcs.Task)
                {
                    return null;
                }

                return await contextTask;
            }
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    private async Task ProcessAS2Request(HttpListenerContext context)
    {
        try
        {
            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 405;
                return;
            }

            byte[] bodyBytes;
            using (var ms = new MemoryStream())
            {
                await context.Request.InputStream.CopyToAsync(ms);
                bodyBytes = ms.ToArray();
            }

            var headers = context.Request.Headers
                .AllKeys
                .ToDictionary(key => key, key => context.Request.Headers[key], StringComparer.OrdinalIgnoreCase);

            var input = new Input
            {
                Headers = headers,
                Body = bodyBytes,
            };

            var connection = _serverConnection ?? new Connection
            {
                RequireSigned = true,
                RequireEncrypted = true,
                PartnerCertificatePath = Path.Combine(_certsPath, "sender.pem"),
                OwnCertificatePath = Path.Combine(_certsPath, "receiver.pfx"),
                OwnCertificatePassword = "receiver123",
            };

            var options = new Options { ThrowErrorOnFailure = false };

            lock (_captureLock)
            {
                _lastCapturedHeaders = headers;
                _lastCapturedBody = bodyBytes;
            }

            var result = await As2.ValidateAndParsePayload(input, connection, options, CancellationToken.None);

            if (result.Success && result.MdnReceipt != null)
            {
                var mdnHeaders = result.MdnReceipt.Headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var header in mdnHeaders)
                {
                    var parts = header.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                        context.Response.Headers.Add(parts[0].Trim(), parts[1].Trim());
                }

                var mdnContent = result.MdnReceipt.ContentB ?? Encoding.UTF8.GetBytes(result.MdnReceipt.Content ?? string.Empty);
                context.Response.ContentLength64 = mdnContent.Length;
                await context.Response.OutputStream.WriteAsync(mdnContent, 0, mdnContent.Length);
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 400;
                var errorBytes = Encoding.UTF8.GetBytes("Failed to process AS2 message");
                await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            var errorBytes = Encoding.UTF8.GetBytes($"Server error: {ex.Message}");
            await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private Connection BuildServerConnection(
        bool requireSigned,
        bool requireEncrypted,
        string partnerCertFile = "sender.pem",
        string ownCertFile = "receiver.pfx",
        string ownCertPassword = "receiver123")
    {
        return new Connection
        {
            RequireSigned = requireSigned,
            RequireEncrypted = requireEncrypted,
            PartnerCertificatePath = Path.Combine(_certsPath, partnerCertFile),
            OwnCertificatePath = Path.Combine(_certsPath, ownCertFile),
            OwnCertificatePassword = ownCertPassword,
        };
    }
}