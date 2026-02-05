using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing;

namespace HybridImageGenerator.Models;

public static class PngPatcher
{
    public static void PatchGamma(MemoryStream stream, float gamma) {
        PngValidator.Validate(stream);
        
        if (!TryFindGamaChunk(stream, out var chunk))
            chunk = AppendGamaChunk(stream);
        
        PatchGamaChunk(stream, chunk, gamma);
    }

    private static void PatchGamaChunk(MemoryStream stream, PngChunk gamaChunk, float newGammaValue) {
        const int gamaFieldLength = 4; // each field in gama chunk is 4 bytes long
        const float gamaMultiplier = 100_000;
        
        var span = stream.GetBuffer().AsSpan();
        var gamaSpan = span.Slice((int)gamaChunk.DataStart, gamaFieldLength);
        var crcSpan = span.Slice((int)(gamaChunk.DataStart + gamaFieldLength), gamaFieldLength);
        
        var gamma = (uint)(newGammaValue * gamaMultiplier);
        BinaryPrimitives.WriteUInt32BigEndian(gamaSpan, gamma);

        var crc = CalculateCrcForGamaChunk(gamaChunk.Name, gamma);
        BinaryPrimitives.WriteUInt32BigEndian(crcSpan, crc);
    }

    private static uint CalculateCrcForGamaChunk(uint name, uint value) {
        const int gamaFieldLength = 4; // each field in gama chunk is 4 bytes long

        Span<byte> span = stackalloc byte[gamaFieldLength * 2];
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(0, gamaFieldLength), name);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(gamaFieldLength, gamaFieldLength), value);
        
        var crc = Crc32.HashToUInt32(span);
        return crc;
    }

    private static PngChunk AppendGamaChunk(MemoryStream stream) {
        const int pngSignatureLength = 8;
        const uint gamaSignature = 0x67414D41;
        const int gamaFieldLength = 4; // each field in gama chunk is 4 bytes long
        const int gamaChunkLength = 4 * 4;
        ReadOnlySpan<byte> lengthValue = [0x00, 0x00, 0x00, 0x04];
        ReadOnlySpan<byte> gamaSignatureAsBytes = [0x67, 0x41, 0x4D, 0x41];
        
        stream.Position = pngSignatureLength;
        _ = PngChunk.ReadFromStream(stream); // skip IHDR chunk

        var chunkStartPosition = (int)stream.Position;
        var remainingDataLength = (int)(stream.Length - chunkStartPosition);
        stream.SetLength(stream.Length + gamaChunkLength);
        var span = stream.GetBuffer().AsSpan();
        var currentDataSpan = span.Slice(chunkStartPosition, remainingDataLength);
        var newDataSpan = span.Slice(chunkStartPosition + gamaChunkLength, remainingDataLength);
        currentDataSpan.CopyTo(newDataSpan);

        var lengthSpan = span.Slice(chunkStartPosition, gamaFieldLength);
        var nameSpan = span.Slice(chunkStartPosition + gamaFieldLength, gamaFieldLength);
        lengthValue.CopyTo(lengthSpan);
        gamaSignatureAsBytes.CopyTo(nameSpan);
            
        var chunk = new PngChunk() {
            Length = gamaFieldLength, 
            Name = gamaSignature,
            DataStart = chunkStartPosition + gamaFieldLength * 2,
            Crc = 0
        };
        return chunk;
    }
    
    private static bool TryFindGamaChunk(MemoryStream stream, out PngChunk result) {
        const int pngHeaderLength = 8;
        const uint gamaName = 0x67414D41;
        const uint iDatName = 0x49444154;
        
        stream.Position = pngHeaderLength;
        var chunk = PngChunk.ReadFromStream(stream);
        while (chunk.Name != iDatName) {
            if (chunk.Name != gamaName) {
                chunk = PngChunk.ReadFromStream(stream);
                continue;
            }
            
            result = chunk;
            return true;
        }

        result = default;
        return false;
    }
    
    private static class PngValidator {
        public static void Validate(MemoryStream stream) {
            if (!ValidateSignature(stream))
                throw new InvalidPngException("Invalid PNG signature");

            if (!ValidateIHdrChunk(stream))
                throw new InvalidPngException("Missing IHDR chunk;");
                
            if (!ValidateIDatChunk(stream))
                throw new InvalidPngException("Missing IDAT chunk;");
            
            if (!ValidateIEndChunk(stream))
                throw new InvalidPngException("Missing IEND chunk;");
        }

        private static bool ValidateSignature(MemoryStream stream) {
            const int pngSignatureLength = 8;
            const ulong pngSignature = 0x89504E470D0A1A0A;

            if (stream.Length < pngSignatureLength)
                return false;
            
            var span = stream.GetBuffer().AsSpan();
            var signature = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(0, pngSignatureLength));

            return signature == pngSignature;
        }

        private static bool ValidateIHdrChunk(MemoryStream stream) {
            const uint iHdrSignature = 0x49484452;
            const int pngHeaderLength = 8;

            stream.Position = pngHeaderLength; 
            var chunk = PngChunk.ReadFromStream(stream);
            return chunk.Name == iHdrSignature;
        }

        private static bool ValidateIDatChunk(MemoryStream stream) {
            const uint iDatSignature = 0x49444154;
            
            while (stream.Position < stream.Length) {
                var chunk = PngChunk.ReadFromStream(stream);
                if (chunk.Name == iDatSignature)
                    return true;
            }

            return false;
        }

        private static bool ValidateIEndChunk(MemoryStream stream) {
            const int iEndChunkLength = 12;
            const uint iEndSignature = 0x49454E44;

            stream.Position = stream.Length - iEndChunkLength;
            var chunk = PngChunk.ReadFromStream(stream);
            return chunk.Name == iEndSignature;
        }
    }
    

    public readonly ref struct PngChunk {
        public uint Length { get; init; }
        public uint Name { get; init; }
        public long DataStart { get; init; }
        public uint Crc { get; init; }

        public static PngChunk ReadFromStream(MemoryStream stream) {
            const int nonDataFieldLength = 4;
            const int lengthFieldOffset = 0;
            const int nameFieldOffset = 4;
            const int dataFieldOffset = 8;
            
            var span = stream.GetBuffer().AsSpan();
            var position = (int)stream.Position;

            var lengthSpan = span.Slice(position + lengthFieldOffset, nonDataFieldLength);
            var length = BinaryPrimitives.ReadUInt32BigEndian(lengthSpan);
            var nameSpan = span.Slice(position + nameFieldOffset, nonDataFieldLength);
            var name = BinaryPrimitives.ReadUInt32BigEndian(nameSpan);
            var crcSpan = span.Slice(position + dataFieldOffset + (int)length, nonDataFieldLength);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(crcSpan);

            stream.Position += nonDataFieldLength * 3 + length;

            var chunk = new PngChunk() {
                Length = length,
                Name = name,
                DataStart = position + dataFieldOffset,
                Crc = crc
            };
            return chunk;
        }
    }
    
    private class InvalidPngException(string message) : Exception(message);
}

