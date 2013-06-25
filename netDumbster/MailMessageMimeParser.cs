#region Header

// Copyright (c) 2009 snarum, (http://mimeparser.codeplex.com/)
// All rights reserved.
// Modified by Carlos Mendible

#endregion Header

namespace netDumbster.smtp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class MailMessageMimeParser
    {
        #region Methods

        public static MailMessage ParseMessage(StringReader mimeMail)
        {
            MailMessage returnValue = ParseMessageRec(mimeMail);

            FixStandardFields(returnValue);
            return returnValue;
        }

        private static string DecodeBase64(string line, string enc)
        {
            string returnValue = string.Empty;
            switch (enc.ToLower())
            {
                case "utf-7":
                    returnValue = System.Text.Encoding.UTF7.GetString(Convert.FromBase64String(line));
                    break;
                case "utf-8":
                    returnValue = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(line));
                    break;
                default:
                    break;
            }

            return returnValue;
        }

        private static byte[] DecodeBase64Binary(string line)
        {
            return Convert.FromBase64String(line);
        }

        private static void DecodeHeaders(NameValueCollection headers)
        {
            ArrayList tmpKeys = new ArrayList(headers.Keys);

            foreach (string key in headers.AllKeys)
            {
                //strip qp encoding information from the header if present
                headers[key] = Regex.Replace(headers[key].ToString(), @"=\?.*?\?Q\?(.*?)\?=", new MatchEvaluator(MyMatchEvaluator), RegexOptions.IgnoreCase | RegexOptions.Multiline);
                headers[key] = Regex.Replace(headers[key].ToString(), @"=\?.*?\?B\?(.*?)\?=", new MatchEvaluator(MyMatchEvaluatorBase64), RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
        }

        private static string DecodeQP(string trall)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < trall.Length; i++)
            {
                if (trall[i] == '=')
                {
                    byte tmpbyte = Convert.ToByte(trall.Substring(i + 1, 2), 16);
                    i += 2;
                    b.Append((char)tmpbyte);
                }
                else if (trall[i] == '_')
                {
                    b.Append(' ');
                }
                else
                {
                    b.Append(trall[i]);
                }
            }
            return b.ToString();
        }

        private static void FillAddressesCollection(ICollection<MailAddress> addresses, string addressHeader)
        {
            if(string.IsNullOrEmpty(addressHeader))
            {
                return;
            }

            string[] emails = addressHeader.Split(',');

            for (int i = 0; i < emails.Length; i++)
            {
                MailAddress address;

                try
                {
                    address = new MailAddress(emails[i]);
                }
                catch
                {
                    if (i < emails.Length - 1)
                    {
                        address = new MailAddress(emails[i] + "," + emails[i + 1]);
                        i++;
                    }
                    else
                    {
                        address = new MailAddress("missing@missing.biz");
                    }
                }

                addresses.Add(address);
            }
        }

        private static System.Net.Mime.ContentType FindContentType(NameValueCollection headers)
        {
            System.Net.Mime.ContentType returnValue = new ContentType();
            if (headers["content-type"] == null)
            {
                return returnValue;
            }
            returnValue = new System.Net.Mime.ContentType(Regex.Match(headers["content-type"], @"^([^;]*)", RegexOptions.IgnoreCase).Groups[1].Value);

            if(Regex.IsMatch(headers["content-type"],  @"name=""?(.*?)""?($|;)", RegexOptions.IgnoreCase))
            {
                returnValue.Name = Regex.Match(headers["content-type"],  @"name=""?(.*?)""?($|;)", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            if (Regex.IsMatch(headers["content-type"], @"boundary=""(.*?)""", RegexOptions.IgnoreCase))
            {
                returnValue.Boundary = Regex.Match(headers["content-type"], @"boundary=""(.*?)""", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            else if (Regex.IsMatch(headers["content-type"], @"boundary=(.*?)(;|$)", RegexOptions.IgnoreCase))
            {
                returnValue.Boundary = Regex.Match(headers["content-type"], @"boundary=(.*?)(;|$)", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            if (Regex.IsMatch(headers["content-type"], @"charset=""(.*?)""", RegexOptions.IgnoreCase))
            {
                returnValue.CharSet = Regex.Match(headers["content-type"], @"charset=""(.*?)""", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            if (Regex.IsMatch(headers["content-type"], @"charset=(.*?)(;|$)", RegexOptions.IgnoreCase))
            {
                returnValue.CharSet = Regex.Match(headers["content-type"], @"charset=(.*?)(;|$)", RegexOptions.IgnoreCase).Groups[1].Value;
            }

            return returnValue;
        }

        private static void FixStandardFields(MailMessage message)
        {
            if(message.Headers["content-type"]!= null)
            {

                //extract the value of the content-type
                string type = Regex.Match(message.Headers["content-type"], @"^([^;]*)", RegexOptions.IgnoreCase).Groups[1].Value;
                if (type.ToLower() == "multipart/related" || type.ToLower() == "multipart/alternative")
                {
                    List<string> toBeRemoved = new List<string>();
                    List<AlternateView> viewsToBeRemoved = new List<AlternateView>();
                    List<AlternateView> viewsToBeAdded = new List<AlternateView>();

                    foreach (AlternateView view in message.AlternateViews)
                    {
                        if (view.ContentType.MediaType == "text/html")
                        {
                            foreach (Attachment att in message.Attachments)
                            {
                                if (!string.IsNullOrEmpty(att.ContentId))
                                {
                                    LinkedResource res = new LinkedResource(att.ContentStream, att.ContentType);
                                    res.ContentId = att.ContentId;
                                    if(att.ContentId.StartsWith("tmpContentId123_"))
                                    {
                                        string tmpLocation = Regex.Match(att.ContentId, "tmpContentId123_(.*)").Groups[1].Value;
                                        string tmpid = Guid.NewGuid().ToString();
                                        res.ContentId = tmpid;
                                        string oldHtml = GetStringFromStream(view.ContentStream, view.ContentType);
                                        ContentType ct = new ContentType("text/html; charset=utf-7");
                                        AlternateView tmpView = AlternateView.CreateAlternateViewFromString(Regex.Replace(oldHtml, "src=\"" + tmpLocation + "\"", "src=\"cid:" + tmpid + "\"", RegexOptions.IgnoreCase), ct);
                                        tmpView.LinkedResources.Add(res);
                                        viewsToBeAdded.Add(tmpView);
                                        viewsToBeRemoved.Add(view);
                                    }
                                    else
                                    {
                                        view.LinkedResources.Add(res);
                                    }

                                    toBeRemoved.Add(att.ContentId);
                                }
                            }
                        }
                    }
                    foreach (AlternateView view in viewsToBeRemoved)
                    {
                        message.AlternateViews.Remove(view);
                    }
                    foreach (AlternateView view in viewsToBeAdded)
                    {
                        message.AlternateViews.Add(view);
                    }
                    foreach (string s in toBeRemoved)
                    {
                        foreach (Attachment att in message.Attachments)
                        {
                            if (att.ContentId == s)
                            {
                                message.Attachments.Remove(att);
                                break;
                            }
                        }
                    }
                }

            }
            if (string.IsNullOrEmpty(message.Subject))
            {
                message.Subject = GetHeaderValue(message.Headers, "subject");
            }
            if (message.From == null)
            {
                if (!string.IsNullOrEmpty(message.Headers["from"]))
                {
                    try
                    {

                        message.From = new MailAddress(message.Headers["from"].ToString());
                    }
                    catch
                    {
                        message.From = new MailAddress("missing@missing.biz");
                    }
                }
                else
                {
                    message.From = new MailAddress("missing@missing.biz");
                }
            }

            FillAddressesCollection(message.CC, message.Headers["cc"]);
            FillAddressesCollection(message.To, message.Headers["to"]);
            FillAddressesCollection(message.Bcc, message.Headers["bcc"]);

            foreach (AlternateView view in message.AlternateViews)
            {
                view.ContentStream.Seek(0, SeekOrigin.Begin);
            }

            if (message.AlternateViews.Count == 1)
            {
                StreamReader re = new StreamReader(message.AlternateViews[0].ContentStream);
                message.Body = re.ReadToEnd();
                message.IsBodyHtml = message.AlternateViews[0].ContentType.MediaType == "text/html";
                message.AlternateViews.Clear();
            }
        }

        private static string GetHeaderValue(NameValueCollection collection, string key)
        {
            foreach (string k in collection.Keys)
            {
                if(k.Equals(key,StringComparison.InvariantCultureIgnoreCase))
                {
                    return collection[k];
                }
            }
            return string.Empty;
        }

        private static string GetStringFromStream(Stream stream, ContentType contentType)
        {
            stream.Seek(0, new SeekOrigin());
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            string returnValue = string.Empty;
            switch (contentType.CharSet.ToLower())
            {
            case "utf-8":
                returnValue = System.Text.UTF8Encoding.UTF8.GetString(buffer);
                break;
            case "utf-7":
                returnValue = System.Text.UTF7Encoding.UTF7.GetString(buffer);
                break;
            }
            return returnValue;
        }

        private static Attachment ImportAttachment(StringReader r, string encoding, ContentType contentType, NameValueCollection headers)
        {
            string line = r.ReadToEnd();
            Attachment returnValue = null;
            switch (encoding)
            {
                //case "quoted-printable":
                //    returnValue = new Attachment(new MemoryStream(DecodeBase64Binary(line)), contentType);
                //    returnValue.TransferEncoding = TransferEncoding.QuotedPrintable;
                //    break;
                //case "base64":
                //    returnValue = new Attachment(new MemoryStream(DecodeBase64Binary(line)),contentType);
                //    returnValue.TransferEncoding = TransferEncoding.Base64;
                //    break;
                default :
                    returnValue = new Attachment(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(line)), contentType);
                    returnValue.TransferEncoding = TransferEncoding.SevenBit;
                    break;
            }
            if (headers["content-id"] != null)
            {
                returnValue.ContentId = headers["content-id"].ToString().Trim('<', '>');
            }
            else if (headers["content-location"] != null)
            {
                returnValue.ContentId = "tmpContentId123_" + headers["content-location"].ToString();
            }

            return returnValue;
        }

        private static MailMessage ImportMultiPartAlternative(string multipartBoundary, StringReader message)
        {
            MailMessage returnValue = new MailMessage();
            string line = string.Empty;
            List<string> messageParts = new List<string>();

            //ffw until first boundary
            while (!message.ReadLine().TrimEnd().Equals("--" + multipartBoundary)) ;
            StringBuilder part = new StringBuilder();
            while ((line = message.ReadLine()) != null)
            {
                if (line.TrimEnd().Equals("--" + multipartBoundary) || line.TrimEnd().Equals("--" + multipartBoundary + "--"))
                {
                    MailMessage tmpMessage = ParseMessageRec(new StringReader(part.ToString()));
                    if (tmpMessage != null)
                    {
                        foreach (AlternateView view in tmpMessage.AlternateViews)
                        {
                            returnValue.AlternateViews.Add(view);
                        }
                        foreach (Attachment att in tmpMessage.Attachments)
                        {
                            returnValue.Attachments.Add(att);
                        }
                        if (line.Equals("--" + multipartBoundary))
                        {
                            part = new StringBuilder();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    part.AppendLine(line);
                }
            }
            return returnValue;
        }

        private static AlternateView ImportText(StringReader r, string encoding, System.Net.Mime.ContentType contentType)
        {
            string line = string.Empty;
            StringBuilder b = new StringBuilder();
            while ((line = r.ReadLine())!= null)
            {
                switch (encoding)
                {
                    case "quoted-printable":
                        if (line.EndsWith("="))
                        {
                            b.Append(DecodeQP(line.TrimEnd('=')));
                        }
                        else
                            // b.Append(DecodeQP(line) + "\n");
                        {
                            b.Append(DecodeQP(line));
                        }
                        break;
                    case "base64":
                        // b.Append(DecodeBase64(line, contentType.CharSet));
                        b.Append(line);
                        break;
                    default:
                        b.Append(line);
                        break;
                }
            }

            AlternateView returnValue = AlternateView.CreateAlternateViewFromString(b.ToString(), null, contentType.MediaType);
            returnValue.TransferEncoding = TransferEncoding.QuotedPrintable;
            return returnValue;
        }

        private static string MyMatchEvaluator(Match m)
        {
            return DecodeQP(m.Groups[1].Value);
        }

        private static string MyMatchEvaluatorBase64(Match m)
        {
            System.Text.Encoding enc = System.Text.Encoding.UTF7;
            return enc.GetString(Convert.FromBase64String(m.Groups[1].Value));
        }

        private static MailMessage ParseMessageRec(StringReader mimeMail)
        {
            MailMessage returnValue = new MailMessage();
            string line = string.Empty;
            string lastHeader = string.Empty;
            while ((!string.IsNullOrEmpty(line = mimeMail.ReadLine()) && (line.Trim().Length != 0)))
            {

                //If the line starts with a whitespace it is a continuation of the previous line
                if (Regex.IsMatch(line, @"^\s"))
                {
                    returnValue.Headers[lastHeader] = GetHeaderValue(returnValue.Headers,lastHeader) + " " + line.TrimStart('\t',' ');
                }
                else
                {
                    string headerkey = line.Substring(0, line.IndexOf(':')).ToLower();
                    string value = line.Substring(line.IndexOf(':') + 1).TrimStart(' ');
                    if (value.Length > 0)
                    {
                        returnValue.Headers[headerkey] = line.Substring(line.IndexOf(':') + 1).TrimStart(' ');
                    }
                    lastHeader = headerkey;
                }
            }
            if (returnValue.Headers.Count == 0)
            {
                return null;
            }
            DecodeHeaders(returnValue.Headers);
            string contentTransferEncoding = string.Empty;
            if (!string.IsNullOrEmpty(returnValue.Headers["content-transfer-encoding"]))
            {
                contentTransferEncoding = returnValue.Headers["content-transfer-encoding"];
            }
            System.Net.Mime.ContentType tmpContentType = FindContentType(returnValue.Headers);
            string contentId = string.Empty;

            switch (tmpContentType.MediaType)
            {
                case "multipart/alternative":
                case "multipart/related":
                case "multipart/mixed":
                    MailMessage tmpMessage = ImportMultiPartAlternative(tmpContentType.Boundary, mimeMail);
                    foreach (AlternateView view in tmpMessage.AlternateViews)
                    {
                        returnValue.AlternateViews.Add(view);
                    }
                    foreach (Attachment att in tmpMessage.Attachments)
                    {
                        returnValue.Attachments.Add(att);
                    }
                    break;
                case "text/html":
                case "text/plain":
                    returnValue.AlternateViews.Add(ImportText(mimeMail, contentTransferEncoding, tmpContentType));
                    break;
                default:
                    returnValue.Attachments.Add(ImportAttachment(mimeMail, contentTransferEncoding, tmpContentType, returnValue.Headers));
                    break;
            }
            return returnValue;
        }

        #endregion Methods
    }
}