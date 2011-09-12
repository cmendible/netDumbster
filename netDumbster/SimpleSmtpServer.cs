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
		ILog _Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// TCP Listener
		/// </summary>
		private TcpListener _Listener;

		/// <summary>
		/// Stores all of the email received since this instance started up.
		/// </summary>
		private List<SmtpMessage> receivedMail = new List<SmtpMessage>();

		/// <summary>
		/// Smtp Processor
		/// </summary>
		private SmtpProcessor _Processor;

		/// <summary>
		/// Thread signal.
		/// </summary>
		internal AutoResetEvent _ClientConnected = null;
		/// <summary>
		/// Thread signal.
		/// </summary>
		internal AutoResetEvent _ServerReady = null;

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
					return this.receivedMail.ToArray();
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
					return this.receivedMail.Count;
			}
		}

		/// <summary>
		/// Clears the received email.
		/// </summary>
		public void ClearReceivedEmail()
		{
			lock (this)
				this.receivedMail.Clear();
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
			_ClientConnected = new AutoResetEvent(false);
			_ServerReady = new AutoResetEvent(false);
			_Processor = new SmtpProcessor(string.Empty, receivedMail);
		}

		#endregion

		#region Private and Internal Methods

		/// <summary>
		/// Starts the Server
		/// </summary>
		internal void _Start()
		{
			_Log.Info("Starting Smtp server");

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Port);
			_Listener = new TcpListener(endPoint);
			_Listener.Start();

			_Log.DebugFormat("Started Tcp Listener at port {0}", Port);

			_ClientConnected.Set();

			try
			{
				_ClientConnected.Reset();
				_Log.Debug("Calling BeginAcceptSocket.");
				_Listener.BeginAcceptSocket(new AsyncCallback(_SocketHandler), _Listener);
				_Log.Debug("BeginAcceptSocket called.");
				_ServerReady.Set();
				_ClientConnected.WaitOne();
			}
			catch (Exception ex)
			{
				_Log.Warn("Unexpected Exception starting the SmtpServer.", ex);
			}
			finally
			{
				_ClientConnected.Set();
			}
		}

		/// <summary>
		/// Async Socket handler.
		/// </summary>
		/// <param name="result">The result.</param>
		private void _SocketHandler(IAsyncResult result)
		{
			_Log.Debug("Entering Socket Handler.");

			try
			{
				TcpListener listener = (TcpListener)result.AsyncState;

				_Log.Debug("Calling EndAcceptSocket.");
				var socket = listener.EndAcceptSocket(result);
				_Log.Debug("Socket accepted and ready to be processed.");
				_Processor.ProcessConnection(socket);

				// If socket is closed by any reason we should start listening again recursively.
				// This is a failsafe for smtp authentications tests.
				_Listener.BeginAcceptSocket(new AsyncCallback(_SocketHandler), _Listener);
			}
			catch (ObjectDisposedException ex)
			{
				_Log.Warn("Object Disposed Exception. THIS IS EXPECTED ONLY IF SERVER WAS STOPPED.", ex);
			}
			catch (SocketException ex)
			{
				_Log.Warn("Socket Exception", ex);
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
			server._ServerReady.WaitOne();
			return server;
		}

		/// <summary>
		/// Stop the server.  This notifies the listener to stop accepting new connections
		/// and that the loop should exit.
		/// </summary>
		public void Stop()
		{
			_Log.Debug("Trying to stop SmtpServer.");

			try
			{
				lock(this)
				{
					// Kick the server accept loop
					if (_Listener != null)
					{
						_Processor.Stop();
						_Log.Debug("Stopping tcp listener.");
						_Listener.Stop();
						_Log.Debug("Tcp listener stopped.");
					}
					_Listener = null;
				}
			}
			catch (Exception ex)
			{
				_Log.Warn("Unexpected Exception stopping SmtpServer", ex);
			}
			finally
			{
				_Log.Debug("SmtpServer Stopped.");
				_ClientConnected.Set();
			}
		}

		#endregion
	}

}

