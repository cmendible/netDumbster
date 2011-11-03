// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using log4net;

namespace netDumbster.smtp
{
    /// <summary>
    /// Simple Smtp Server
    /// </summary>
	public class SimpleSmtpServer
	{
		#region Variables

		/// <summary>
		/// Logger
		/// </summary>
		ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// TCP Listener
		/// </summary>
		private TcpListener _tcpListener;

		/// <summary>
		/// Stores all of the email received since this instance started up.
		/// </summary>
		private List<SmtpMessage> _receivedMail = new List<SmtpMessage>();

		/// <summary>
		/// Smtp Processor
		/// </summary>
		private SmtpProcessor _processor;

		/// <summary>
		/// Thread signal.
		/// </summary>
		internal AutoResetEvent _clientConnected = null;
		/// <summary>
		/// Thread signal.
		/// </summary>
		internal AutoResetEvent _serverReady = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		/// <value>The port.</value>
		public int Port { get; private set; }

		/// <summary>
		/// List of email received by this instance since start up.
		/// </summary>
		/// <value><see cref="T:System.Array">Array</see> holding received <see cref="T:netDumbster.smtp.SmtpMessage">SmtpMessage</see></value>
		public virtual SmtpMessage[] ReceivedEmail
		{
			get
			{
				lock(this)
					return this._receivedMail.ToArray();
			}
		}

		/// <summary>
		/// Number of messages received by this instance since start up.
		/// </summary>
		/// <value>Number of messages</value>
		public virtual int ReceivedEmailCount
		{
			get
			{
				lock (this)
					return this._receivedMail.Count;
			}
		}

		/// <summary>
		/// Clears the received email.
		/// </summary>
		public void ClearReceivedEmail()
		{
			lock (this)
				this._receivedMail.Clear();
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleSmtpServer"/> class.
		/// </summary>
		/// <param name="port">The port.</param>
		private SimpleSmtpServer(int port)
		{
			Port = port;
			_clientConnected = new AutoResetEvent(false);
			_serverReady = new AutoResetEvent(false);
			_processor = new SmtpProcessor(string.Empty, _receivedMail);
		}

		#endregion

		#region Private and Internal Methods

		/// <summary>
		/// Starts the Server
		/// </summary>
		internal void _Start()
		{
			_log.Info("Starting Smtp server");

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Port);
			_tcpListener = new TcpListener(endPoint);
			_tcpListener.Start();

			_log.DebugFormat("Started Tcp Listener at port {0}", Port);

			_clientConnected.Set();

			try
			{
				_clientConnected.Reset();
				_log.Debug("Calling BeginAcceptSocket.");
				_tcpListener.BeginAcceptSocket(new AsyncCallback(_SocketHandler), _tcpListener);
				_log.Debug("BeginAcceptSocket called.");
				_serverReady.Set();
				_clientConnected.WaitOne();
			}
			catch (Exception ex)
			{
				_log.Warn("Unexpected Exception starting the SmtpServer.", ex);
			}
			finally
			{
				_clientConnected.Set();
			}
		}

		/// <summary>
		/// Async Socket handler.
		/// </summary>
		/// <param name="result">The result.</param>
		private void _SocketHandler(IAsyncResult result)
		{
			_log.Debug("Entering Socket Handler.");

			try
			{
				TcpListener listener = (TcpListener)result.AsyncState;

				_log.Debug("Calling EndAcceptSocket.");
				var socket = listener.EndAcceptSocket(result);
				_log.Debug("Socket accepted and ready to be processed.");
				_processor.ProcessConnection(socket);

				// If socket is closed by any reason we should start listening again recursively.
				// This is a failsafe for smtp authentications tests.
				_tcpListener.BeginAcceptSocket(new AsyncCallback(_SocketHandler), _tcpListener);
			}
			catch (ObjectDisposedException ex)
			{
				_log.Warn("Object Disposed Exception. THIS IS EXPECTED ONLY IF SERVER WAS STOPPED.", ex);
			}
			catch (SocketException ex)
			{
				_log.Warn("Socket Exception", ex);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Starts server at the specified port.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <returns></returns>
		public static SimpleSmtpServer Start(int port)
		{
			var server = new SimpleSmtpServer(port);
			new Thread(new ThreadStart(server._Start)).Start();
			server._serverReady.WaitOne();
			return server;
		}

		/// <summary>
		/// Stop the server.  This notifies the listener to stop accepting new connections
		/// and that the loop should exit.
		/// </summary>
		public void Stop()
		{
			_log.Debug("Trying to stop SmtpServer.");

			try
			{
				lock(this)
				{
					// Kick the server accept loop
					if (_tcpListener != null)
					{
						_processor.Stop();
						_log.Debug("Stopping tcp listener.");
						_tcpListener.Stop();
						_log.Debug("Tcp listener stopped.");
					}
					_tcpListener = null;
				}
			}
			catch (Exception ex)
			{
				_log.Warn("Unexpected Exception stopping SmtpServer", ex);
			}
			finally
			{
				_log.Debug("SmtpServer Stopped.");
				_clientConnected.Set();
                _serverReady.Close();
			}
		}

		#endregion
	}

}

