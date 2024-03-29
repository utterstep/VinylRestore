﻿using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VStepanov.Experiments.Vinyl.Audio
{
    public class WavPcmWriter : AudioWriter
    {
        #region Format structs
        [StructLayout(LayoutKind.Sequential)]
        private struct WavPcmHeader
        {
            private uint sGroupID;
            private uint dwFileLength;
            private uint sRiffType;

            public WavPcmHeader(uint fileLength)
                : this()
            {
                sGroupID = 0x46464952U;
                dwFileLength = fileLength;
                sRiffType = 0x45564157U;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PcmFormatChunk
        {
            private uint sGroupID;
            private uint dwChunkSize;
            private ushort wFormatTag;
            private ushort wChannels;
            private uint dwSamplesPerSec;
            private uint dwAvgBytesPerSec;
            private ushort wBlockAlign;
            private ushort wBitsPerSample;

            public PcmFormatChunk(ushort nOfChannels, uint sampleRate, ushort bitsPerSample)
                : this()
            {
                sGroupID = 0x20746D66U;
                dwChunkSize = 16;
                wFormatTag = 1;
                wChannels = nOfChannels;
                dwSamplesPerSec = sampleRate;
                dwAvgBytesPerSec = (uint)(sampleRate * nOfChannels * (bitsPerSample / 8));
                wBlockAlign = (ushort)(nOfChannels * (bitsPerSample / 8));
                wBitsPerSample = bitsPerSample;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PcmDataChunk
        {
            private uint sGroupID;
            private uint dwChunkSize;

            public PcmDataChunk(byte[] data)
                : this()
            {
                sGroupID = 0x61746164U;
                dwChunkSize = (uint)data.Length;
            }
        } 
        #endregion

        private readonly PcmFormatChunk _formatChunk;

        public WavPcmWriter(int sampleRate, int bitsPerSample, int nOfChannels, string path)
            : base(path)
        {
            _formatChunk = new PcmFormatChunk(
                (ushort)nOfChannels,
                (uint)sampleRate,
                (ushort)bitsPerSample);
        }

        byte[] GetBytes(object @struct)
        {
            int size = Marshal.SizeOf(@struct);
            var arr = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(@struct, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public override void Write(byte[] data, int offset)
        {
            var dataChunk = new PcmDataChunk(data);

            var formatBytes = GetBytes(_formatChunk);
            var dataBytes = GetBytes(dataChunk);

            var fileHeader = new WavPcmHeader((uint)(formatBytes.Length + dataBytes.Length + data.Length + sizeof(uint)));
            var fileHeaderBytes = GetBytes(fileHeader);

            FileStream.Seek(offset, SeekOrigin.Current);

            FileStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
            FileStream.Write(formatBytes, 0, formatBytes.Length);
            FileStream.Write(dataBytes, 0, dataBytes.Length);
            FileStream.Write(data, 0, data.Length);

            FileStream.Flush();
        }
    }
}
