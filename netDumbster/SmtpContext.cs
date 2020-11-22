// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    using netDumbster.smtp.Logging;

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

        /// <summary>The client domain, as specified by the helo command.</summary>
        private string clientDomain;

        /// <summary>Encoding to use to send/receive data from the socket.</summary>
        private Encoding encoding;

        /// <summary>
        /// It is possible that more than one line will be in
        /// the queue at any one time, so we need to store any input
        /// that has been read from the socket but not requested by the
        /// ReadLine command yet.
        /// </summary>
        private StringBuilder inputBuffer;

        /// <summary>Last successful command received.</summary>
        private int lastCommand;
        IPEndPoint localEndPoint;

        /// <summary>The incoming message.</summary>
        private RawSmtpMessage rawSmtpMessage;
        IPEndPoint remoteEndPoint;

        /// <summary>The socket to the client.</summary>
        private Socket socket;
        ILog _Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Initialize this context for a given socket connection.
        /// </summary>
        public SmtpContext(Socket socket)
        {
            this.lastCommand = -1;
            this.socket = socket;

            // Set the encoding to ASCII.
            this.encoding = Encoding.ASCII;

            // Initialize the input buffer
            this.inputBuffer = new StringBuilder();

            this.remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
            this.localEndPoint = socket.LocalEndPoint as IPEndPoint;

            this.rawSmtpMessage = new RawSmtpMessage(this.localEndPoint.Address, this.localEndPoint.Port, this.remoteEndPoint.Address, this.remoteEndPoint.Port);
        }

        /// <summary>
        /// The client domain, as specified by the helo command.
        /// </summary>
        public string ClientDomain
        {
            get
            {
                return this.clientDomain;
            }

            set
            {
                this.clientDomain = value;
            }
        }

        /// <summary>
        /// Last successful command received.
        /// </summary>
        public int LastCommand
        {
            get
            {
                return this.lastCommand;
            }

            set
            {
                this.lastCommand = value;
            }
        }

        /// <summary>
        /// The SMTPMessage that is currently being received.
        /// </summary>
        public RawSmtpMessage Message
        {
            get
            {
                return this.rawSmtpMessage;
            }

            set
            {
                this.rawSmtpMessage = value;
            }
        }

        /// <summary>
        /// The Socket that is connected to the client.
        /// </summary>
        public Socket Socket
        {
            get
            {
                return this.socket;
            }
        }

        /// <summary>
        /// Closes the socket connection to the client and performs any cleanup.
        /// </summary>
        public void Close()
        {
            this._Log.Debug("Closing SmtpContext.");
            this.inputBuffer.Length = 0;
            this.socket.Close();
            this._Log.Debug("SmtpContext Closed.");
        }

        /// <summary>
        /// Reads an entire line from the socket.  This method
        /// will block until an entire line has been read.
        /// </summary>
        public string ReadLine()
        {
            // If we already buffered another line, just return
            // from the buffer.
            string output = this.ReadBuffer();
            if (output != null)
            {
                return output;
            }

            // Otherwise, read more input.
            byte[] byteBuffer = new byte[80];
            int count;

            // Read from the socket until an entire line has been read.
            do
            {
                // Read the input data.
                count = this.socket.Receive(byteBuffer);

                if (count == 0)
                {
                    return null;
                }

                this.inputBuffer.Append(this.encoding.GetString(byteBuffer, 0, count));
            }
            while ((output = this.ReadBuffer()) == null);

            // IO Log statement is in ReadBuffer...

            return output;
        }

        /// <summary>
        /// Resets this context for a new message
        /// </summary>
        public void Reset()
        {
            this._Log.Debug("Resetting SmtpContext.");
            this.inputBuffer.Length = 0;
            this.rawSmtpMessage = new RawSmtpMessage(this.localEndPoint.Address, this.localEndPoint.Port, this.remoteEndPoint.Address, this.remoteEndPoint.Port);
            this.lastCommand = SmtpProcessor.COMMAND_HELO;
            this._Log.Debug("Done resetting SmtpContext.");
        }

        /// <summary>
        /// Writes the string to the socket as an entire line.  This
        /// method will append the end of line characters, so the data
        /// parameter should not contain them.
        /// </summary>
        /// <param name="data">The data to write the the client.</param>
        public void WriteLine(string data)
        {
            this.socket.Send(this.encoding.GetBytes(data + EOL));
        }

        /// <summary>
        /// Helper method that returns the first full line in
        /// the input buffer, or null if there is no line in the buffer.
        /// If a line is found, it will also be removed from the buffer.
        /// </summary>
        private string ReadBuffer()
        {
            // If the buffer has data, check for a full line.
            if (this.inputBuffer.Length > 0)
            {
                string buffer = this.inputBuffer.ToString();
                int eolIndex = buffer.IndexOf(EOL);
                if (eolIndex != -1)
                {
                    string output = buffer.Substring(0, eolIndex);
                    this.inputBuffer = new StringBuilder(buffer.Substring(eolIndex + 2));
                    return output;
                }
            }

            return null;
        }
    }
}