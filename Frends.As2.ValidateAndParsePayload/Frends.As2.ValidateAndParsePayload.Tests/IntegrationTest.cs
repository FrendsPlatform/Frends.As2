using System;
using System.IO;
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
    private string _lastCapturedRawMessage;
    private CancellationTokenSource _serverCancellationTokenSource;

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
    public async Task Should_Send_And_Receive_AS2_Message_Successfully()
    {
        var senderResult = await SendSuccessfulMessage();

        Assert.That(senderResult.Success, Is.True, "AS2 message should be sent successfully");
    }

    private async Task<SenderResult> SendSuccessfulMessage()
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
            ThrowErrorOnFailure = true,
            SignMessage = true,
            EncryptMessage = true,
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
                context.Response.Close();
                return;
            }

            byte[] bodyBytes;
            using (var ms = new MemoryStream())
            {
                await context.Request.InputStream.CopyToAsync(ms);
                bodyBytes = ms.ToArray();
            }

            var headersBuilder = new StringBuilder();
            for (int i = 0; i < context.Request.Headers.Count; i++)
            {
                var key = context.Request.Headers.GetKey(i);
                var value = context.Request.Headers.Get(i);
                headersBuilder.AppendLine($"{key}:{value}");
            }

            var rawMessageBytes = Encoding.UTF8.GetBytes(headersBuilder.ToString() + "\r\n");
            var fullRawMessageBytes = new byte[rawMessageBytes.Length + bodyBytes.Length];
            Array.Copy(rawMessageBytes, 0, fullRawMessageBytes, 0, rawMessageBytes.Length);
            Array.Copy(bodyBytes, 0, fullRawMessageBytes, rawMessageBytes.Length, bodyBytes.Length);

            var fullRawMessage = Encoding.GetEncoding("ISO-8859-1").GetString(fullRawMessageBytes);

            lock (_captureLock)
            {
                _lastCapturedRawMessage = fullRawMessage;
            }

            var as2From = context.Request.Headers["AS2-From"]?.Trim('"') ?? "TestSender";
            var as2To = context.Request.Headers["AS2-To"]?.Trim('"') ?? "TestReceiver";
            var messageId = context.Request.Headers["Message-ID"]?.Trim('<', '>') ?? Guid.NewGuid().ToString();

            var receiverInput = new Input
            {
                RawMessage = fullRawMessage,
                SenderAs2Id = as2From,
                ReceiverAs2Id = as2To,
                MessageId = messageId,
            };

            var receiverConnection = new Connection
            {
                RequireSigned = true,
                RequireEncrypted = true,
                PartnerCertificatePath = Path.Combine(_certsPath, "sender.pem"),
                OwnCertificatePath = Path.Combine(_certsPath, "receiver.pfx"),
                OwnCertificatePassword = "receiver123",
            };

            var receiverOptions = new Options
            {
                ThrowErrorOnFailure = false,
            };

            var result = await As2.ValidateAndParsePayload(
                receiverInput, receiverConnection, receiverOptions, CancellationToken.None);

            if (result.Success && result.MdnReceipt != null)
            {
                var mdnHeaders = result.MdnReceipt.Headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var header in mdnHeaders)
                {
                    var parts = header.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        context.Response.Headers.Add(parts[0].Trim(), parts[1].Trim());
                    }
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
            Console.WriteLine($"Error processing AS2 request: {ex.Message}");
            context.Response.StatusCode = 500;
            var errorBytes = Encoding.UTF8.GetBytes($"Server error: {ex.Message}");
            await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
        }
        finally
        {
            context.Response.Close();
        }
    }
}