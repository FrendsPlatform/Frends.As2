using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Frends.As2.SendMessage.Helpers;
using NUnit.Framework;
using Org.BouncyCastle.Cms;

namespace Frends.As2.SendMessage.Tests;

[TestFixture]
public class UnitTests
{
    private readonly string signingCertPath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pem");
    private readonly string signingPfxPath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pfx");
    private readonly string receiverCertPath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pem");
    private readonly string receiverPfxPath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pfx");

    [Test]
    public async Task ShouldEncryptAndDecryptMessage()
    {
        var input = new byte[] { 1, 2, 3, 4, 5 };
        var encrypted = await input.Encrypt(receiverCertPath, CmsEnvelopedGenerator.Aes256Cbc, CancellationToken.None);
        var decrypted = encrypted.Decrypt(receiverPfxPath, "receiver123");
        Assert.That(decrypted, Is.EqualTo(input));
    }

    [Test]
    public void ShouldSignAndVerifyMessage()
    {
        var input = new byte[] { 1, 2, 3, 4, 5 };
        var signed = input.Sign(signingPfxPath, "sender123", CmsSignedGenerator.DigestSha512);
        var signedContent = signed.Encode();
        var verified = signedContent.VerifySignature(signingCertPath);
        Assert.That(verified, Is.EqualTo(input));
    }
}