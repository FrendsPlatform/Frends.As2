using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Cms;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Frends.As2.SendMessage.Helpers;

public static class Helpers
{
    public static byte[] Sign(this byte[] data, X509Certificate2 signingCert, string algorithmOid)
    {
        var contentInfo = new ContentInfo(data);
        var signedCms = new SignedCms(contentInfo, detached: false);
        var cmsSigner = new CmsSigner(signingCert)
        {
            DigestAlgorithm = new Oid(algorithmOid),
        };
        signedCms.ComputeSignature(cmsSigner);
        return signedCms.Encode();
    }

    public static byte[] Encrypt(this byte[] data, X509Certificate receiverCert,  string algorithmOid)
    {
        var cmsData = new CmsProcessableByteArray(data);
        var generator = new CmsEnvelopedDataGenerator();
        generator.AddKeyTransRecipient(receiverCert);
        var enveloped = generator.Generate(cmsData, algorithmOid);
        return enveloped.GetEncoded();
    }
}