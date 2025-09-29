using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Frends.As2.ValidateAndParsePayload.Definitions;

namespace Frends.As2.ValidateAndParsePayload.Tests;

public static class TestSetup
{
    public static Options DefaultOptions()
    {
        return new Options
        {
            ThrowErrorOnFailure = true,
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

    public static async Task<Input> SpecifiedInput(string testDirName)
    {
        var headersJson = await File.ReadAllTextAsync(TestHeadersFilePath(testDirName));
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        var bodyBytes = await File.ReadAllBytesAsync(TestBodyFilePath(testDirName));

        return new Input
        {
            Headers = headers,
            Body = bodyBytes,
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