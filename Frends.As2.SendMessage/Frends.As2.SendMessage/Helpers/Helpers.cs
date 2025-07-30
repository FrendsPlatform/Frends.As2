using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;

namespace Frends.As2.SendMessage.Helpers;

public static class Helpers
{
    public static byte[] Sign(this byte[] data, string signingCertPath, string signingCertPassword, string algorithmOid)
    {
        var signingCert = new X509Certificate2(signingCertPath, signingCertPassword);

        var contentInfo = new ContentInfo(data);
        var signedCms = new SignedCms(contentInfo, detached: false);
        var cmsSigner = new CmsSigner(signingCert)
        {
            DigestAlgorithm = new Oid(algorithmOid),
        };
        signedCms.ComputeSignature(cmsSigner);
        return signedCms.Encode();
    }

    public static async Task<byte[]> Encrypt(
        this byte[] data,
        string receiverCertificatePath,
        string algorithmOid,
        CancellationToken cancellationToken)
    {
        var receiverCertBytes = await File.ReadAllBytesAsync(receiverCertificatePath, cancellationToken);
        var receiverCert = new X509CertificateParser().ReadCertificate(receiverCertBytes);

        var cmsData = new CmsProcessableByteArray(data);
        var generator = new CmsEnvelopedDataGenerator();
        generator.AddKeyTransRecipient(receiverCert);
        var enveloped = generator.Generate(cmsData, algorithmOid);
        return enveloped.GetEncoded();
    }

    public static byte[] Decrypt(
        this byte[] encryptedData,
        string receiverPfxPath,
        string receiverPfxPassword)
    {
        var pfxBytes = File.ReadAllBytes(receiverPfxPath);
        var store = new Pkcs12StoreBuilder().Build();
        using (var stream = new MemoryStream(pfxBytes))
        {
            store.Load(stream, receiverPfxPassword.ToCharArray());
        }

        // Get the first alias with a private key
        var alias = store.Aliases.FirstOrDefault(store.IsKeyEntry);
        var privateKey = store.GetKey(alias).Key;
        var cert = store.GetCertificate(alias).Certificate;

        // Parse the encrypted data
        var envelopedData = new CmsEnvelopedData(encryptedData);

        // Find the recipient info that matches our certificate
        var recipientInfos = envelopedData.GetRecipientInfos();
        var recipientId = new RecipientID
        {
            Issuer = cert.IssuerDN,
            SerialNumber = cert.SerialNumber,
        };

        var recipient = recipientInfos.GetFirstRecipient(recipientId);

        // Decrypt
        var decrypted = recipient.GetContent(privateKey);
        return decrypted;
    }

    public static byte[] VerifySignature(
        this byte[] data,
        string caCertPath)
    {
        var caCert = new X509Certificate2(caCertPath);
        var signedCms = new SignedCms();
        signedCms.Decode(data);

        var extraStore = new X509Certificate2Collection { caCert };
        signedCms.CheckSignature(extraStore, true);
        return signedCms.ContentInfo.Content;
    }
}