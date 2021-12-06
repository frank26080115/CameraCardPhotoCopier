using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;

/// <summary>
/// Copied from https://www.codeproject.com/articles/380769/creating-an-ftp-server-in-csharp-with-ipv6-support
/// Modified to remove SSL, remove user database
/// Main modification is to rename and move the photo files as they are being transferred, but their existance are still tracked and reported back to the FTP client
/// </summary>

namespace SharpFtpServer
{
    public enum FtpClientStatus
    {
        // Sony cameras will immediately establish FTP connection as soon as the camera is turned on
        // detecting and notifying the user of every new connection is quite annoying
        // so we have two different connected states, and only notify upon a real file transfer
        None,
        Connected,
        Transfering,
    };

    public class FtpServer : IDisposable
    {

        public FtpClientStatus new_client_flag = FtpClientStatus.None;

        private bool _disposed = false;
        private bool _listening = false;

        private TcpListener _listener;
        private List<ClientConnection> _activeConnections;

        private IPEndPoint _localEndPoint;

        public FtpServer()
            : this(IPAddress.Any, 21)
        {
        }

        public FtpServer(IPAddress ipAddress, int port)
        {
            _localEndPoint = new IPEndPoint(ipAddress, port);
        }

        public FtpServer(int port)
            : this(IPAddress.Any, port)
        {
        }

        public void Start()
        {
            _listener = new TcpListener(_localEndPoint);

            _listening = true;
            _listener.Start();

            _activeConnections = new List<ClientConnection>();

            _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
        }

        public void Stop()
        {
            _listening = false;
            _listener.Stop();

            _listener = null;
        }

        private void HandleAcceptTcpClient(IAsyncResult result)
        {
            if (_listening)
            {
                _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);

                TcpClient client = _listener.EndAcceptTcpClient(result);

                ClientConnection connection = new ClientConnection(client, this);

                if (new_client_flag == FtpClientStatus.None)
                {
                    new_client_flag = FtpClientStatus.Connected;
                }

                _activeConnections.Add(connection);

                ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();

                    foreach (ClientConnection conn in _activeConnections)
                    {
                        conn.Dispose();
                    }
                }
            }

            _disposed = true;
        }
    }
}
