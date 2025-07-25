using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Frends.As2.SendMessage.Helpers;

public static class Helpers
{
    public static byte[] EncryptEntity(MimeEntity entity, X509Certificate recipientCert)
    {
        var builder = new MimeMessage();
        builder.Body = entity;

        var stream = new MemoryStream();
        builder.WriteTo(stream);
        var contentBytes = stream.ToArray();

        var cmsData = new CmsProcessableByteArray(contentBytes);
        var gen = new CmsEnvelopedDataGenerator();
        gen.AddKeyTransRecipient(recipientCert);
        var enveloped = gen.Generate(cmsData, CmsEnvelopedGenerator.Aes256Cbc);

        return enveloped.GetEncoded();
    }

    public static MimeEntity SignEntity(MimeEntity entity, X509Certificate2 signingCert, string signingCertPassword)
    {
        var certParser = new X509CertificateParser();
        var bcCert = certParser.ReadCertificate(signingCert.RawData);

        AsymmetricKeyParameter privateKey;
        var store = new Pkcs12StoreBuilder().Build();
        using (var stream = new MemoryStream(signingCert.Export(X509ContentType.Pfx, signingCertPassword)))
        {
            store.Load(stream, signingCertPassword.ToCharArray());
            var alias = store.Aliases.First(a => store.IsKeyEntry(a));
            privateKey = store.GetKey(alias).Key;
        }

        var signer = new CmsSigner(bcCert, privateKey)
        {
            DigestAlgorithm = DigestAlgorithm.Sha1, // or DigestAlgorithm.Sha1 if required
        };
        var signedEntity = ApplicationPkcs7Mime.Sign(new DefaultSecureMimeContext(), signer, entity, CancellationToken.None);
        return signedEntity;
    }
}