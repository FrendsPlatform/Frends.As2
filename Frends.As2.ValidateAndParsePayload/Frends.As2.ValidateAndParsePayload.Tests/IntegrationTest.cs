using System;
using System.IO;
using System.Linq;
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
        var input = await TestSetup.SpecifiedInput(testDirName);
        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = requireEncrypted;
        connection.RequireSigned = requireSigned;

        if (testDirName is "AsyncSignedMessage" or "SignedMessage")
        {
            Console.WriteLine($"Bytes number : {input.Body.Length}");
            var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath(testDirName));

            Console.WriteLine($"Headers: {headersJson}");
        }

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payload, Is.EqualTo("This is a nice test message :)"));
    }

    [Test]
    public async Task Should_Fail_With_Incorrect_PartnerCert()
    {
        var input = await TestSetup.SpecifiedInput("SignedMessage");
        var connection = TestSetup.DefaultConnection();
        connection.RequireEncrypted = false;
        connection.RequireSigned = true;
        connection.PartnerCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem");

        var result =
            await As2.ValidateAndParsePayload(input, connection, TestSetup.DefaultOptions(), CancellationToken.None);

        Console.WriteLine($"Bytes number: {input.Body.Length}");
        var headersJson = await File.ReadAllTextAsync(TestSetup.TestHeadersFilePath("SignedMessage"));

        Console.WriteLine($"Headers: {headersJson}");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Unable to authenticate signature"));
    }

    [Test]
    public async Task Should_Fail_With_Incorrect_OwnCert()
    {
        var input = await TestSetup.SpecifiedInput("EncryptedMessage");
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
        var input = await TestSetup.SpecifiedInput("EncryptedMessage");
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