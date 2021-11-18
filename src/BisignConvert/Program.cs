using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BisignConvert
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = Assembly.GetExecutingAssembly().FullName;
            Console.CursorVisible = false;

            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine($"{versionInfo.ProductName} v{Assembly.GetExecutingAssembly().GetName().Version}\n" +
                              $"{versionInfo.LegalCopyright}\n");

            Console.ResetColor();

            var unattented = (args.Length > 0 && args[args.Length - 1] == "/s");
            var files = args.Length > 0 ? args : EnumerateFiles(Environment.CurrentDirectory, "*.bisign", SearchOption.AllDirectories);

            if (files.Any())
            {
                ProcessFiles(files);
                Console.WriteLine("Done!");

                if (!unattented) Thread.Sleep(-1);
                return 0;
            }

            Console.WriteLine("Extracts bikey from bisign files for DayZ and ArmA titles, works with both v2 and v3 signatures.\n\n" +
                              "Example Usage:\n" +
                              $"  {Assembly.GetExecutingAssembly().GetName().Name}.exe Scripts.pbo.Wardog.v3.bisign\n");
            
            if (!unattented) Thread.Sleep(-1);
            return 1;
        }

        private static void ProcessFiles(IEnumerable<string> paths)
        {
            var keys = new List<byte[]>();

            foreach (var path in paths)
            {
                if (path == "/s") continue;

                if (Directory.Exists(path))
                {

                    var content = EnumerateFiles(path, "*.bisign", SearchOption.AllDirectories);
                    ProcessFiles(content);

                    continue;
                }

                var fileName = Path.GetFileName(path);

                if (!File.Exists(path))
                {
                    Console.WriteLine($"File '{fileName}' does not exist");
                    continue;
                }

                if (!fileName.EndsWith(".bisign"))
                {
                    Console.WriteLine($"File '{fileName}' is not a .bisign");
                    continue;
                }

                using (var reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    var buffer = new List<byte>();
                    while (reader.PeekChar() != 0)
                    {
                        var num = reader.ReadByte();
                        buffer.Add(num);
                    }

                    reader.ReadByte();
                    var authority = Encoding.UTF8.GetString(buffer.ToArray());

                    var temp = reader.ReadUInt32();

                    // who knows what is stored here? i assume RSA1 version data
                    var garbage1 = reader.ReadUInt32();
                    var garbage2 = reader.ReadUInt32();
                    var garbage3 = reader.ReadUInt32();

                    var length = reader.ReadUInt32();
                    var exponent = reader.ReadUInt32();

                    if (temp != length / 8 + 20)
                    {
                        Console.WriteLine($"Improper signature '{fileName}' skipping...");
                        continue;
                    }

                    var key = reader.ReadBytes((int)length / 8); // end of bikey

                    if (keys.Contains(key)) // duplicate
                        continue;
                    
                    keys.Add(key);

                    var block = reader.ReadUInt32();
                    reader.ReadBytes((int)block);

                    var version = reader.ReadUInt32();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{authority}.bikey, Version={version}");
                    Console.ResetColor();

                    var keyString = BitConverter.ToString(key)
                        .Replace("-", ":");

                    Console.WriteLine($"{keyString}\n");
                    
                    Directory.CreateDirectory("Keys");
                    var keyFile = Path.Combine(Environment.CurrentDirectory, "Keys", $"{authority}.bikey");

                    using (var writer = new BinaryWriter(File.OpenWrite(keyFile)))
                    {
                        writer.Write(buffer.ToArray());
                        writer.Write((byte)0);

                        writer.Write(temp);

                        writer.Write(garbage1);
                        writer.Write(garbage2);
                        writer.Write(garbage3);

                        writer.Write(length);
                        writer.Write(exponent);

                        writer.Write(key);
                    }
                }
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt)
        {
            try
            {
                var files = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    files = Directory.EnumerateDirectories(path)
                        .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
                }

                return files.Concat(Directory.EnumerateFiles(path, searchPattern));
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex.Message);
                return Enumerable.Empty<string>();
            }
        }
    }
}