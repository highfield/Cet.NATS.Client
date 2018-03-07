using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Defines the possible authentication modes
    /// </summary>
    public enum ServerAuthMode
    {
        Anonymous,
        Credentials,
        Token,
    }


    /// <summary>
    /// Describes a connection endpoint to target a NATS server
    /// </summary>
    /// <remarks>
    /// This is an immutable class
    /// </remarks>
    public sealed class ServerEndPoint
    {
        private ServerEndPoint() { }


        /// <summary>
        /// Creates the default endpoint: anonymous and local
        /// </summary>
        /// <returns></returns>
        public static ServerEndPoint Default()
        {
            return Anonymous(Defaults.Address, Defaults.Port, secured: false);
        }


        /// <summary>
        /// Creates an anonymous endpoint
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> to target the server</param>
        /// <param name="port">The TCP port</param>
        /// <param name="secured">Whether to use a secure SSL connection</param>
        /// <returns></returns>
        public static ServerEndPoint Anonymous(
            IPAddress address,
            int port,
            bool secured
            )
        {
            var instance = new ServerEndPoint();
            instance.Address = address ?? throw new ArgumentNullException(nameof(address));
            instance.Port = port;
            instance.Secured = secured;
            instance.AuthMode = ServerAuthMode.Anonymous;
            return instance;
        }


        /// <summary>
        /// Creates an anonymous endpoint
        /// </summary>
        /// <remarks>
        /// Used internally to create the implicit server endpoints as of
        /// the NATS broker inform
        /// </remarks>
        /// <param name="source">A IP:Port string pattern</param>
        /// <param name="secured">Whether to use a secure SSL connection</param>
        /// <returns></returns>
        internal static ServerEndPoint ParseAnonymous(
            string source,
            bool secured
            )
        {
            //see: https://stackoverflow.com/questions/4968795/ipaddress-parse-using-port-on-ipv4
            if (Uri.TryCreate("http://" + source, UriKind.Absolute, out Uri uri) &&
               IPAddress.TryParse(uri.Host, out IPAddress ip)
               )
            {
                return Anonymous(ip, uri.Port, secured);
            }
            return null;
        }


        /// <summary>
        /// Creates an endpoint with credentials
        /// </summary>
        /// <remarks>
        /// This method exposes a clear-string password, which is not the safe-way to specify a secret.
        /// Please, consider the alternate method <seealso cref="WithCredentials(IPAddress, int, string, SecureString, bool)"/>.
        /// </remarks>
        /// <param name="address">The <see cref="IPAddress"/> to target the server</param>
        /// <param name="port">The TCP port</param>
        /// <param name="userName">The credentials user name</param>
        /// <param name="password">The credentials non-null password</param>
        /// <param name="secured">Whether to use a secure SSL connection</param>
        /// <returns></returns>
        public static ServerEndPoint WithCredentials(
            IPAddress address,
            int port,
            string userName,
            string password,
            bool secured
            )
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            //immediately convert to a SecureString
            SecureString secpwd = null;
            try
            {
                secpwd = new SecureString();
                for (int i = 0; i < password.Length; i++)
                {
                    secpwd.AppendChar(password[i]);
                }
                secpwd.MakeReadOnly();

                //invoke the alternate method
                return WithCredentials(
                    address,
                    port,
                    userName,
                    secpwd,
                    secured
                    );
            }
            catch (Exception ex)
            {
                secpwd?.Dispose();
                throw ex;
            }
        }


        /// <summary>
        /// Creates an endpoint with credentials
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> to target the server</param>
        /// <param name="port">The TCP port</param>
        /// <param name="userName">The credentials user name</param>
        /// <param name="password">The credentials password</param>
        /// <param name="secured">Whether to use a secure SSL connection</param>
        /// <returns></returns>
        public static ServerEndPoint WithCredentials(
            IPAddress address,
            int port,
            string userName,
            SecureString password,
            bool secured
            )
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var instance = new ServerEndPoint();
            instance.Address = address ?? throw new ArgumentNullException(nameof(address));
            instance.Port = port;
            instance.UserName = userName;
            instance.Password = password ?? throw new ArgumentNullException(nameof(password));
            instance.Secured = secured;
            instance.AuthMode = ServerAuthMode.Credentials;
            return instance;
        }


        /// <summary>
        /// Creates an endpoint with a token-based authentication
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> to target the server</param>
        /// <param name="port">The TCP port</param>
        /// <param name="authToken">The authentication token</param>
        /// <param name="secured">Whether to use a secure SSL connection</param>
        /// <returns></returns>
        public ServerEndPoint WithToken(
            IPAddress address,
            int port,
            string authToken,
            bool secured
            )
        {
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentNullException(nameof(authToken));
            }

            var instance = new ServerEndPoint();
            instance.Address = address ?? throw new ArgumentNullException(nameof(address));
            instance.Port = port;
            instance.AuthToken = authToken;
            instance.Secured = secured;
            instance.AuthMode = ServerAuthMode.Token;
            return instance;
        }


        /// <summary>
        /// The <see cref="IPAddress"/> to target the NATS server
        /// </summary>
        public IPAddress Address { get; private set; }


        /// <summary>
        /// The TCP port
        /// </summary>
        public int Port { get; private set; }


        /// <summary>
        /// The credentials user name.
        /// </summary>
        /// <remarks>
        /// This property is valid only when <see cref="AuthMode"/> is <see cref="ServerAuthMode.Credentials"/>.
        /// </remarks>
        public string UserName { get; private set; }


        /// <summary>
        /// The credentials secure password
        /// </summary>
        /// <remarks>
        /// This property is valid only when <see cref="AuthMode"/> is <see cref="ServerAuthMode.Credentials"/>.
        /// </remarks>
        internal SecureString Password { get; private set; }


        /// <summary>
        /// The authentication token
        /// </summary>
        /// <remarks>
        /// This property is valid only when <see cref="AuthMode"/> is <see cref="ServerAuthMode.Token"/>.
        /// </remarks>
        internal string AuthToken { get; private set; }


        /// <summary>
        /// Indicates whether to use a secure SSL connection
        /// </summary>
        public bool Secured { get; private set; }


        /// <summary>
        /// Indicates the authentication mode
        /// </summary>
        public ServerAuthMode AuthMode { get; private set; }


        public override bool Equals(object obj)
        {
            if (obj is ServerEndPoint ep)
            {
                if (object.Equals(this.Address, ep.Address) &&
                    this.Port == ep.Port &&
                    this.AuthMode == ep.AuthMode
                    )
                {
                    switch (this.AuthMode)
                    {
                        case ServerAuthMode.Anonymous:
                            return true;

                        case ServerAuthMode.Credentials:
                            return this.UserName == ep.UserName;

                        case ServerAuthMode.Token:
                            return this.AuthToken == ep.AuthToken;
                    }
                }
            }
            return false;
        }


        public override int GetHashCode()
        {
            int h = this.Address.GetHashCode() ^ this.Port.GetHashCode();
            switch (this.AuthMode)
            {
                case ServerAuthMode.Anonymous:
                    break;

                case ServerAuthMode.Credentials:
                    h ^= this.UserName.GetHashCode();
                    break;

                case ServerAuthMode.Token:
                    h ^= this.AuthToken.GetHashCode();
                    break;
            }
            return h;
        }

    }
}
