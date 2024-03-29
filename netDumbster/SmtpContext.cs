// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

/// <summary>
/// Maintains the current state for a SMTP client connection.
/// </summary>
/// <remarks>
/// This class is similar to a HTTP Session.  It is used to maintain all
/// the state information about the current connection.
/// </remarks>
public class SmtpContext
{
    private const string EOL = "\r\n";

    /// <summary>Encoding to use to send/receive data from the socket.</summary>
    private readonly Encoding encoding;

    /// <summary>
    /// It is possible that more than one line will be in
    /// the queue at any one time, so we need to store any input
    /// that has been read from the socket but not requested by the
    /// ReadLine command yet.
    /// </summary>
    private StringBuilder inputBuffer;

    readonly IPEndPoint localEndPoint;

    readonly IPEndPoint remoteEndPoint;

    readonly ILog _Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Initialize this context for a given socket connection.
    /// </summary>
    public SmtpContext(Socket socket)
    {
        ClientDomain = string.Empty;
        LastCommand = -1;
        Socket = socket;

        // Set the encoding to ASCII.
        encoding = Encoding.ASCII;

        // Initialize the input buffer
        inputBuffer = new StringBuilder();

        remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        localEndPoint = (IPEndPoint)socket.LocalEndPoint;

        Message = new RawSmtpMessage(localEndPoint.Address, localEndPoint.Port, remoteEndPoint.Address, remoteEndPoint.Port);
    }

    /// <summary>
    /// The client domain, as specified by the helo command.
    /// </summary>
    public string ClientDomain
    {
        get; set;
    }

    /// <summary>
    /// Last successful command received.
    /// </summary>
    public int LastCommand
    {
        get; set;
    }

    /// <summary>
    /// The SMTPMessage that is currently being received.
    /// </summary>
    public RawSmtpMessage Message
    {
        get; private set;
    }

    /// <summary>
    /// The Socket that is connected to the client.
    /// </summary>
    public Socket Socket
    {
        get;
    }

    /// <summary>
    /// Closes the socket connection to the client and performs any cleanup.
    /// </summary>
    public void Close()
    {
        _Log.Debug("Closing SmtpContext.");
        inputBuffer.Length = 0;
        Socket.Close();
        _Log.Debug("SmtpContext Closed.");
    }

    /// <summary>
    /// Reads an entire line from the socket.  This method
    /// will block until an entire line has been read.
    /// </summary>
    public string ReadLine()
    {
        // If we already buffered another line, just return
        // from the buffer.
        var output = ReadBuffer();
        if (output != null)
        {
            return output;
        }

        // Otherwise, read more input.
        var byteBuffer = new byte[80];
        int count;

        // Read from the socket until an entire line has been read.
        do
        {
            // Read the input data.
            count = Socket.Receive(byteBuffer);

            if (count == 0)
            {
                return null;
            }

            inputBuffer.Append(encoding.GetString(byteBuffer, 0, count));
        }
        while ((output = ReadBuffer()) == null);

        // IO Log statement is in ReadBuffer...

        return output;
    }

    /// <summary>
    /// Resets this context for a new message
    /// </summary>
    public void Reset()
    {
        _Log.Debug("Resetting SmtpContext.");
        inputBuffer.Length = 0;
        Message = new RawSmtpMessage(localEndPoint.Address, localEndPoint.Port, remoteEndPoint.Address, remoteEndPoint.Port);
        LastCommand = SmtpProcessor.COMMAND_HELO;
        _Log.Debug("Done resetting SmtpContext.");
    }

    /// <summary>
    /// Writes the string to the socket as an entire line.  This
    /// method will append the end of line characters, so the data
    /// parameter should not contain them.
    /// </summary>
    /// <param name="data">The data to write the the client.</param>
    public void WriteLine(string data)
    {
        Socket.Send(encoding.GetBytes(data + EOL));
    }

    /// <summary>
    /// Helper method that returns the first full line in
    /// the input buffer, or null if there is no line in the buffer.
    /// If a line is found, it will also be removed from the buffer.
    /// </summary>
    private string ReadBuffer()
    {
        // If the buffer has data, check for a full line.
        if (inputBuffer.Length > 0)
        {
            var buffer = inputBuffer.ToString();
            var eolIndex = buffer.IndexOf(EOL);
            if (eolIndex != -1)
            {
                var output = buffer.Substring(0, eolIndex);
                inputBuffer = new StringBuilder(buffer.Substring(eolIndex + 2));
                return output;
            }
        }

        return null;
    }
}
