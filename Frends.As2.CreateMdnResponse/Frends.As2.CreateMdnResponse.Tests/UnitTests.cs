using System.Threading;
using Frends.As2.CreateMdnResponse.Definitions;
using NUnit.Framework;

namespace Frends.As2.CreateMdnResponse.Tests;

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

[TestFixture]
public class CreateMdnResponseTests
{
    private Input _defaultInput;

    [SetUp]
    public void SetUp()
    {
        var sampleMdnContent = @"------=_NextPart_000_1234
Content-Type: text/plain

The AS2 message was processed successfully.

------=_NextPart_000_1234
Content-Type: message/disposition-notification

Reporting-UA: Test AS2 System
Original-Recipient: rfc822; PartnerCompany
Final-Recipient: rfc822; YourCompany
Original-Message-ID: <msg123@partner.com>
Disposition: automatic-action/MDN-sent-automatically; processed
Received-Content-MIC: 7v7F2y8qb2kEhNy+wjB7QQvlzaA=, sha1

------=_NextPart_000_1234--";

        _defaultInput = new Input
        {
            MdnContentB = Encoding.UTF8.GetBytes(sampleMdnContent),
            MdnHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "multipart/report; report-type=disposition-notification; boundary=\"----=_NextPart_000_1234\"",
                ["Message-ID"] = "<mdn456@system.com>",
                ["AS2-From"] = "YourCompany",
                ["AS2-To"] = "PartnerCompany",
            },
        };
    }

    [Test]
    public void Should_Return_Success_With_Valid_Input()
    {
        var options = new Options
        {
            DispositionStatus = "processed",
            MdnText = "The AS2 message was processed successfully.",
        };

        var result = As2.CreateMdnResponse(_defaultInput, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Method should succeed with valid input");
        Assert.That(result.Headers, Is.Not.Null, "Headers should not be null");
        Assert.That(result.Content, Is.Not.Null, "Content should not be null");
        Assert.That(result.ContentType, Is.EqualTo(_defaultInput.MdnHeaders["Content-Type"]), "Should use Content-Type from input headers");
    }

    [Test]
    public void Should_Modify_Disposition_Status_And_Text()
    {
        var options = new Options
        {
            DispositionStatus = "processed/warning",
            MdnText = "Message processed with warnings",
        };

        var result = As2.CreateMdnResponse(_defaultInput, options, CancellationToken.None);

        var contentString = Encoding.UTF8.GetString(result.Content);
        Assert.That(contentString, Contains.Substring("Disposition: automatic-action/MDN-sent-automatically; processed/warning"), "Should update disposition status to 'processed/warning'");
        Assert.That(contentString, Contains.Substring("Message processed with warnings"), "Should update MDN text with custom message");
    }

    [Test]
    public void Should_Handle_Failed_Status()
    {

        var options = new Options
        {
            DispositionStatus = "failed/failure",
            MdnText = "Message processing failed",
        };

        var result = As2.CreateMdnResponse(_defaultInput, options, CancellationToken.None);

        var contentString = Encoding.UTF8.GetString(result.Content);
        Assert.That(result.Success, Is.True, "Should succeed even with failed disposition");
        Assert.That(contentString, Contains.Substring("failed/failure"), "Should set failed disposition");
        Assert.That(contentString, Contains.Substring("Message processing failed"), "Should include failure message");
    }

    [Test]
    public void Should_Handle_Error_When_ThrowErrorOnFailure_Is_False()
    {
        var invalidInput = new Input
        {
            MdnContentB = null,
            MdnHeaders = _defaultInput.MdnHeaders,
        };

        var options = new Options { ThrowErrorOnFailure = false };

        var result = As2.CreateMdnResponse(invalidInput, options, CancellationToken.None);

        Assert.That(result.Success, Is.False, "Should return failure on error");
        Assert.That(result.Error, Is.Not.Null, "Should include error information");
    }
}
