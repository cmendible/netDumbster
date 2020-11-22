// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp
{
    using System.Text.RegularExpressions;

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
        private string domain;
        private Regex ILLEGAL_CHARACTERS = new Regex( "[][)(@><\\\",;:]");
        private string username;

        /// <summary>
        /// Creates a new EmailAddress using a valid address.
        /// </summary>
        /// <exception cref="InvalidEmailAddressException">
        /// Thrown if the username or domain is invalid.
        /// </exception>
        public EmailAddress(string address)
        {
            this.Address = address;
        }

        /// <summary>
        /// Creates a new EmailAddress using valid name and domain.
        /// </summary>
        /// <exception cref="InvalidEmailAddressException">
        /// Thrown if the username or domain is invalid.
        /// </exception>
        public EmailAddress(string username, string domain)
        {
            this.Username = username;
            this.Domain = domain;
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
                return this.username + "@" + this.domain;
            }

            set
            {
                // Verify it isn't null/empty.
                if (value == null || value.Length == 0)
                {
                    throw new InvalidEmailAddressException( "Invalid address.  Specified address is empty");
                }

                string[] addressParts = value.Split("@".ToCharArray());
                if (addressParts.Length != 2)
                {
                    throw new InvalidEmailAddressException( "Invalid address.  The address must be formatted as: username@domain.");
                }

                // Store the individual parts.  These methods will
                // verify that the individual parts are valid or will
                // throw their own InvalidEmailAddressException
                this.Username = addressParts[0];
                this.Domain = addressParts[1];
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
            get
            {
                return this.domain;
            }

            set
            {
                if (value == null || value.Length < 5)
                {
                    throw new InvalidEmailAddressException( "Invalid domian.  Domain must be at least 5 charecters (a.com, a.edu, etc...)");
                }

                // Verify that the username does not contain illegal characters.
                this.VerifySpecialCharacters(value);

                this.domain = value;
            }
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
            get
            {
                return this.username;
            }

            set
            {
                if (value == null || value.Length == 0)
                {
                    throw new InvalidEmailAddressException( "Invalid username.  Username must be at least one charecter");
                }

                // Verify that the username does not contain illegal characters.
                this.VerifySpecialCharacters(value);

                this.username = value;
            }
        }

        /// <summary>
        /// Returns the email address as: "user@domain.com".;
        /// </summary>
        /// <returns>Value of Address Property.</returns>
        public override string ToString()
        {
            return this.Address;
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
            if (this.ILLEGAL_CHARACTERS.IsMatch(data))
            {
                throw new InvalidEmailAddressException("Invalid address.  The username and domain address parts can not contain any of the following characters: ( ) < > @ , ; : \\ \" . [ ]");
            }
        }
    }
}