using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Frends.As2.CreateMdnResponse.Definitions;
using Frends.As2.CreateMdnResponse.Helpers;

namespace Frends.As2.CreateMdnResponse;

/// <summary>
/// Task class.
/// </summary>
public static class As2
{
    /// <summary>
    /// Task to create a properly formatted MDN (Message Disposition Notification) response for an AS2 message.
    /// This method allows full customization of the MDN including disposition status, custom headers, and signing/encryption options.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-As2-CreateMdnResponse)
    /// </summary>
    /// <param name="input">MDN data from ValidateAndParsePayload result.</param>
    /// <param name="options">Optional customization for disposition status and text.</param>
    /// <param name="cancellationToken">Cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, Dictionary&lt;string, string&gt; Headers, byte[] Content, string ContentType, object Error }</returns>
    public static Result CreateMdnResponse(
        Input input,
        Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            var headers = new Dictionary<string, string>(input.MdnHeaders);
            var content = input.MdnContentB;

            if (options != null)
            {
                var existingContent = System.Text.Encoding.UTF8.GetString(content);

                existingContent = Regex.Replace(
                    existingContent,
                    @"Disposition:\s*[^;]+;\s*\w+",
                    $"Disposition: automatic-action/MDN-sent-automatically; {options.DispositionStatus}");

                existingContent = Regex.Replace(
                    existingContent,
                    @"Content-Type: text/plain\r?\n\r?\n[^\r\n-]+",
                    $"Content-Type: text/plain\r\n\r\n{options.MdnText}");

                content = System.Text.Encoding.UTF8.GetBytes(existingContent);
            }

            var contentType = headers.ContainsKey("Content-Type")
                ? headers["Content-Type"]
                : "multipart/report; report-type=disposition-notification";

            return new Result
            {
                Success = true,
                Headers = headers,
                Content = content,
                ContentType = contentType,
            };
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }
}
