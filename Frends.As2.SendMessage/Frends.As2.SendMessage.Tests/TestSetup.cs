using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;

namespace Frends.As2.SendMessage.Tests;

public static class TestSetup
{
    private static readonly string TestFilePath = Path.Combine(AppContext.BaseDirectory, "testData", "mess.txt");

    public static Input Input() => new()
    {
        SenderAs2Id = "Sender",
        ReceiverAs2Id = "Receiver",
        Subject = "Test Connection",
        MessageFilePath = TestFilePath,
    };

    public static Connection Connection() =>
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

    public static Connection HttpsConnection()
    {
        var connection = Connection();
        connection.As2EndpointUrl = "https://localhost:4443";

        return connection;
    }

    public static async Task<string> GetOpenAs2ServerCertificateBase64Async(CancellationToken token)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("localhost", 4443, token);

        await using var ssl = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
        await ssl.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
            }, token);

        using var certificate = new X509Certificate2(ssl.RemoteCertificate);

        return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
    }

    public static Options Options() => new()
    {
        ThrowErrorOnFailure = false,
        ErrorMessageOnFailure = null,
        AsyncMdnUrl = "http://host.docker.internal:9090/mdn-receiver/",
    };

    public static async Task<string> StartMdnReceiver(CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(OperatingSystem.IsWindows()
            ? "http://+:9090/mdn-receiver/"
            : "http://*:9090/mdn-receiver/");
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
}
