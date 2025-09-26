using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.ValidateAndParsePayload.Definitions;
using NUnit.Framework;

namespace Frends.As2.ValidateAndParsePayload.Tests;

public class IntegrationTest
{
    [TestCase("AsyncSignedMessage", true, false)]
    [TestCase("EncryptedMessage", false, true)]
    [TestCase("PlainMessage", false, false)]
    [TestCase("SignedAndEncryptedMessage", true, true)]
    [TestCase("SignedMessage", true, false)]
    public async Task Should_Succeed_With_Correct_Message(string testDirName, bool requireSigned, bool requireEncrypted)
    {
        var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath(testDirName));
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        var bodyBytes = await File.ReadAllBytesAsync(TestSetup.TestBodyFilePath(testDirName));

        var input = new Input
        {
            Headers = headers,
            Body = bodyBytes,
        };

        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = requireEncrypted;
        connection.RequireSigned = requireSigned;

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payload, Is.EqualTo("This is a nice test message :)"));
    }

    [Test]
    public async Task Should_Fail_With_Incorrect_PartnerCert()
    {
        var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath("SignedMessage"));
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        var bodyBytes = await File.ReadAllBytesAsync(TestSetup.TestBodyFilePath("SignedMessage"));

        var input = new Input
        {
            Headers = headers,
            Body = bodyBytes,
        };

        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = false;
        connection.RequireSigned = true;
        connection.PartnerCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem");

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Unable to authenticate signature"));
    }

    [Test]
    public async Task Should_Fail_With_Incorrect_OwnCert()
    {
        var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath("EncryptedMessage"));
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        var bodyBytes = await File.ReadAllBytesAsync(TestSetup.TestBodyFilePath("EncryptedMessage"));

        var input = new Input
        {
            Headers = headers,
            Body = bodyBytes,
        };

        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = true;
        connection.RequireSigned = false;
        connection.OwnCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx");
        connection.OwnCertificatePassword = "sender123";

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Unable to decrypt message"));
    }

    [Test]
    public async Task Should_Fail_With_Incorrect_OwnCert_Password()
    {
        var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath("EncryptedMessage"));
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        var bodyBytes = await File.ReadAllBytesAsync(TestSetup.TestBodyFilePath("EncryptedMessage"));

        var input = new Input
        {
            Headers = headers,
            Body = bodyBytes,
        };

        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = true;
        connection.RequireSigned = false;
        connection.OwnCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pfx");
        connection.OwnCertificatePassword = "invalidPassword";

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Cannot open certificate store"));
    }

    [Test]
    public void Should_Throw_Error_When_Flag_Enabled()
    {
        var input = new Input
        {
            Headers = [],
            Body = "message".Select(x => (byte)x).ToArray(),
        };
        var options = TestSetup.DefaultOptions();
        options.ThrowErrorOnFailure = true;

        Assert.ThrowsAsync<Exception>(async () =>
            await As2.ValidateAndParsePayload(input, TestSetup.DefaultConnection(), options, CancellationToken.None));
    }

    [Test]
    public async Task Should_Return_Failure_With_Custom_Error_Message()
    {
        var input = new Input
        {
            Headers = [],
            Body = "foobar".Select(x => (byte)x).ToArray(),
        };

        var result =
            await As2.ValidateAndParsePayload(
                input,
                TestSetup.DefaultConnection(),
                TestSetup.DefaultOptions(),
                CancellationToken.None);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Error occured"));
    }
}