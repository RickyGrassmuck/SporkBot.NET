﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sys-module.
    /// </summary>
    public class SwitchConnectionAsync : SwitchConnectionBase
    {
        public SwitchConnectionAsync(string ipaddress, int port, SwitchBotConfig cfg) : base(ipaddress, port, cfg) { }
        public SwitchConnectionAsync(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port, cfg) { }

        public void Connect()
        {
            if (Config.ConnectionType == ConnectionType.WiFi)
            {
                if (Connected)
                {
                    Log("Already connected prior, skipping initial connection.");
                    return;
                }

                Log("Connecting to device...");
                Connection.Connect(IP, Port);
                Connected = true;
                Log("Connected!");
            }
            else
            {
                Log("Connecting to USB device...");
                ConnectionUSB.ConnectUSB();
                ConnectedUSB = true;
                Log("Connected!");
            }
        }

        public void Reset(string ip)
        {
            if (Connected)
                Disconnect();

            Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Log("Connecting to device...");
            var address = Dns.GetHostAddresses(ip);
            foreach (IPAddress adr in address)
            {
                IPEndPoint ep = new(adr, Port);
                Connection.BeginConnect(ep, ConnectCallback, Connection);
                Connected = true;
                Log("Connected!");
            }
        }

        public void Disconnect()
        {
            if (Config.ConnectionType == ConnectionType.WiFi)
            {
                Log("Disconnecting from device...");
                Connection.Shutdown(SocketShutdown.Both);
                Connection.BeginDisconnect(true, DisconnectCallback, Connection);
                Connected = false;
                Log("Disconnected!");
            }
            else
            {
                Log("Disconnecting from USB device...");
                ConnectionUSB.DisconnectUSB();
                ConnectedUSB = false;
                Log("Disconnected!");
            }
        }

        private readonly AutoResetEvent connectionDone = new(false);

        private void ConnectCallback(IAsyncResult ar)
        {
            // Complete the connection request.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Socket client = (Socket)ar.AsyncState;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            client.EndConnect(ar);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Signal that the connection is complete.
            connectionDone.Set();
            LogUtil.LogInfo("Connected.", Name);
        }

        private readonly AutoResetEvent disconnectDone = new(false);

        private void DisconnectCallback(IAsyncResult ar)
        {
            // Complete the disconnect request.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Socket client = (Socket)ar.AsyncState;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            client.EndDisconnect(ar);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Signal that the disconnect is complete.
            disconnectDone.Set();
            LogUtil.LogInfo("Disconnected.", Name);
        }

        public int Read(byte[] buffer)
        {
            int br = Connection.Receive(buffer, 0, 1, SocketFlags.None);
            while (buffer[br - 1] != (byte)'\n')
                br += Connection.Receive(buffer, br, 1, SocketFlags.None);
            return br;
        }

        public async Task<int> SendAsync(byte[] buffer, ConnectionType type, CancellationToken token)
        {
            return type switch
            {
                ConnectionType.WiFi => await Task.Run(() => Connection.Send(buffer), token).ConfigureAwait(false),
                ConnectionType.USB => ConnectionUSB.SendUSB(buffer),
                _ => throw new NotImplementedException(),
            };
        }

        private async Task<byte[]> ReadBytesFromCmdAsync(byte[] cmd, int length, CancellationToken token)
        {
            await SendAsync(cmd, Config.ConnectionType, token).ConfigureAwait(false);

            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public async Task<byte[]> ReadBytesAsync(uint offset, int length, ConnectionType type, CancellationToken token)
        {
            return type switch
            {
                ConnectionType.WiFi => await ReadBytesFromCmdAsync(SwitchCommand.Peek(offset, length), length, token).ConfigureAwait(false),
                ConnectionType.USB => ConnectionUSB.ReadBytesUSB(offset, length),
                _ => throw new NotImplementedException(),
            };
        }

        public async Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
        {
            return await ReadBytesFromCmdAsync(SwitchCommand.PeekAbsolute(offset, length), length, token).ConfigureAwait(false);
        }

        public async Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token)
        {
            return await ReadBytesFromCmdAsync(SwitchCommand.PeekMain(offset, length), length, token).ConfigureAwait(false);
        }

        public async Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
        {
            byte[] baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetMainNsoBase(), sizeof(ulong), token).ConfigureAwait(false);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public async Task<ulong> GetHeapBaseAsync(CancellationToken token)
        {
            var baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetHeapBase(), sizeof(ulong), token).ConfigureAwait(false);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public async Task WriteBytesAsync(byte[] data, uint offset, ConnectionType type, CancellationToken token)
        {
            var cmd = SwitchCommand.Poke(offset, data);
            switch (type)
            {
                case ConnectionType.WiFi: await SendAsync(cmd, Config.ConnectionType, token).ConfigureAwait(false); break;
                case ConnectionType.USB: ConnectionUSB.WriteBytesUSB(data, offset); break;
            };
        }

        public async Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token)
        {
            var cmd = SwitchCommand.PokeAbsolute(offset, data);
            await SendAsync(cmd, Config.ConnectionType, token).ConfigureAwait(false);
        }
    }
}
