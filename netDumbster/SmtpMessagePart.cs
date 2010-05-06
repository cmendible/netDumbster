// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible 

using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace netDumbster.smtp
{
	/// <summary>
	/// Stores a single part of a multipart message.
	/// </summary>
	public class SmtpMessagePart
	{
		#region Constants

		private static readonly string DOUBLE_NEWLINE = Environment.NewLine + Environment.NewLine;

		#endregion

		#region Variables

		private Hashtable headerFields = null;
		private string headerData = String.Empty;
		private string bodyData = String.Empty;

		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Creates a new message part.  The input string should be the body of 
		/// the attachment, without the "------=_NextPart" separator strings.
		/// The last 4 characters of the data will be "\r\n\r\n".
		/// </summary>
		public SmtpMessagePart( string data )
		{
			string[] parts = Regex.Split( data, DOUBLE_NEWLINE );

			headerData = parts[0] + DOUBLE_NEWLINE;
			bodyData = parts[1] + DOUBLE_NEWLINE;					
		}
		
		#endregion
		
		#region Properies
		
		/// <summary>
		/// A hash table of all the Headers in the email message.  They keys
		/// are the header names, and the values are the assoicated values, including
		/// any sub key/value pairs is the header.
		/// </summary>
		public Hashtable Headers
		{
			get
			{
				if( headerFields == null )
				{
					headerFields = SmtpMessage.ParseHeaders( headerData );
				}
				return headerFields;
			}
		}
		/// <summary>
		/// The raw text that represents the header of the mime part.
		/// </summary>
		public string HeaderData
		{
			get { return headerData; }
		}

		/// <summary>
		/// The raw text that represents the actual mime part.
		/// </summary>
		public string BodyData
		{
			get { return bodyData; }
		}

		#endregion
	}
}
