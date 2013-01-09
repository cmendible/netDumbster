// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible 

using System;
using System.Text;
using System.Net.Sockets;
using netDumbster.smtp.Logging;

namespace netDumbster.smtp
{
	/// <summary>
	/// Maintains the current state for a SMTP client connection.
	/// </summary>
	/// <remarks>
	/// This class is similar to a HTTP Session.  It is used to maintain all
	/// the state information about the current connection.
	/// </remarks>
	public class SmtpContext
	{
		#region Constants

		private const string EOL = "\r\n";

		#endregion

		#region Variables

		ILog _Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>The socket to the client.</summary>
		private Socket socket;

		/// <summary>Last successful command received.</summary>
		private int lastCommand;

		/// <summary>The client domain, as specified by the helo command.</summary>
		private string clientDomain;

		/// <summary>The incoming message.</summary>
		private SmtpMessage message;

		/// <summary>Encoding to use to send/receive data from the socket.</summary>
		private Encoding encoding;

		/// <summary>
		/// It is possible that more than one line will be in
		/// the queue at any one time, so we need to store any input
		/// that has been read from the socket but not requested by the
		/// ReadLine command yet.
		/// </summary>
		private StringBuilder inputBuffer;

		#endregion

		#region Constructors

		/// <summary>
		/// Initialize this context for a given socket connection.
		/// </summary>
		public SmtpContext(Socket socket)
		{
			this.lastCommand = -1;
			this.socket = socket;
			message = new SmtpMessage();

			// Set the encoding to ASCII.  
			encoding = Encoding.ASCII;

			// Initialize the input buffer
			inputBuffer = new StringBuilder();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Last successful command received.
		/// </summary>
		public int LastCommand
		{
			get
			{
				return lastCommand;
			}
			set
			{
				lastCommand = value;
			}
		}

		/// <summary>
		/// The client domain, as specified by the helo command.
		/// </summary>
		public string ClientDomain
		{
			get
			{
				return clientDomain;
			}
			set
			{
				clientDomain = value;
			}
		}

		/// <summary>
		/// The Socket that is connected to the client.
		/// </summary>
		public Socket Socket
		{
			get
			{
				return socket;
			}
		}

		/// <summary>
		/// The SMTPMessage that is currently being received.
		/// </summary>
		public SmtpMessage Message
		{
			get
			{
				return message;
			}
			set
			{
				message = value;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Writes the string to the socket as an entire line.  This
		/// method will append the end of line characters, so the data
		/// parameter should not contain them.
		/// </summary>
		/// <param name="data">The data to write the the client.</param>
		public void WriteLine(string data)
		{
			socket.Send(encoding.GetBytes(data + EOL));
		}

		/// <summary>
		/// Reads an entire line from the socket.  This method
		/// will block until an entire line has been read.
		/// </summary>
		public String ReadLine()
		{
			// If we already buffered another line, just return
			// from the buffer.			
			string output = ReadBuffer();
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
				count = socket.Receive(byteBuffer);

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
			message = new SmtpMessage();
			lastCommand = SmtpProcessor.COMMAND_HELO;
			_Log.Debug("Done resetting SmtpContext.");
		}

		/// <summary>
		/// Closes the socket connection to the client and performs any cleanup.
		/// </summary>
		public void Close()
		{
			_Log.Debug("Closing SmtpContext.");
			inputBuffer.Length = 0;
			socket.Close();
			_Log.Debug("SmtpContext Closed.");
		}

		#endregion

		#region Private Methods

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
				string buffer = inputBuffer.ToString();
				int eolIndex = buffer.IndexOf(EOL);
				if (eolIndex != -1)
				{
					string output = buffer.Substring(0, eolIndex);
					inputBuffer = new StringBuilder(buffer.Substring(eolIndex + 2));
					return output;
				}
			}
			return null;
		}

		#endregion
	}
}
