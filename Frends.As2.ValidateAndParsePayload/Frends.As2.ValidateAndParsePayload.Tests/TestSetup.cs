using System;
using System.IO;
using Frends.As2.ValidateAndParsePayload.Definitions;

namespace Frends.As2.ValidateAndParsePayload.Tests;

public static class TestSetup
{
    public static Options DefaultOptions()
    {
        return new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = "Error occured",
        };
    }

    public static Connection DefaultConnection()
    {
        return new Connection
        {
            RequireSigned = false,
            RequireEncrypted = false,
            OwnCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "receiver.pfx"),
            OwnCertificatePassword = "receiver123",
            PartnerCertificatePath = Path.Combine(AppContext.BaseDirectory, "certs", "sender.pem"),
        };
    }

    public static string TestBodyFilePath(string testDirName)
    {
        return Path.Combine(AppContext.BaseDirectory, "testData", testDirName, "body.bin");
    }

    public static string TestHeadersFilePath(string testDirName)
    {
        return Path.Combine(AppContext.BaseDirectory, "testData", testDirName, "headers.json");
    }
}