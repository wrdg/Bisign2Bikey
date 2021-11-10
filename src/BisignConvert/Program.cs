using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BisignConvert
{
    internal class Program
    {
        private static readonly byte[] SignaturePattern = { 0x80, 0x00, 0x00, 0x00 };
        private static readonly IList<string> Keys = new List<string>();

        internal static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = Assembly.GetExecutingAssembly().FullName;
            Console.CursorVisible = false;

            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine($"{versionInfo.ProductName}\n{versionInfo.LegalCopyright}\n");
            
            Console.ResetColor();

            var files = args.Length > 0 ? args : Directory.GetFiles(Environment.CurrentDirectory, "*.bisign");

            if (files.Length > 0)
            {
                ProcessFiles(files);

                Console.WriteLine("Done!");
                Console.ReadKey();
            }

            Console.WriteLine("No bisign files found.");
            Console.ReadKey();
        }

        private static void ProcessFiles(string[] paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    var addons = Path.Combine(path, "addons");
                    var content = Directory.GetFiles(Directory.Exists(addons) ? addons : path, "*.bisign");

                    ProcessFiles(content);
                    continue;
                }

                if (!File.Exists(path)) continue;

                var fileName = Path.GetFileName(path);

                if (!fileName.Contains(".bisign")) continue;

                var keyName = fileName
                    .Split(new[] { ".pbo." }, StringSplitOptions.None).Last()
                    .Replace("bisign", "bikey");

                if (Keys.Contains(keyName)) continue;

                var key = GetPublicKey(path);
                if (key == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(keyName);

                Console.ResetColor();
                Console.WriteLine($"{ByteArrayString(key)}\n");

                var dir = Directory.CreateDirectory("Keys");
                File.WriteAllBytes(Path.Combine(dir.FullName, keyName), key);

                Keys.Add(keyName);
            }
        }

        private static byte[] GetPublicKey(string path)
        {
            if (!File.Exists(path)) return null;

            var stream = new FileStream(path, FileMode.Open);
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                var sigBytes = memory.ToArray();
                stream.Dispose();

                var position = FindFirstBytePattern(sigBytes, SignaturePattern);
                return position < 1 ? null : sigBytes.Take(position).ToArray();
            }
        }

        private static string ByteArrayString(byte[] array)
        {
            return BitConverter.ToString(array)
                .Replace("-", ":");
        }

        private static int FindFirstBytePattern(IReadOnlyList<byte> src, IReadOnlyList<byte> pattern)
        {
            var first = src.Count - pattern.Count + 1;

            for (var i = 0; i < first; i++)
            {
                if (src[i] != pattern[0])
                    continue;

                for (var j = pattern.Count - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }

            return -1;
        }
    }
}
