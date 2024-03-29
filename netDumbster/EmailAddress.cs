// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

/// <summary>
/// Stores a single EmailAddress.  The class will only
/// represent valid email addresses, and will never contain
/// an invalid address.
/// </summary>
/// <remarks>
/// This class provides a way to store and pass a valid email address
/// within the system.  This class can not be created with an invalid address,
/// so if parameter of this type is not null, the address can be assumed to
/// be valid.
/// </remarks>
public class EmailAddress
{
    private readonly Regex ILLEGAL_CHARACTERS = new("[][)(@><\\\",;:]");

    /// <summary>
    /// Creates a new EmailAddress using a valid address.
    /// </summary>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the username or domain is invalid.
    /// </exception>
    public EmailAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new InvalidEmailAddressException("Invalid address.  Specified address is empty");
        }

        string[] addressParts = address.Split("@".ToCharArray());
        if (addressParts.Length != 2)
        {
            throw new InvalidEmailAddressException("Invalid address.  The address must be formatted as: username@domain.");
        }

        Username = addressParts[0];
        Domain = addressParts[1];

        ValidateAddress(Username, Domain);
    }

    /// <summary>
    /// Creates a new EmailAddress using valid name and domain.
    /// </summary>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the username or domain is invalid.
    /// </exception>
    public EmailAddress(string username, string domain)
    {
        Username = username;
        Domain = domain;

        ValidateAddress(Username, Domain);
    }

    /// <summary>
    /// The entire EmailAddress (username@domian)
    /// </summary>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the address is invalid.
    /// </exception>
    public string Address
    {
        get
        {
            return Username + "@" + Domain;
        }
    }

    /// <summary>
    /// The domain component of the EmailAddress.  This
    /// consists of everything after the @.
    /// </summary>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the domain is invalid.
    /// </exception>
    public string Domain
    {
        get;
    }

    /// <summary>
    /// The username component of the EmailAddress.  This
    /// consists of everything before the @.
    /// </summary>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the username is invalid.
    /// </exception>
    public string Username
    {
        get;
    }

    /// <summary>
    /// Returns the email address as: "user@domain.com".;
    /// </summary>
    /// <returns>Value of Address Property.</returns>
    public override string ToString()
    {
        return Address;
    }

    /// <summary>
    /// Checks the specified string to verify it does not
    /// contain any of the following characters: ( ) &lt; &gt; @ , ; : \ " . [ ]
    /// </summary>
    /// <param name="data">The string to test</param>
    /// <exception cref="InvalidEmailAddressException">
    /// Thrown if the data contains any illegal special characters.
    /// </exception>
    private void VerifySpecialCharacters(string data)
    {
        if (ILLEGAL_CHARACTERS.IsMatch(data))
        {
            throw new InvalidEmailAddressException("Invalid address.  The username and domain address parts can not contain any of the following characters: ( ) < > @ , ; : \\ \" . [ ]");
        }
    }

    private void ValidateAddress(string username, string domain)
    {
        if (string.IsNullOrEmpty(username))
        {
            throw new InvalidEmailAddressException("Invalid username.  Username must be at least one charecter");
        }
        // Verify that the username does not contain illegal characters.
        VerifySpecialCharacters(username);

        if (domain == null || domain.Length < 5)
        {
            throw new InvalidEmailAddressException("Invalid domain.  Domain must be at least 5 charecters (a.com, a.edu, etc...)");
        }

        // Verify that the domain does not contain illegal characters.
        VerifySpecialCharacters(domain);
    }
}

public class EmailAddressComparer : IEqualityComparer<EmailAddress>
{
    public bool Equals(EmailAddress x, EmailAddress y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Address == y.Address;
    }

    public int GetHashCode(EmailAddress obj)
    {
        return obj.Address != null ? obj.Address.GetHashCode() : 0;
    }
}
