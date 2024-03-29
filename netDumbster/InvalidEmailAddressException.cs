// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

/// <summary>
/// Indicates that an email address is not valid.
/// </summary>
/// <remarks>
/// Thrown by the EmailAddress class when part of all of the email
/// address being set is invalid.
/// </remarks>
public class InvalidEmailAddressException : ApplicationException
{
    /// <summary>
    /// Creates a new Exception with a user-displayable message.
    /// </summary>
    public InvalidEmailAddressException(string userMessage)
    : base(userMessage)
    {
    }
}
