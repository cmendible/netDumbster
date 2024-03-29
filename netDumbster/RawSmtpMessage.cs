// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp;

public class RawSmtpMessage
{
    readonly List<EmailAddress> recipients;

    public RawSmtpMessage(IPAddress localIPAddress, int localPort, IPAddress remoteIPAddress, int remotePort)
    {
        Data = new StringBuilder();
        recipients = [];
        LocalIPAddress = localIPAddress;
        LocalPort = localPort;
        RemoteIPAddress = remoteIPAddress;
        RemotePort = remotePort;
    }

    public StringBuilder Data
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the local IP address.
    /// </summary>
    public IPAddress LocalIPAddress
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the local port.
    /// </summary>
    public int LocalPort
    {
        get;
        private set;
    }

    public IEnumerable<EmailAddress> Recipients
    {
        get
        {
            return recipients;
        }
    }

    /// <summary>
    /// Gets the remote IP address.
    /// </summary>
    public IPAddress RemoteIPAddress
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the remote port.
    /// </summary>
    public int RemotePort
    {
        get;
        private set;
    }

    public void AddRecipient(EmailAddress recipient)
    {
        recipients.Add(recipient);
    }
}
