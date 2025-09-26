using System;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Definitions;
using NUnit.Framework;

namespace Frends.As2.SendMessage.Tests;

// Start the OpenAS2 Docker environment first: docker-compose up -d.
[TestFixture]
public class IntegrationTests
{
    [Test]
    public async Task ShouldSendPlainMessage()
    {
        var result = await As2.SendMessage(
            TestSetup.Input(),
            TestSetup.Connection(),
            TestSetup.Options(),
            CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedMessage()
    {
        var con = TestSetup.Connection();
        con.SignMessage = true;

        var result = await As2.SendMessage(TestSetup.Input(), con, TestSetup.Options(), CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendEncryptedMessage()
    {
        var con = TestSetup.Connection();
        con.EncryptMessage = true;

        var result = await As2.SendMessage(TestSetup.Input(), con, TestSetup.Options(), CancellationToken.None);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ShouldSendSignedAndEncryptedMessage()
    {
        var con = TestSetup.Connection();
        con.SignMessage = true;
        con.EncryptMessage = true;

        var opt = TestSetup.Options();
        opt.MdnMode = MdnMode.Sync;
        opt.ThrowErrorOnFailure = true;

        var result = await As2.SendMessage(TestSetup.Input(), con, opt, CancellationToken.None);
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.False);
    }

    [Test]
    public async Task ShouldSendMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var mdnTask = TestSetup.StartMdnReceiver(cts.Token);

        var opt = TestSetup.Options();
        opt.MdnMode = MdnMode.Async;

        var result = await As2.SendMessage(TestSetup.Input(), TestSetup.Connection(), opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True, "Async MDN should be pending");

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty, "Async MDN should have been posted");
        Assert.That(rawMdn, Does.Contain("Disposition:"), "MDN should contain Disposition header");
    }

    [Test]
    public async Task ShouldSendSignedMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var mdnTask = TestSetup.StartMdnReceiver(cts.Token);

        var opt = TestSetup.Options();
        opt.MdnMode = MdnMode.Async;

        var con = TestSetup.Connection();
        con.SignMessage = true;

        var result = await As2.SendMessage(TestSetup.Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True, "Async MDN should be pending");

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty, "Async MDN should have been posted to listener");
        Assert.That(rawMdn, Does.Contain("Disposition:"), "MDN should contain Disposition header");
    }

    [Test]
    public async Task ShouldSendEncryptedMessageWithAsyncMdn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var mdnTask = TestSetup.StartMdnReceiver(cts.Token);

        var opt = TestSetup.Options();
        opt.MdnMode = MdnMode.Async;

        var con = TestSetup.Connection();
        con.EncryptMessage = true;

        var result = await As2.SendMessage(TestSetup.Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsMdnPending, Is.True);

        var rawMdn = await mdnTask;

        Assert.That(rawMdn, Is.Not.Null.And.Not.Empty);
        Assert.That(rawMdn, Does.Contain("Disposition:"));
    }

    [Test]
    public async Task ShouldFailWithInvalidEndpointUrl()
    {
        var con = TestSetup.Connection();
        con.As2EndpointUrl = "http://invalid-endpoint:9999";

        var opt = TestSetup.Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(TestSetup.Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        var expectedMessage = OperatingSystem.IsWindows()
            ? "No such host is known"
            : "System error: Resource temporarily unavailable";
        Assert.That(result.Error.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public async Task ShouldFailWithInvalidCertificatePath()
    {
        var con = TestSetup.Connection();
        con.SenderCertificatePath = "invalid/path/sender.pfx";
        con.SignMessage = true;

        var opt = TestSetup.Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(TestSetup.Input(), con, opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        var expectedMessage = OperatingSystem.IsWindows()
            ? "Cannot open certificate store: The system cannot find the file specified"
            : "The storeName value was invalid.";
        Assert.That(result.Error.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public async Task ShouldFailWithInvalidMessageFilePath()
    {
        var input = TestSetup.Input();
        input.MessageFilePath = "invalid/path/message.txt";

        var opt = TestSetup.Options();
        opt.ThrowErrorOnFailure = false;

        var result = await As2.SendMessage(input, TestSetup.Connection(), opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Could not find a part of the path"));
    }

    [Test]
    public async Task ShouldFailWhenAsyncMdnUrlIsMissing()
    {
        var opt = TestSetup.Options();
        opt.MdnMode = MdnMode.Async;
        opt.AsyncMdnUrl = null;
        opt.ThrowErrorOnFailure = false;
        var result = await As2.SendMessage(TestSetup.Input(), TestSetup.Connection(), opt, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("AsyncMdnUrl must be provided when MdnMode is set to Async"));
    }
}