#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace M3P.Editor
{
    /// <summary>
    /// itch.io HTML5 rejects any single extracted file over 200 MB.
    /// After each WebGL build, split an oversized data file and patch index.html
    /// so the next zip is uploadable without a manual workaround.
    /// </summary>
    public sealed class WebGLItchPostBuild : IPostprocessBuildWithReport
    {
        public const long ItchMaxFileBytes = 200L * 1024 * 1024;
        const long PartBudgetBytes = 190L * 1024 * 1024;

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            try
            {
                ProcessBuildFolder(report.summary.outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MenuItem("M3P/WebGL/Prepare build folder for itch.io")]
        public static void PrepareFolderMenu()
        {
            string path = EditorUtility.OpenFolderPanel(
                "WebGL build folder (contains index.html)",
                "",
                "");
            if (!string.IsNullOrEmpty(path))
                ProcessBuildFolder(path);
        }

        public static void ProcessBuildFolder(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
            {
                Debug.LogError($"{nameof(WebGLItchPostBuild)}: build folder not found: {outputPath}");
                return;
            }

            string indexPath = Path.Combine(outputPath, "index.html");
            string buildDir = Path.Combine(outputPath, "Build");
            if (!File.Exists(indexPath) || !Directory.Exists(buildDir))
            {
                Debug.LogError(
                    $"{nameof(WebGLItchPostBuild)}: expected index.html and Build/ under {outputPath}");
                return;
            }

            string compressed = FindDataFile(buildDir);
            if (compressed == null)
            {
                bool alreadySplit = Directory.GetFiles(buildDir, "*.data.part*").Length > 0;
                Debug.Log(
                    alreadySplit
                        ? $"{nameof(WebGLItchPostBuild)}: folder already split for itch.io."
                        : $"{nameof(WebGLItchPostBuild)}: no Unity data file in {buildDir}");
                return;
            }

            string dataStem = StripCompressionSuffix(compressed);
            DeleteExistingParts(dataStem);

            long packedSize = new FileInfo(compressed).Length;
            if (packedSize < ItchMaxFileBytes)
            {
                Debug.Log(
                    $"{nameof(WebGLItchPostBuild)}: {Path.GetFileName(compressed)} is " +
                    $"{packedSize / (1024f * 1024f):F2} MB — under itch 200 MB, leaving as-is.");
                return;
            }

            string unpacked = UnpackIfNeeded(compressed, dataStem);
            try
            {
                long unpackedSize = new FileInfo(unpacked).Length;
                int partCount = (int)((unpackedSize + PartBudgetBytes - 1) / PartBudgetBytes);
                if (partCount < 2)
                    partCount = 2;

                WriteParts(unpacked, dataStem, partCount);
                PatchIndexHtml(indexPath, Path.GetFileName(dataStem), partCount);

                if (File.Exists(compressed))
                    File.Delete(compressed);

                Debug.Log(
                    $"{nameof(WebGLItchPostBuild)}: split {Path.GetFileName(compressed)} " +
                    $"({packedSize / (1024f * 1024f):F2} MB packed, {unpackedSize / (1024f * 1024f):F2} MB raw) " +
                    $"into {partCount} parts for itch.io.");
            }
            finally
            {
                if (unpacked.EndsWith(".unpacked", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(unpacked))
                    File.Delete(unpacked);
            }
        }

        static string FindDataFile(string buildDir)
        {
            return FirstDataFile(buildDir, ".data.br")
                   ?? FirstDataFile(buildDir, ".data.gz")
                   ?? FirstDataFile(buildDir, ".data");
        }

        static string FirstDataFile(string buildDir, string suffix)
        {
            foreach (string file in Directory.GetFiles(buildDir))
            {
                string name = Path.GetFileName(file);
                if (name.Contains(".part") || name.EndsWith(".unpacked", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return file;
            }

            return null;
        }

        static string StripCompressionSuffix(string path)
        {
            if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                return path.Substring(0, path.Length - 3);
            return path;
        }

        static void DeleteExistingParts(string dataStem)
        {
            string dir = Path.GetDirectoryName(dataStem);
            string prefix = Path.GetFileName(dataStem) + ".part";
            foreach (string file in Directory.GetFiles(dir))
            {
                if (Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    File.Delete(file);
            }
        }

        static string UnpackIfNeeded(string compressed, string dataStem)
        {
            if (compressed.EndsWith(".data", StringComparison.OrdinalIgnoreCase))
                return compressed;

            string temp = dataStem + ".unpacked";
            using (FileStream input = File.OpenRead(compressed))
            using (FileStream output = File.Create(temp))
            using (Stream decoder = compressed.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                       ? (Stream)new GZipStream(input, CompressionMode.Decompress)
                       : new BrotliStream(input, CompressionMode.Decompress))
            {
                decoder.CopyTo(output);
            }

            return temp;
        }

        static void WriteParts(string unpacked, string dataStem, int partCount)
        {
            long total = new FileInfo(unpacked).Length;
            long partSize = (total + partCount - 1) / partCount;
            using (FileStream src = File.OpenRead(unpacked))
            {
                for (int i = 0; i < partCount; i++)
                {
                    long remaining = total - src.Position;
                    long chunk = Math.Min(partSize, remaining);
                    string partPath = $"{dataStem}.part{i}";
                    using (FileStream dst = File.Create(partPath))
                        CopyBytes(src, dst, chunk);
                    if (new FileInfo(partPath).Length >= ItchMaxFileBytes)
                        throw new InvalidOperationException(
                            $"{partPath} is still over itch.io's 200 MB file limit.");
                }
            }
        }

        static void CopyBytes(Stream src, Stream dst, long count)
        {
            byte[] buffer = new byte[1024 * 1024];
            long left = count;
            while (left > 0)
            {
                int read = src.Read(buffer, 0, (int)Math.Min(buffer.Length, left));
                if (read <= 0)
                    break;
                dst.Write(buffer, 0, read);
                left -= read;
            }
        }

        static void PatchIndexHtml(string indexPath, string dataFileName, int partCount)
        {
            string html = File.ReadAllText(indexPath, Encoding.UTF8);
            html = Regex.Replace(
                html,
                @"dataUrl:\s*buildUrl\s*\+\s*""[^""]+""",
                $"dataUrl: buildUrl + \"/{dataFileName}\"");

            const string marker = "var script = document.createElement(\"script\");";
            string hook = BuildFetchHook(dataFileName, partCount);
            if (!html.Contains("itchDataPartCount"))
            {
                if (!html.Contains(marker))
                    throw new InvalidOperationException("index.html loader hook point not found.");
                html = html.Replace(marker, hook + marker);
            }

            File.WriteAllText(indexPath, html, new UTF8Encoding(false));
        }

        static string BuildFetchHook(string dataFileName, int partCount)
        {
            return
                "      // itch.io: stitch data parts so no extracted file exceeds 200 MB.\n" +
                "      (function () {\n" +
                $"        var itchDataFile = \"{dataFileName}\";\n" +
                $"        var itchDataPartCount = {partCount};\n" +
                "        var origFetch = window.fetch.bind(window);\n" +
                "        window.fetch = function (input, init) {\n" +
                "          var url = typeof input === \"string\" ? input : (input && input.url) || \"\";\n" +
                "          if (url.indexOf(itchDataFile) !== -1 && url.indexOf(\".part\") === -1) {\n" +
                "            var base = url.replace(/\\.br$|\\.gz$/i, \"\");\n" +
                "            var reqs = [];\n" +
                "            for (var i = 0; i < itchDataPartCount; i++)\n" +
                "              reqs.push(origFetch(base + \".part\" + i, init));\n" +
                "            return Promise.all(reqs).then(function (resps) {\n" +
                "              return Promise.all(resps.map(function (r) {\n" +
                "                if (!r.ok) throw new Error(\"Failed loading \" + r.url + \" (\" + r.status + \")\");\n" +
                "                return r.arrayBuffer();\n" +
                "              })).then(function (bufs) {\n" +
                "                var total = 0;\n" +
                "                for (var b = 0; b < bufs.length; b++) total += bufs[b].byteLength;\n" +
                "                return new Response(new Blob(bufs), {\n" +
                "                  status: 200,\n" +
                "                  headers: { \"Content-Type\": \"application/octet-stream\", \"Content-Length\": String(total) }\n" +
                "                });\n" +
                "              });\n" +
                "            });\n" +
                "          }\n" +
                "          return origFetch(input, init);\n" +
                "        };\n" +
                "      })();\n\n";
        }
    }
}
#endif
