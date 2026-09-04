using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ZeroUI.Core.Scada;

namespace ZeroUI.Core.Communication
{
    /// <summary>
    /// High-throughput Modbus TCP master protocol adapter.
    /// Communicates with PLCs and RTUs over standard port 502,
    /// decoding Coils, Discrete Inputs, and Holding Registers directly into ZeroTagEngine.
    /// </summary>
    public sealed class ModbusTcpAdapter : IProtocolAdapter
    {
        private readonly string _adapterId;
        private readonly string _host;
        private readonly int _port;
        private readonly byte _unitId;
        private readonly TimeSpan _timeout;

        private readonly ConcurrentDictionary<string, AdapterTagDefinition> _tags =
            new ConcurrentDictionary<string, AdapterTagDefinition>(StringComparer.OrdinalIgnoreCase);

        private Socket? _socket;
        private ushort _transactionId;
        private AdapterConnectionState _state = AdapterConnectionState.Disconnected;
        private TimeSpan _latency = TimeSpan.Zero;
        private readonly object _syncLock = new object();
        private bool _isDisposed;

        public string AdapterId => _adapterId;
        public string Endpoint => $"{_host}:{_port}";
        public AdapterConnectionState State => _state;
        public TimeSpan Latency => _latency;

        public event Action<IProtocolAdapter, AdapterConnectionState>? StateChanged;

        public ModbusTcpAdapter(
            string adapterId,
            string host,
            int port = 502,
            byte unitId = 1,
            TimeSpan? timeout = null)
        {
            _adapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _unitId = unitId;
            _timeout = timeout ?? TimeSpan.FromSeconds(3);
        }

        public void RegisterTag(AdapterTagDefinition tagDef)
        {
            if (tagDef == null) throw new ArgumentNullException(nameof(tagDef));
            _tags[tagDef.TagPath] = tagDef;
        }

        public IReadOnlyCollection<AdapterTagDefinition> GetRegisteredTags()
        {
            return (IReadOnlyCollection<AdapterTagDefinition>)_tags.Values;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ModbusTcpAdapter));
            UpdateState(AdapterConnectionState.Connecting);

            try
            {
                DisconnectSocket();

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true,
                    SendTimeout = (int)_timeout.TotalMilliseconds,
                    ReceiveTimeout = (int)_timeout.TotalMilliseconds
                };

                var connectTask = socket.ConnectAsync(_host, _port);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(_timeout, cancellationToken)).ConfigureAwait(false);

                if (completedTask != connectTask)
                {
                    socket.Dispose();
                    throw new TimeoutException($"Modbus connection to {Endpoint} timed out.");
                }

                _socket = socket;
                UpdateState(AdapterConnectionState.Connected);
            }
            catch (Exception)
            {
                UpdateState(AdapterConnectionState.Faulted);
                throw;
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectSocket();
            UpdateState(AdapterConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public async Task PollOnceAsync(CancellationToken cancellationToken = default)
        {
            if (_socket == null || !_socket.Connected)
            {
                UpdateState(AdapterConnectionState.Disconnected);
                return;
            }

            var sw = Stopwatch.StartNew();

            foreach (var kvp in _tags)
            {
                var tag = kvp.Value;
                try
                {
                    await PollTagAsync(tag, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    ZeroTagEngine.SetTagValue(tag.TagPath, null, ScadaQuality.Bad);
                }
            }

            sw.Stop();
            _latency = sw.Elapsed;
        }

        public async Task<bool> WriteTagAsync(string tagPath, object value, CancellationToken cancellationToken = default)
        {
            if (!_tags.TryGetValue(tagPath, out var tag) || _socket == null || !_socket.Connected)
            {
                return false;
            }

            if (!ushort.TryParse(tag.FieldAddress, out ushort registerAddress))
            {
                return false;
            }

            // Write Holding Register (FC6) or Coil (FC5)
            if (tag.DataType == TagDataType.Boolean)
            {
                bool bVal = Convert.ToBoolean(value);
                ushort coilVal = bVal ? (ushort)0xFF00 : (ushort)0x0000;
                var request = BuildMbapPacket(functionCode: 0x05, registerAddress, coilVal);
                var response = await SendReceiveAsync(request, cancellationToken).ConfigureAwait(false);
                return response != null && response.Length >= 12;
            }
            else
            {
                ushort regVal = Convert.ToUInt16(value);
                var request = BuildMbapPacket(functionCode: 0x06, registerAddress, regVal);
                var response = await SendReceiveAsync(request, cancellationToken).ConfigureAwait(false);
                return response != null && response.Length >= 12;
            }
        }

        private async Task PollTagAsync(AdapterTagDefinition tag, CancellationToken ct)
        {
            if (!ushort.TryParse(tag.FieldAddress, out ushort startAddress)) return;

            ushort numRegisters = tag.DataType switch
            {
                TagDataType.Float32 => 2,
                TagDataType.Int32 => 2,
                TagDataType.UInt32 => 2,
                TagDataType.Double64 => 4,
                _ => 1
            };

            // Read Holding Registers (FC3)
            var request = BuildMbapPacket(functionCode: 0x03, startAddress, numRegisters);
            var response = await SendReceiveAsync(request, ct).ConfigureAwait(false);

            if (response != null && response.Length >= 9)
            {
                byte byteCount = response[8];
                if (response.Length >= 9 + byteCount)
                {
                    var dataSlice = new ReadOnlySpan<byte>(response, 9, byteCount);
                    double parsedVal = DecodeRegisterData(dataSlice, tag.DataType);
                    double scaledVal = parsedVal * tag.Scale + tag.Offset;

                    ZeroTagEngine.SetTagValue(tag.TagPath, scaledVal, ScadaQuality.Good);
                }
            }
        }

        private static double DecodeRegisterData(ReadOnlySpan<byte> data, TagDataType dataType)
        {
            if (data.Length < 2) return 0.0;

            switch (dataType)
            {
                case TagDataType.Boolean:
                    return data[1] != 0 ? 1.0 : 0.0;

                case TagDataType.Int16:
                    return BinaryPrimitives.ReadInt16BigEndian(data);

                case TagDataType.UInt16:
                    return BinaryPrimitives.ReadUInt16BigEndian(data);

                case TagDataType.Int32 when data.Length >= 4:
                    return BinaryPrimitives.ReadInt32BigEndian(data);

                case TagDataType.UInt32 when data.Length >= 4:
                    return BinaryPrimitives.ReadUInt32BigEndian(data);

                case TagDataType.Float32 when data.Length >= 4:
                    // Standard Modbus IEEE 754 Big Endian (High word first)
                    uint rawBits = BinaryPrimitives.ReadUInt32BigEndian(data);
                    return ZeroUI.Core.Common.ZeroMemory.Int32BitsToSingle((int)rawBits);

                default:
                    return BinaryPrimitives.ReadUInt16BigEndian(data);
            }
        }

        private byte[] BuildMbapPacket(byte functionCode, ushort address, ushort valueOrCount)
        {
            var packet = new byte[12];
            lock (_syncLock)
            {
                _transactionId++;
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), _transactionId);
            }

            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), 0x0000); // Protocol ID (Modbus)
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), 0x0006); // Length (6 bytes follow)
            packet[6] = _unitId;
            packet[7] = functionCode;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(8, 2), address);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(10, 2), valueOrCount);

            return packet;
        }

        private async Task<byte[]?> SendReceiveAsync(byte[] request, CancellationToken ct)
        {
            if (_socket == null || !_socket.Connected) return null;

            try
            {
                var sendSegment = new ArraySegment<byte>(request);
                await _socket.SendAsync(sendSegment, SocketFlags.None).ConfigureAwait(false);

                var buffer = new byte[260];
                var recvSegment = new ArraySegment<byte>(buffer);
                int received = await _socket.ReceiveAsync(recvSegment, SocketFlags.None).ConfigureAwait(false);

                if (received <= 0) return null;

                var result = new byte[received];
                Array.Copy(buffer, result, received);
                return result;
            }
            catch
            {
                UpdateState(AdapterConnectionState.Faulted);
                return null;
            }
        }

        private void DisconnectSocket()
        {
            try
            {
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
                _socket?.Dispose();
            }
            catch
            {
                // Ignored during cleanup
            }
            finally
            {
                _socket = null;
            }
        }

        private void UpdateState(AdapterConnectionState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                try
                {
                    StateChanged?.Invoke(this, newState);
                }
                catch
                {
                    // Guard subscriber exceptions
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            DisconnectSocket();
        }
    }
}
