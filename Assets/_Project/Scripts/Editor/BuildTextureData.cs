#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace M3P.Editor
{
    [Serializable]
    internal sealed class BuildTextureEntry
    {
        public string name, assetPath, format, type, package, sourceKind, dimensionError;
        public long packedBytes, pathId, offset;
        public int width = -1, height = -1, mipCount, faces = 1;
    }

    [Serializable]
    internal sealed class BuildTextureSnapshot
    {
        public List<BuildTextureEntry> entries = new List<BuildTextureEntry>();
        public string buildTime, reportPath, dataPath, error;
        public long totalBytes;
    }

    /// <summary>
    /// Joins BuildReport contributions to texture headers in the WebGL player data.
    /// Does not load source Texture objects: importer settings can change after a build.
    /// </summary>
    internal static class BuildTextureData
    {
        internal static BuildTextureSnapshot LoadLatest(Action<float, string> progress = null)
        {
            var snapshot = new BuildTextureSnapshot();
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                snapshot.reportPath = Path.Combine(root, "Library/LastBuild.buildreport");
                snapshot.dataPath = Path.Combine(root, "Library/PlayerDataCache/WebGL/Data/data.unity3d");
                BuildReport report = BuildReport.GetLatestReport();
                if (report == null)
                    throw new InvalidOperationException("Brak raportu buildu. Najpierw wykonaj build WebGL.");
                if (report.summary.platform != BuildTarget.WebGL)
                    throw new InvalidOperationException("Ostatni raport dotyczy " + report.summary.platform + ". Najpierw wykonaj build WebGL.");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Ostatni build nie zakończył się sukcesem. Wykonaj poprawny build WebGL.");

                snapshot.buildTime = report.summary.buildEndedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                foreach (PackedAssets package in report.packedAssets)
                {
                    foreach (PackedAssetInfo info in package.contents)
                    {
                        if (info.type == null || !typeof(Texture).IsAssignableFrom(info.type)) continue;
                        string assetPath = info.sourceAssetPath ?? string.Empty;
                        var entry = new BuildTextureEntry
                        {
                            name = Path.GetFileNameWithoutExtension(assetPath),
                            assetPath = assetPath,
                            packedBytes = checked((long)info.packedSize),
                            offset = checked((long)info.offset),
                            pathId = info.id,
                            type = info.type.Name,
                            package = package.shortPath,
                            sourceKind = assetPath.StartsWith("Assets/", StringComparison.Ordinal) ? "Assets" :
                                assetPath.StartsWith("Packages/", StringComparison.Ordinal) ? "Package" : "Built-in",
                            faces = info.type == typeof(Cubemap) ? 6 : 1
                        };
                        snapshot.entries.Add(entry);
                        snapshot.totalBytes += entry.packedBytes;
                    }
                }

                if (snapshot.entries.Count == 0)
                    throw new InvalidOperationException("Raport nie zawiera danych tekstur. Wykonaj pełny build z raportem assetów.");
                if (!File.Exists(snapshot.dataPath))
                    throw new FileNotFoundException("Brak danych WebGL w Library. Rozmiary pochodzą z raportu; rozdzielczości wymagają ponownego buildu.", snapshot.dataPath);
                // A later build can replace the player cache while an older report remains.
                if (File.GetLastWriteTimeUtc(snapshot.dataPath) > report.summary.buildEndedAt.ToUniversalTime().AddSeconds(2))
                    throw new InvalidOperationException("Dane WebGL są nowsze niż raport. Wykonaj nowy build, aby dopasować rozdzielczości.");

                ReadBuiltDimensions(snapshot.dataPath, snapshot.entries, progress);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                snapshot.error = ex.Message;
                foreach (BuildTextureEntry entry in snapshot.entries)
                {
                    if (entry.width >= 0 && entry.height >= 0) continue;
                    if (string.IsNullOrEmpty(entry.dimensionError)) entry.dimensionError = ex.Message;
                    if (string.IsNullOrEmpty(entry.format)) entry.format = "brak danych";
                }
            }
            snapshot.entries.Sort((a, b) => b.packedBytes.CompareTo(a.packedBytes));
            return snapshot;
        }

        /// <summary>Pure file parser, separately testable without a running Editor.</summary>
        internal static void ReadBuiltDimensions(string path, List<BuildTextureEntry> entries, Action<float, string> progress = null)
        {
            using (var bundle = new BundleBlocks(path))
            {
                var metadata = new Dictionary<string, SerializedMetadata>(StringComparer.Ordinal);
                foreach (string name in entries.Select(e => e.package).Distinct())
                {
                    Node node;
                    if (!bundle.nodes.TryGetValue(name, out node))
                        throw new InvalidDataException("Brak pliku w danych buildu: " + name);
                    metadata.Add(name, ReadMetadata(bundle, node));
                }

                // Validate every object before writing dimensions. A mismatched player cache
                // must not create a mixture of old and new build measurements.
                foreach (BuildTextureEntry entry in entries)
                {
                    SerializedMetadata file = metadata[entry.package];
                    ObjectLocation obj;
                    if (!file.objects.TryGetValue(entry.pathId, out obj) || obj.offset != entry.offset || obj.size != entry.packedBytes)
                        throw new InvalidDataException("Dane WebGL nie odpowiadają raportowi (" + entry.package + ", ID " + entry.pathId + "). Wykonaj ponowny build.");
                    if (entry.type == "Texture2D" && obj.classId != 28 || entry.type == "Cubemap" && obj.classId != 89)
                        throw new InvalidDataException("Typ obiektu w danych WebGL nie odpowiada raportowi.");
                }

                var measured = new List<BuildTextureEntry>();
                for (int index = 0; index < entries.Count; index++)
                {
                    BuildTextureEntry entry = entries[index];
                    progress?.Invoke((float)index / entries.Count, entry.assetPath);
                    if (entry.type != "Texture2D" && entry.type != "Cubemap")
                    {
                        entry.dimensionError = "Odczyt wymiarów tego typu tekstury nie jest obsługiwany.";
                        continue;
                    }
                    SerializedMetadata file = metadata[entry.package];
                    bundle.Position = file.node.offset + entry.offset;
                    var reader = new DataReader(bundle, file.littleEndian, file.node.offset);
                    long end = bundle.Position + entry.packedBytes;
                    var value = new BuildTextureEntry();
                    value.name = reader.AlignedString(16384);
                    reader.Byte(); // m_IsAlphaChannelOptional (Unity 6000.3 player layout)
                    reader.Align(4);
                    value.width = reader.Int32();
                    value.height = reader.Int32();
                    int imageSize = reader.Int32();
                    int stripped = reader.Int32();
                    int format = reader.Int32();
                    value.mipCount = reader.Int32();
                    if (bundle.Position > end || value.width < 0 || value.width > 65536 || value.height < 0 || value.height > 65536 ||
                        value.mipCount < 0 || value.mipCount > 32 || imageSize < 0 || stripped < 0 || stripped > 32 || !Enum.IsDefined(typeof(TextureFormat), format))
                        throw new InvalidDataException("Nieobsługiwany nagłówek tekstury: " + entry.assetPath);
                    value.format = ((TextureFormat)format).ToString();
                    measured.Add(value);
                }
                int measuredIndex = 0;
                foreach (BuildTextureEntry entry in entries)
                {
                    if (entry.dimensionError != null) continue;
                    BuildTextureEntry value = measured[measuredIndex++];
                    entry.name = value.name;
                    entry.width = value.width;
                    entry.height = value.height;
                    entry.format = value.format;
                    entry.mipCount = value.mipCount;
                }
                progress?.Invoke(1f, "Gotowe");
            }
        }

        private sealed class SerializedMetadata
        {
            internal Node node;
            internal bool littleEndian;
            internal Dictionary<long, ObjectLocation> objects = new Dictionary<long, ObjectLocation>();
        }

        private struct ObjectLocation { internal long offset, size; internal int classId; }
        private struct Node { internal long offset, size; }

        private static SerializedMetadata ReadMetadata(BundleBlocks stream, Node node)
        {
            stream.Position = node.offset;
            var reader = new DataReader(stream, false, node.offset);
            reader.UInt32(); reader.UInt32();
            uint version = reader.UInt32();
            reader.UInt32();
            if (version != 22)
                throw new NotSupportedException("Nieobsługiwana wersja pliku Unity: " + version + ". Rozmiary raportu pozostają dostępne.");
            bool little = reader.Byte() == 0;
            reader.Skip(3);
            uint metadataSize = reader.UInt32();
            long fileSize = reader.Int64();
            long dataOffset = reader.Int64();
            reader.Int64();
            if (fileSize != node.size || dataOffset < 48 || dataOffset > fileSize || metadataSize > dataOffset)
                throw new InvalidDataException("Niepoprawny nagłówek pliku danych Unity.");
            reader.little = little;
            string unityVersion = reader.ZeroString(128);
            if (!unityVersion.StartsWith("6000.3.", StringComparison.Ordinal))
                throw new NotSupportedException("Odczyt wymiarów zweryfikowano dla Unity 6000.3. Build używa " + unityVersion + ".");
            reader.Int32(); // Build target.
            bool typeTree = reader.Byte() != 0;
            int count = reader.Count(100000);
            var classes = new int[count];
            for (int i = 0; i < count; i++)
            {
                int classId = reader.Int32(); classes[i] = classId;
                reader.Byte(); reader.Int16();
                if (classId == 114) reader.Skip(16);
                reader.Skip(16);
                if (typeTree)
                {
                    int nodes = reader.Count(1000000);
                    int strings = reader.Count(100000000);
                    reader.Skip(checked(nodes * 32L + strings));
                    reader.Skip(reader.Count(1000000) * 4L);
                }
            }
            int objects = reader.Count(10000000);
            var result = new SerializedMetadata { node = node, littleEndian = little };
            for (int i = 0; i < objects; i++)
            {
                reader.Align(4);
                long id = reader.Int64();
                long offset = checked(reader.Int64() + dataOffset);
                uint size = reader.UInt32();
                int typeIndex = reader.Int32();
                if (typeIndex < 0 || typeIndex >= classes.Length || offset < dataOffset || offset > fileSize - size)
                    throw new InvalidDataException("Niepoprawna tabela obiektów Unity.");
                result.objects.Add(id, new ObjectLocation { offset = offset, size = size, classId = classes[typeIndex] });
            }
            if (stream.Position > node.offset + dataOffset)
                throw new InvalidDataException("Tabela obiektów wykracza poza metadane.");
            return result;
        }

        /// <summary>Seekable view over UnityFS blocks; keeps only the currently read block in memory.</summary>
        private sealed class BundleBlocks : Stream
        {
            private readonly FileStream file;
            private readonly List<Block> blocks = new List<Block>();
            internal readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
            private long length, position;
            private int loaded = -1;
            private byte[] buffer;
            private struct Block { internal long fileOffset, offset; internal int compressed, size, flags; }

            internal BundleBlocks(string path)
            {
                file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    var reader = new DataReader(file, false);
                    if (reader.ZeroString(32) != "UnityFS") throw new NotSupportedException("Dane buildu nie są archiwum UnityFS.");
                    uint version = reader.UInt32();
                    reader.ZeroString(128); reader.ZeroString(128);
                    long archiveSize = reader.Int64();
                    int compressedInfo = reader.Count(128 * 1024 * 1024);
                    int infoSize = reader.Count(128 * 1024 * 1024);
                    int flags = reader.Int32();
                    if (archiveSize != file.Length || version < 7 || version > 8)
                        throw new NotSupportedException("Nieobsługiwany nagłówek archiwum UnityFS.");
                    reader.Align(16);
                    long start = file.Position;
                    if ((flags & 128) != 0) file.Position = file.Length - compressedInfo;
                    byte[] info = Decompress(reader.Bytes(compressedInfo), infoSize, flags & 63);
                    if ((flags & 128) != 0) file.Position = start;
                    if ((flags & 512) != 0) reader.Align(16);
                    long compressedOffset = file.Position;
                    using (var infoStream = new MemoryStream(info, false))
                    {
                        var index = new DataReader(infoStream, false);
                        index.Skip(16);
                        int blockCount = index.Count(1000000);
                        for (int i = 0; i < blockCount; i++)
                        {
                            var block = new Block { size = index.Count(256 * 1024 * 1024), compressed = index.Count(256 * 1024 * 1024), flags = index.UInt16(), offset = length, fileOffset = compressedOffset };
                            if (block.size == 0 || block.compressed == 0) throw new InvalidDataException("Pusty blok UnityFS.");
                            blocks.Add(block);
                            length = checked(length + block.size);
                            compressedOffset = checked(compressedOffset + block.compressed);
                        }
                        if (compressedOffset > file.Length) throw new InvalidDataException("Bloki UnityFS wykraczają poza plik.");
                        int nodeCount = index.Count(1000000);
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var node = new Node { offset = index.Int64(), size = index.Int64() };
                            index.Int32();
                            string name = index.ZeroString(32768);
                            if (node.offset < 0 || node.size < 0 || node.offset > length - node.size) throw new InvalidDataException("Niepoprawny wpis UnityFS.");
                            nodes.Add(name, node);
                        }
                    }
                }
                catch { file.Dispose(); throw; }
            }

            public override int Read(byte[] target, int offset, int count)
            {
                if (target == null || offset < 0 || count < 0 || offset > target.Length - count) throw new ArgumentOutOfRangeException();
                int total = 0;
                while (count > 0 && position < length)
                {
                    int low = 0, high = blocks.Count - 1;
                    while (low < high)
                    {
                        int mid = (low + high + 1) / 2;
                        if (blocks[mid].offset <= position) low = mid; else high = mid - 1;
                    }
                    Block block = blocks[low];
                    if (loaded != low)
                    {
                        file.Position = block.fileOffset;
                        buffer = Decompress(new DataReader(file, false).Bytes(block.compressed), block.size, block.flags & 63);
                        loaded = low;
                    }
                    int local = checked((int)(position - block.offset));
                    int copied = Math.Min(count, block.size - local);
                    Buffer.BlockCopy(buffer, local, target, offset, copied);
                    position += copied; offset += copied; count -= copied; total += copied;
                }
                return total;
            }

            public override long Position { get => position; set { if (value < 0 || value > length) throw new ArgumentOutOfRangeException(nameof(value)); position = value; } }
            public override long Length => length;
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Seek(long offset, SeekOrigin origin) { Position = checked((origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? position : length) + offset); return position; }
            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] bytes, int offset, int count) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { if (disposing) file.Dispose(); base.Dispose(disposing); }
        }

        // UnityFS uses ordinary raw LZ4 blocks (compression kinds 2 and 3).
        private static byte[] Decompress(byte[] source, int size, int kind)
        {
            if (kind == 0)
            {
                if (source.Length != size) throw new InvalidDataException("Niepoprawny rozmiar bloku UnityFS.");
                return source;
            }
            if (kind != 2 && kind != 3) throw new NotSupportedException("Nieobsługiwana kompresja UnityFS: " + kind + ". Użyj LZ4/LZ4HC.");
            byte[] output = new byte[size];
            int input = 0, written = 0;
            while (input < source.Length)
            {
                int token = source[input++];
                int literal = token >> 4;
                if (literal == 15) literal = checked(literal + ExtendedLength(source, ref input));
                if (literal > source.Length - input || literal > size - written) throw new InvalidDataException("Uszkodzony blok LZ4.");
                Buffer.BlockCopy(source, input, output, written, literal);
                input += literal; written += literal;
                if (input == source.Length) break;
                if (input > source.Length - 2) throw new InvalidDataException("Uszkodzony offset LZ4.");
                int distance = source[input] | source[input + 1] << 8; input += 2;
                int match = token & 15;
                if (match == 15) match = checked(match + ExtendedLength(source, ref input));
                match = checked(match + 4);
                if (distance <= 0 || distance > written || match > size - written) throw new InvalidDataException("Uszkodzone dopasowanie LZ4.");
                for (int i = 0; i < match; i++) { output[written] = output[written - distance]; written++; }
            }
            if (written != size) throw new InvalidDataException("Niepoprawny wynik dekompresji LZ4.");
            return output;
        }

        private static int ExtendedLength(byte[] input, ref int index)
        {
            int length = 0, next;
            do
            {
                if (index >= input.Length) throw new InvalidDataException("Niepełny blok LZ4.");
                next = input[index++]; length = checked(length + next);
            } while (next == 255);
            return length;
        }

        private sealed class DataReader
        {
            private readonly Stream stream;
            private readonly long origin;
            internal bool little;
            internal DataReader(Stream stream, bool little, long origin = 0) { this.stream = stream; this.little = little; this.origin = origin; }
            internal byte Byte() { int b = stream.ReadByte(); if (b < 0) throw new EndOfStreamException(); return (byte)b; }
            internal byte[] Bytes(int count)
            {
                byte[] bytes = new byte[count]; int read = 0;
                while (read < count) { int n = stream.Read(bytes, read, count - read); if (n == 0) throw new EndOfStreamException(); read += n; }
                return bytes;
            }
            internal ushort UInt16() { int a = Byte(), b = Byte(); return (ushort)(little ? a | b << 8 : a << 8 | b); }
            internal short Int16() => unchecked((short)UInt16());
            internal uint UInt32()
            {
                uint a = UInt16(), b = UInt16(); return little ? a | b << 16 : a << 16 | b;
            }
            internal int Int32() => unchecked((int)UInt32());
            internal long Int64()
            {
                ulong a = UInt32(), b = UInt32(); return unchecked((long)(little ? a | b << 32 : a << 32 | b));
            }
            internal int Count(int max) { int count = Int32(); if (count < 0 || count > max) throw new InvalidDataException("Niepoprawny rozmiar danych Unity."); return count; }
            internal void Skip(long count) { if (count < 0 || count > stream.Length - stream.Position) throw new EndOfStreamException(); stream.Position += count; }
            internal void Align(int alignment) { long relative = stream.Position - origin; Skip((alignment - relative % alignment) % alignment); }
            internal string ZeroString(int max)
            {
                var bytes = new List<byte>();
                for (int i = 0; i < max; i++) { byte b = Byte(); if (b == 0) return Encoding.UTF8.GetString(bytes.ToArray()); bytes.Add(b); }
                throw new InvalidDataException("Niepoprawna nazwa w pliku Unity.");
            }
            internal string AlignedString(int max) { string value = Encoding.UTF8.GetString(Bytes(Count(max))); Align(4); return value; }
        }
    }
}
#endif
