using System;
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

/// <summary>
/// Methods for signing, encrypting, decrypting, and verifying messages.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Extension method to sign data using a certificate.
    /// </summary>
    /// <param name="data">bytes to sign</param>
    /// <param name="signingCertPath">path to certificate</param>
    /// <param name="signingCertPassword">password for certificate</param>
    /// <param name="algorithmOid">used algorithm Oid</param>
    /// <returns>byte[]</returns>
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

    /// <summary>
    /// Extension method to encrypt data using a receiver's certificate.
    /// </summary>
    /// <param name="data">bytes to encrypt</param>
    /// <param name="receiverCertificatePath">path to file</param>
    /// <param name="algorithmOid">algorithm Oid</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>byte[]</returns>
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

    /// <summary>
    /// Extension method to decrypt data using a receiver's private key from a PFX file.
    /// </summary>
    /// <param name="encryptedData">data to decrypt</param>
    /// <param name="receiverPfxPath">path to file</param>
    /// <param name="receiverPfxPassword">password to file</param>
    /// <returns>byte[]</returns>
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

        var alias = store.Aliases.FirstOrDefault(store.IsKeyEntry);
        if (alias is null) throw new InvalidOperationException("No private key found in the PFX certificate.");

        var privateKey = store.GetKey(alias).Key;
        var cert = store.GetCertificate(alias).Certificate;

        var envelopedData = new CmsEnvelopedData(encryptedData);

        var recipientInfos = envelopedData.GetRecipientInfos();
        var recipientId = new RecipientID
        {
            Issuer = cert.IssuerDN,
            SerialNumber = cert.SerialNumber,
        };

        var recipient = recipientInfos.GetFirstRecipient(recipientId);

        var decrypted = recipient.GetContent(privateKey);
        return decrypted;
    }

    /// <summary>
    /// Extension method to verify a signed message using a CA certificate.
    /// </summary>
    /// <param name="data">bytes to verify</param>
    /// <param name="caCertPath">path to file</param>
    /// <returns>byte[]</returns>
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