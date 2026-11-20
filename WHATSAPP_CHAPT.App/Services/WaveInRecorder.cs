#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed class WaveInRecorder : IDisposable
    {
        private const uint CallbackFunction = 0x00030000;
        private const ushort WaveFormatPcm = 1;
        private const uint WaveMapper = 0xFFFFFFFF;
        private const uint WimmData = 0x3C0;
        private const int BufferBytes = 3200;
        private const int BufferCount = 4;
        private const int SampleRate = 16000;

        private readonly object _sync = new();
        private readonly WaveInCallback _callback;
        private readonly List<IntPtr> _headers = new();
        private readonly List<IntPtr> _buffers = new();
        private readonly MemoryStream _audio = new();
        private IntPtr _device;
        private bool _recording;
        private bool _disposed;
        private int _peak;
        private int _openResult;

        public WaveInRecorder()
        {
            _callback = OnWaveInCallback;
        }

        public int Peak => Volatile.Read(ref _peak);
        public bool IsRecording => _recording;
        public bool IsPaused { get; private set; }

        public bool Start(out string? error)
        {
            ThrowIfDisposed();
            error = null;
            if (_recording)
            {
                return true;
            }

            var format = new WaveFormat
            {
                FormatTag = WaveFormatPcm,
                Channels = 1,
                SamplesPerSecond = SampleRate,
                AverageBytesPerSecond = SampleRate * 2,
                BlockAlign = 2,
                BitsPerSample = 16,
                ExtraSize = 0
            };

            _openResult = WaveInOpen(out _device, WaveMapper, ref format, _callback, IntPtr.Zero, CallbackFunction);
            if (_openResult != 0)
            {
                error = $"Microphone indisponible (waveInOpen, code {_openResult}). Vérifiez les autorisations du microphone dans les paramètres Windows.";
                return false;
            }

            for (int i = 0; i < BufferCount; i++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(BufferBytes);
                IntPtr headerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHeader>());
                var header = new WaveHeader
                {
                    Data = buffer,
                    BufferLength = (uint)BufferBytes,
                    BytesRecorded = 0,
                    User = IntPtr.Zero,
                    Flags = 0,
                    Loops = 0,
                    Next = IntPtr.Zero,
                    Reserved = IntPtr.Zero
                };
                Marshal.StructureToPtr(header, headerPointer, false);
                _buffers.Add(buffer);
                _headers.Add(headerPointer);

                int prepare = WaveInPrepareHeader(_device, headerPointer, (uint)Marshal.SizeOf<WaveHeader>());
                if (prepare != 0)
                {
                    error = $"Impossible de préparer le microphone (code {prepare}).";
                    CloseDevice();
                    return false;
                }

                int add = WaveInAddBuffer(_device, headerPointer, (uint)Marshal.SizeOf<WaveHeader>());
                if (add != 0)
                {
                    error = $"Impossible de démarrer le tampon du microphone (code {add}).";
                    CloseDevice();
                    return false;
                }
            }

            int start = WaveInStart(_device);
            if (start != 0)
            {
                error = $"Impossible de démarrer le microphone (code {start}).";
                CloseDevice();
                return false;
            }

            _recording = true;
            IsPaused = false;
            return true;
        }

        public bool Pause()
        {
            if (!_recording || IsPaused)
            {
                return false;
            }
            int result = WaveInStop(_device);
            if (result != 0)
            {
                return false;
            }
            IsPaused = true;
            return true;
        }

        public bool Resume()
        {
            if (!_recording || !IsPaused)
            {
                return false;
            }
            int result = WaveInStart(_device);
            if (result != 0)
            {
                return false;
            }
            IsPaused = false;
            return true;
        }

        public void WriteWaveFile(string path)
        {
            StopCapture();
            byte[] data;
            lock (_sync)
            {
                data = _audio.ToArray();
            }
            using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            WriteWave(output, data);
        }

        internal static void WriteWave(Stream output, byte[] data)
        {
            ArgumentNullException.ThrowIfNull(output);
            ArgumentNullException.ThrowIfNull(data);
            using var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + data.Length);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)WaveFormatPcm);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(data.Length);
            writer.Write(data);
            writer.Flush();
        }

        internal static int CalculatePeak(byte[] samples, int byteCount)
        {
            ArgumentNullException.ThrowIfNull(samples);
            if (byteCount < 0 || byteCount > samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            int peak = 0;
            for (int i = 0; i + 1 < byteCount; i += 2)
            {
                short sample = BitConverter.ToInt16(samples, i);
                peak = Math.Max(peak, Math.Abs((int)sample));
            }
            return peak;
        }

        public void Cancel()
        {
            StopCapture();
            lock (_sync)
            {
                _audio.SetLength(0);
            }
        }

        private void OnWaveInCallback(IntPtr device, uint message, IntPtr instance, IntPtr parameter1, IntPtr parameter2)
        {
            if (message != WimmData || parameter1 == IntPtr.Zero || !_recording || IsPaused)
            {
                return;
            }

            var header = Marshal.PtrToStructure<WaveHeader>(parameter1);
            if (header.BytesRecorded == 0 || header.Data == IntPtr.Zero)
            {
                return;
            }

            int byteCount = Math.Min((int)header.BytesRecorded, BufferBytes);
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(header.Data, bytes, 0, byteCount);
            lock (_sync)
            {
                _audio.Write(bytes, 0, bytes.Length);
            }

            Volatile.Write(ref _peak, CalculatePeak(bytes, bytes.Length));

            if (_recording && !IsPaused)
            {
                header.BytesRecorded = 0;
                Marshal.StructureToPtr(header, parameter1, false);
                _ = WaveInAddBuffer(_device, parameter1, (uint)Marshal.SizeOf<WaveHeader>());
            }
        }

        private void StopCapture()
        {
            if (!_recording)
            {
                return;
            }
            _recording = false;
            IsPaused = false;
            if (_device != IntPtr.Zero)
            {
                _ = WaveInStop(_device);
                _ = WaveInReset(_device);
                foreach (IntPtr header in _headers)
                {
                    _ = WaveInUnprepareHeader(_device, header, (uint)Marshal.SizeOf<WaveHeader>());
                }
                _ = WaveInClose(_device);
                _device = IntPtr.Zero;
            }
            FreeBuffers();
        }

        private void CloseDevice()
        {
            if (_device != IntPtr.Zero)
            {
                _ = WaveInStop(_device);
                _ = WaveInReset(_device);
                foreach (IntPtr header in _headers)
                {
                    _ = WaveInUnprepareHeader(_device, header, (uint)Marshal.SizeOf<WaveHeader>());
                }
                _ = WaveInClose(_device);
                _device = IntPtr.Zero;
            }
            FreeBuffers();
        }

        private void FreeBuffers()
        {
            foreach (IntPtr header in _headers)
            {
                Marshal.FreeHGlobal(header);
            }
            foreach (IntPtr buffer in _buffers)
            {
                Marshal.FreeHGlobal(buffer);
            }
            _headers.Clear();
            _buffers.Clear();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            StopCapture();
            _audio.Dispose();
            _disposed = true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveFormat
        {
            public ushort FormatTag;
            public ushort Channels;
            public uint SamplesPerSecond;
            public uint AverageBytesPerSecond;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public ushort ExtraSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr Data;
            public uint BufferLength;
            public uint BytesRecorded;
            public IntPtr User;
            public uint Flags;
            public uint Loops;
            public IntPtr Next;
            public IntPtr Reserved;
        }

        private delegate void WaveInCallback(
            IntPtr device,
            uint message,
            IntPtr instance,
            IntPtr parameter1,
            IntPtr parameter2);

        [DllImport("winmm.dll", EntryPoint = "waveInOpen")]
        private static extern int WaveInOpen(
            out IntPtr handle,
            uint deviceId,
            ref WaveFormat format,
            WaveInCallback callback,
            IntPtr instance,
            uint flags);

        [DllImport("winmm.dll", EntryPoint = "waveInPrepareHeader")]
        private static extern int WaveInPrepareHeader(IntPtr handle, IntPtr header, uint size);

        [DllImport("winmm.dll", EntryPoint = "waveInUnprepareHeader")]
        private static extern int WaveInUnprepareHeader(IntPtr handle, IntPtr header, uint size);

        [DllImport("winmm.dll", EntryPoint = "waveInAddBuffer")]
        private static extern int WaveInAddBuffer(IntPtr handle, IntPtr header, uint size);

        [DllImport("winmm.dll", EntryPoint = "waveInStart")]
        private static extern int WaveInStart(IntPtr handle);

        [DllImport("winmm.dll", EntryPoint = "waveInStop")]
        private static extern int WaveInStop(IntPtr handle);

        [DllImport("winmm.dll", EntryPoint = "waveInReset")]
        private static extern int WaveInReset(IntPtr handle);

        [DllImport("winmm.dll", EntryPoint = "waveInClose")]
        private static extern int WaveInClose(IntPtr handle);
    }
}
