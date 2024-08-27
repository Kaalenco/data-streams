using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Polly;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace N2.DataStreams.A2Protocol;

public interface IHttpContext
{
    HttpRequestMessage Request { get; }
    HttpResponseMessage Response { get; }
    HttpMethod RequestMethod { get; }
}
public class A2Listener
{
    public void ProcessRequest(IHttpContext context, string dropLocation)
    {
        var sTo = context.Request.Headers.FirstOrDefault(m => m.Key == "AS2-To");
        var sFrom = context.Request.Headers.FirstOrDefault(m => m.Key == "AS2-From");
        var sMessageID = context.Request.Headers.FirstOrDefault(m => m.Key == "Message-ID");

        if (context.Request.Method.Method == "POST" || context.Request.Method.Method == "PUT" ||
           (context.Request.Method.Method == "GET" && context.Request.QueryString.Count > 0))
        {

            if (sFrom.Value.Count()==0 || string.IsNullOrEmpty(sFrom.Value.First())
                || sTo.Value.Count() == 0 || string.IsNullOrEmpty(sTo.Value.First()))
            {
                //Invalid AS2 Request.
                //Section 6.2 The AS2-To and AS2-From header fields MUST be present
                //    in all AS2 messages
                if (!(context.Request.HttpMethod == "GET" && context.Request.QueryString[0].Length == 0))
                {
                    AS2Receive.BadRequest(context.Response, "Invalid or unauthorized AS2 request received.");
                }
            }
            else
            {
                AS2Receive.Process(context.Request, dropLocation);
            }
        }
        else
        {
            AS2Receive.GetMessage(context.Response);
        }
    }
}

public static class AS2Receive
{

    public static void GetMessage(HttpResponseMessage response)
    {
        response.StatusCode = HttpStatusCode.OK;

        var sb = new StringBuilder(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 3.2 Final//EN"">"
        + @"<HTML><HEAD><TITLE>Generic AS2 Receiver</TITLE></HEAD>"
        + @"<BODY><H1>200 Okay</H1><HR>This is to inform you that the AS2 interface is working and is "
        + @"accessable from your location.  This is the standard response to all who would send a GET "
        + @"request to this page instead of the POST context.Request defined by the AS2 Draft Specifications.<HR></BODY></HTML>");

        response.Content = new StringContent(sb.ToString());
    }

    public static void BadRequest(HttpResponseMessage response, string message)
    {
        response.StatusCode = HttpStatusCode.BadRequest;

        response.Write(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 3.2 Final//EN"">"
        + @"<HTML><HEAD><TITLE>400 Bad context.Request</TITLE></HEAD>"
        + @"<BODY><H1>400 Bad context.Request</H1><HR>There was a error processing this context.Request.  The reason given by the server was:"
        + @"<P><font size=-1>" + message + @"</Font><HR></BODY></HTML>");
    }

    public static void DmnMessage(HttpResponseMessage response, HttpRequestMessage request)
    {
        response.Clear();
        response.Headers.Add("Date", DateTime.UtcNow.ToString("ddd, dd MMM yyy HH: mm:ss") + " GMT");

        var incoming_subject = request.Headers["Subject"];
        var response_subject = "Re:";
        if (incoming_subject != null) response_subject = response_subject + incoming_subject;
        response.Headers.Add("Subject", response_subject);

        response.Headers.Add("Mime - Version", "1.0");
        response.Headers.Add("AS2 - Version", "1.2");
        response.Headers.Add("From", "XXXXXXXXXX");
        response.Headers.Add("AS2 - To", request.Headers["AS2 - From"]);
        response.Headers.Add("AS2 - From", request.Headers["AS2 - To"]);
        response.Headers.Add("Connection", "Close");
        response.ContentType = "multipart / report; report - type = disposition - notification;";
        response.StatusCode = HttpStatusCode.Accepted;

        var incoming_date = request.Headers["Date"];
        var dateTimeSent = "Unknown date";
        if (incoming_date != null) dateTimeSent = incoming_date;

        var responseContent1 = "The Message from ‘" +request.Headers["AS2 - From"] + "‘ to ‘" +
        request.Headers["AS2 - To"] + "‘ " +Environment.NewLine +
"with MessageID ‘" +request.Headers["Message - Id"] + "‘" +Environment.NewLine + "sent " +dateTimeSent +
" has been accepted for processing. " +Environment.NewLine +
"This does not guarantee that the message has been read or understood." +Environment.NewLine;

var responseContent2 = "Reporting - UA: AS2 Adapter" +Environment.NewLine +
"Final - Recipient: rfc822;" +request.Headers["AS2 - From"] + Environment.NewLine +
"Original - Message - ID: " +request.Headers["Message - ID"] + Environment.NewLine +
"Disposition: automatic - action / MDN - Sent - automatically; processed";

        var finalBodyContent = Encoding.ASCII.GetBytes(responseContent1);
        var finalBodyContent2 = Encoding.ASCII.GetBytes(responseContent2);

        //Wrap the file data with a mime header
        finalBodyContent2 = AS2Utilities.CreateMessage("message / disposition - notification", "7bit", "", finalBodyContent2);

        var PublicAndPrivateKeyPath = "some path";
        var SigningPassword = "take it from app config";
        string contentType;
        finalBodyContent2 = AS2Utilities.Sign(finalBodyContent2, PublicAndPrivateKeyPath, SigningPassword, out contentType);
        response.Headers.Add("EDIINT - Features", "AS2 - Reliability");

        byte[] signedContentTypeHeader = System.Text.Encoding.ASCII.GetBytes("Content - Type: " + "text / plain" +Environment.NewLine);
        byte[] contentWithContentTypeHeaderAdded = AS2Utilities.ConcatBytes(signedContentTypeHeader, finalBodyContent2);

        finalBodyContent2 = AS2Encryption.Encrypt(contentWithContentTypeHeaderAdded, clientCertificatePath,
        EncryptionAlgorithm.DES3);

        byte[] finalResponse = finalBodyContent.Concat(finalBodyContent2).ToArray();

        response.BinaryWrite(finalResponse);
    }

    public static void Process(HttpRequestMessage request, string dropLocation)
    {
        string filename = ParseFilename(request.Headers["Subject"]);

        byte[] data = request.BinaryRead(request.TotalBytes);
        bool isEncrypted = request.ContentType.Contains("application/pkcs7-mime");
        bool isSigned = request.ContentType.Contains("application/pkcs7-signature");

        string message = string.Empty;

        if (isSigned)
        {
            string messageWithMIMEHeaders = System.Text.ASCIIEncoding.ASCII.GetString(data);
            string contentType = request.Headers["Content-Type"];

            message = AS2Utilities.ExtractPayload(messageWithMIMEHeaders, contentType);
        }
        else if (isEncrypted) // encrypted and signed inside
        {
            byte[] decryptedData = AS2Encryption.Decrypt(data);

            string messageWithContentTypeLineAndMIMEHeaders = System.Text.ASCIIEncoding.ASCII.GetString(decryptedData);

            // when encrypted, the Content-Type line is actually stored in the start of the message
            int firstBlankLineInMessage = messageWithContentTypeLineAndMIMEHeaders.IndexOf(Environment.NewLine + Environment.NewLine);
            string contentType = messageWithContentTypeLineAndMIMEHeaders.Substring(0, firstBlankLineInMessage);

            message = AS2Utilities.ExtractPayload(messageWithContentTypeLineAndMIMEHeaders, contentType);
        }
        else // not signed and not encrypted
        {
            message = System.Text.ASCIIEncoding.ASCII.GetString(data);
        }

        System.IO.File.WriteAllText(dropLocation + filename, message);
    }
}
