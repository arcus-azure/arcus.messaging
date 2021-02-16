using System;
using System.Net;
using System.Net.Sockets;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Custom <see cref="TcpListener"/> implementation to have access to the <c>protected</c> <see cref="Active"/> property.
    /// </summary>
    internal class CustomTcpListener : TcpListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTcpListener"/> class.
        /// </summary>
        /// <param name="port">The port on which to listen for incoming connection attempts.</param>
        [Obsolete("This method has been deprecated. Please use TcpListener(IPAddress localaddr, int port) instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public CustomTcpListener(int port) : base(port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTcpListener"/> class.
        /// </summary>
        /// <param name="localaddr">The <see cref="IPAddress"/> that represents the local IP address.</param>
        /// <param name="port">The port on which to listen for incoming connection attempts.</param>
        public CustomTcpListener(IPAddress localaddr, int port) : base(localaddr, port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTcpListener"/> class.
        /// </summary>
        /// <param name="localEP">The <see cref="IPEndPoint"/> that represents the local endpoint to which to bind the listener <see cref="Socket"/>.</param>
        public CustomTcpListener(IPEndPoint localEP) : base(localEP)
        {
        }

        /// <summary>
        /// Gets the flag indicating whether or not the <see cref="TcpListener"/> is actively listening for client connections.
        /// </summary>
        public new bool Active => base.Active;
    }
}