using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GovanifY.Utility;
using RECOM_Toolkit;
using KHCompress;

/* RCM Patch File Format
 * 0    UInt32  Magic 0x5032484B "RCMP"
 * 4    UInt32  16 + Author Length
 * 8    UInt32  16 + Author Length + 16 + Changelog Length + 4 + Credits Length + Other Info Length
 * 12   UInt32  Version number of the patch
 * 13   string  Author
 * ?    UInt32  12
 * ?    UInt32  16 + Changelog Length
 * ?    UInt32  16 + Changelog Length + 4 + Credits Length
 * ?    UInt32  Number of lines of the changelog 
 *   
 *      for each changelog lines:
 *          UInt32  Changelog count * 4
 *          Increase i by the length of the next line
 *
 *      then:
 *          string Changelog line
 *          
 * ?    UInt32  Number of lines of the credits
 * 
 *      for each credit lines:
 *          UInt32  Credits count * 4
 *          Increase i by the length of the next line
 *      then:
 *          string Changelog line
 *          
 * ?    string  Other infos
 * ?    UInt32  Number of files
 *       for each file:
 *          UInt32  Number of parent files
 *             for each parent:
 *                UInt32 Size of the Parent filename
 *                string Parent filename
 *          UInt32  Size of the filename
 *          string  filename
 *          bool    Is Compressed flag
 *          UInt32  5x 0(Reserved!)
 *          long  Length of file uncompressed
 *          long  Length of the file compressed(same than before if not compressed)
 *          byte*?  Raw file data
 * 
 */
namespace RECOM_PatchMaker
{
    internal class PatchFile
    {
        /// <summary>Signature "RCMP" of the PATCH file format.</summary>
        public const uint Signature = 0x504D4352;
        /// <summary>List of byte arrays of the changelog string encoded in an ASCII byte array.</summary>
        private readonly List<byte[]> Changelogs = new List<byte[]>();
        /// <summary>List of byte arrays of the credits string encoded in an ASCII byte array.</summary>
        public List<byte[]> Credits = new List<byte[]>();
        /// <summary>List of FileEntries of the files that will be added into the patch.</summary>
        public List<FileEntry> Files = new List<FileEntry>();
        /// <summary>uint of the version of the patch. As an uint it supports 0 to 4294967295 plain numbers.</summary>
        public uint Version = 1;
        /// <summary>Byte array of the Author string encoded in an ASCII byte array.</summary>
        private byte[] _Author = {0};
        /// <summary>Byte array of the Other info string encoded in an ASCII byte array.</summary>
        private byte[] _OtherInfo = {0};
        /// <summary>Sets the <c>_Author</c> byte array and returns the string of it.</summary>
        public string Author
        {
            get { return Encoding.ASCII.GetString(_Author); }
            set { _Author = Encoding.ASCII.GetBytes(value + '\0'); }
        }
        /// <summary>Sets the <c>_OtherInfo</c> byte array and returns the string of it.</summary>
        public string OtherInfo
        {
            get { return Encoding.ASCII.GetString(_OtherInfo); }
            set
            {
                value = value.Replace("\\n", "\r\n");
                _OtherInfo = Encoding.ASCII.GetBytes(value + '\0');
            }
        }
        /// <summary>Adds a value to the <c>Changelogs</c> list.</summary>
        public void AddChange(string s)
        {
            s = s.Replace("\\n", "\r\n");
            Changelogs.Add(Encoding.ASCII.GetBytes(s + '\0'));
        }
        /// <summary>Adds a value to the <c>Credits</c> list.</summary>
        public void AddCredit(string s)
        {
            s = s.Replace("\\n", "\r\n");
            Credits.Add(Encoding.ASCII.GetBytes(s + '\0'));
        }
        /// <summary>
        /// Converts the input string into a char byte array.
        /// </summary>
        /// <param name="str">The inputted string.</param>
        /// <returns>The char byte array.</returns>
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        /// <summary>
        /// Writes the decrypted patch into the inputted stream.
        /// </summary>
        /// <param name="stream">Stream in which the patch needs to be written.</param>
        public void WriteDecrypted(Stream stream)
        {

            stream.Position = 0;
            uint changeLen = 0, creditLen = 0;
            changeLen = Changelogs.Aggregate(changeLen, (current, b) => current + (4 + (uint) b.Length));
            creditLen = Credits.Aggregate(creditLen, (current, b) => current + (4 + (uint) b.Length));
            using (var bw = new BinaryStream(stream, leaveOpen: true))
            {
                uint i;
                bw.Write(Signature);
                bw.Write((uint) (16 + _Author.Length));
                bw.Write((uint) (16 + _Author.Length + 16 + changeLen + 4 + creditLen + _OtherInfo.Length));
                bw.Write(Version);
                bw.Write(_Author);
                bw.Write((uint) 12);
                bw.Write(16 + changeLen);
                bw.Write(16 + changeLen + 4 + creditLen);
                bw.Write(i = (uint) Changelogs.Count);
                i *= 4;
                foreach (var b in Changelogs)
                {
                    bw.Write(i);
                    i += (uint) b.Length;
                }
                foreach (var b in Changelogs)
                {
                    bw.Write(b);
                }
                bw.Write(i = (uint) Credits.Count);
                i *= 4;
                foreach (var b in Credits)
                {
                    bw.Write(i);
                    i += (uint) b.Length;
                }
                foreach (var b in Credits)
                {
                    bw.Write(b);
                }
                bw.Write(_OtherInfo);
                bw.Write((uint) Files.Count);
                    foreach (FileEntry file in Files)
                    {
                           if (file.Compressed)
                            {
                                Console.WriteLine("Compressing {0}", file.name);
                            }
                            else
                            {
                                Console.WriteLine("Adding {0}", file.name);
                            }
                           bw.Write((UInt32)file.NumberParent);
                           if(file.NumberParent == 1)
                           {
                               bw.Write((UInt32)file.Parent1.Length);
                               bw.Write(GetBytes(file.Parent1).Where(b => b != 0x00).ToArray());
                           }
                           else
                           {
                               if(file.NumberParent == 2)
                               {
                                   bw.Write((UInt32)file.Parent1.Length);
                                   bw.Write(GetBytes(file.Parent1).Where(b => b != 0x00).ToArray());
                                   bw.Write((UInt32)file.Parent2.Length);
                                   bw.Write(GetBytes(file.Parent2).Where(b => b != 0x00).ToArray());
                               }
                           }
                              
                       bw.Write((UInt32)file.name.Length);
                       bw.Write(GetBytes(file.name).Where(b => b != 0x00).ToArray());//Writing w/o the 0x00 the string
                       bw.Write(Convert.ToUInt32(file.Compressed));
                       bw.Write((UInt32)0);
                       bw.Write((UInt32)0);
                       bw.Write((UInt32)0);
                       bw.Write((UInt32)0);
                       bw.Write((UInt32)0);//Reserved
                       bw.Write((UInt32)file.Data.Length);
                        if(file.Compressed){file.Data = RECOMCompressor.Compress(file.Data); bw.Write(file.Data.Length);}else{bw.Write(file.Data.Length);}
                       file.Data.Position = 0; //Ensure at beginning
                        bw.Write(file.Data);
                        file.Dispose();

                    }
            }
        }
        /// <summary>
        /// Function used for writing encrypted patches.
        /// </summary>
        /// <param name="stream">Stream in which the patch needs to be written in.</param>
        public void Write(Stream stream)
        {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    WriteDecrypted(ms);
                    data = ms.ToArray();
                }
                PatchManager.NGYXor(data);
                stream.Write(data, 0, data.Length);
        }

        public class FileEntry : IDisposable
        {
            /// <summary>
            ///     <para>File data, uncompressed</para>
            ///     <para>NULL if relinking</para>
            /// </summary>
            public Stream Data = null;

            /// <summary>Filename, used in UI</summary>
            public string name = null;

            public UInt32 NumberParent;
            public string Parent1;
            public string Parent2;
            public bool Compressed;


            public void Dispose()
            {
                if (Data != null)
                {
                    Data.Dispose();
                    Data = null;
                }
            }
        }
    }
    internal class Program
    {
        /// <summary>Bool that defines whether we should use user input or log input.</summary>
        public static bool uselog = false;
        /// <summary>StreamReader of the log file if the text needs to be readed on it.</summary>
        public static System.IO.StreamReader logfile;
        /// <summary>
        /// Function used for retrieving building time.
        /// </summary>
        /// <returns>Build time.</returns>
        private static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            var b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }
        /// <summary>
        /// Get whether the user inputted a yes or no and convert it to a bool.
        /// </summary>
        /// <returns>The output bool.</returns>
        public static bool GetYesNoInput()
        {
            int cL = Console.CursorLeft, cT = Console.CursorTop;
            do
            {
                string inp;
                if (!uselog) {inp = Console.ReadLine(); } else {inp = logfile.ReadLine(); }
                
                if (inp == "Y" || inp == "y")
                {
                    return true;
                }
                if (inp == "N" || inp == "n")
                {
                    return false;
                }
                Console.SetCursorPosition(cL, cT);
                Console.Beep();
            } while (true);
        }
        /// <summary>
        /// Main function of the patchmaker.
        /// </summary>
        /// <param name="args">Inputted arguments.</param>
        internal static void Mainp(string[] args)
        {
            bool log = false;

            Console.Title = RECOM_Toolkit.Program.program.ProductName + " " + RECOM_Toolkit.Program.program.FileVersion + " [" +
                            RECOM_Toolkit.Program.program.CompanyName + "]";
            var patch = new PatchFile();
            bool encrypt = true,
                batch = false,
                authorSet = false,
                verSet = false,
                changeSet = false,
                creditSet = false,
                otherSet = false;
            string output = "output.rcmpatch";
            string logtouse = "none";
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "-batch":
                        batch = true;
                        break;
                    case "-log":
                        log = true;
                        break;
#if DEBUG
                    case "-decrypted":
                        if (encrypt)
                        {
                            encrypt = false;
                            Console.WriteLine("Writing in decrypted mode!");
                        }
                        break;
#endif
                    case "-version":
                        if (!uint.TryParse(args[++i].Trim(), out patch.Version))
                        {
                            patch.Version = 1;
                        }
                        else
                        {
                            verSet = true;
                        }
                        break;
                    case "-author":
                        patch.Author = args[++i];
                        authorSet = true;
                        break;
                    case "-other":
                        patch.OtherInfo = args[++i];
                        otherSet = true;
                        break;
                    case "-uselog":
                        logtouse = args[++i];
                        uselog = true;
                        break;
                    case "-changelog":
                        patch.AddChange(args[++i]);
                        break;
                    case "-skipchangelog":
                        changeSet = true;
                        break;
                    case "-credits":
                        patch.AddCredit(args[++i]);
                        break;
                    case "-skipcredits":
                        creditSet = true;
                        break;
                    case "-output":
                        output = args[++i];
                        if (!output.EndsWith(".rcmpatch", StringComparison.InvariantCultureIgnoreCase))
                        {
                            output += ".rcmpatch";
                        }
                        break;
                }
            }
            if (log)
            {
                var filestream = new FileStream("log.log", FileMode.Create);
                var streamwriter = new StreamWriter(filestream);
                streamwriter.AutoFlush = true;
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
            }
            if (!batch)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                DateTime builddate = RetrieveLinkerTimestamp();
                Console.Write("{0}\nBuild Date: {2}\nVersion {1}", RECOM_Toolkit.Program.program.ProductName,
                    RECOM_Toolkit.Program.program.FileVersion, builddate);
                Console.ResetColor();
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nPRIVATE RELEASE\n");
                Console.ResetColor();
#else
                Console.Write("\nPUBLIC RELEASE\n");
#endif
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("\nProgrammed by {0}\nhttp://www.govanify.com",
                    RECOM_Toolkit.Program.program.CompanyName);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(
                    "\n\nThis tool is able to create patches for the software RECOM_Toolkit.\n\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nPress enter to run using the file:");
                Console.ResetColor();
                Console.Write(" {0}", output);

                if (!batch)
                {
                    Console.ReadLine();
                }
            }
            if (uselog) {logfile = new System.IO.StreamReader(logtouse);}
            if (!authorSet)
            {
                Console.Write("Enter author's name: ");
                if (!uselog) { patch.Author = Console.ReadLine().Trim(); } else { patch.Author = logfile.ReadLine().Trim(); }
            }
            if (!verSet)
            {
                Console.Write("Enter revision number: ");
                if (!uselog)
                {
                    while (!uint.TryParse(Console.ReadLine().Trim(), out patch.Version))
                    {
                        RECOM_Toolkit.Program.WriteWarning("\nInvalid number! ");
                    }
                }
                else
                {
                    while (!uint.TryParse(logfile.ReadLine().Trim(), out patch.Version))
                    {
                        RECOM_Toolkit.Program.WriteWarning("\nInvalid number! ");
                    }
                }
            }
            if (!changeSet)
            {
                Console.WriteLine("Enter changelog lines here (leave blank to continue):");
                do
                {
                    string inp;
                    if (!uselog) { inp = Console.ReadLine().Trim(); } else { inp = logfile.ReadLine().Trim(); }
                    if (inp.Length == 0)
                    {
                        break;
                    }
                    patch.AddChange(inp);
                } while (true);
            }
            if (!creditSet)
            {
                Console.WriteLine("Enter credits here (leave blank to continue):");
                do
                {
                    string inp;
                    if (!uselog) { inp = Console.ReadLine().Trim(); } else { inp = logfile.ReadLine().Trim(); }
                    if (inp.Length == 0)
                    {
                        break;
                    }
                    patch.AddCredit(inp);
                } while (true);
            }
            if (!otherSet)
            {
                Console.Write("Other information (leave blank to continue): ");
                if (!uselog) { patch.OtherInfo = Console.ReadLine().Trim(); } else { patch.OtherInfo = logfile.ReadLine().Trim(); }
            }
            do
            {
                var file = new PatchFile.FileEntry();
                Console.Write("\nEnter filename: ");
                //Target file
                if (!uselog) { file.name = Console.ReadLine().Replace("\"", "").Trim(); } else { file.name = logfile.ReadLine().Replace("\"", "").Trim(); }
                if (file.name.Length == 0)
                {
                    break;
                }
                try
                {
                    file.Data = File.Open(file.name, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception e)
                {
                    RECOM_Toolkit.Program.WriteWarning("Failed opening the file: " + e.Message);
                    continue;
                }
                var count = file.name.Count(x => x == '/');
                if ( count != 0) 
                {
                    if (count == 1)
                    {
                        file.NumberParent = 1;
                        file.Parent1 = file.name.Substring(0, file.name.IndexOf('/'));
                        file.name = file.name.Substring(file.name.IndexOf('/') + 1);
                    }
                    else
                    {
                        file.NumberParent = 2;
                        file.Parent1 = file.name.Substring(0, file.name.IndexOf('/'));//g001.DAT/SUB/NOM1/NOM2.EFF -> g001.DAT
                        var filename2 = file.name;
                        file.name = file.name.Substring(file.name.IndexOf('/') + 1);//SUB/NOM1/NOM2.EFF
                        file.name = file.name.Substring(file.name.IndexOf('/') + 1);//NOM1/NOM2.EFF
                        file.Parent2 = file.name.Substring(0, file.name.IndexOf('/'));//NOM1/NOM2.EFF -> NOM1
                        file.name = file.name.Substring(file.name.IndexOf('/') + 1);//NOM2.EFF o/


                    }
                }
                if(file.NumberParent == 2)
                {
                //Compress
                Console.Write("Compress this file? [Y/n] ");
                file.Compressed = GetYesNoInput();
                }
                
                patch.Files.Add(file);
            } while (true);
            try
            {
                using (FileStream fs = File.Open(output, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (encrypt)
                    {
                        patch.Write(fs);
                    }
                    else
                    {
                        patch.WriteDecrypted(fs);
                    }
                    if (batch) {Environment.Exit(0);}
                }
            }
            catch (Exception e)
            {
                RECOM_Toolkit.Program.WriteWarning("Failed to save file: " + e.Message);
                RECOM_Toolkit.Program.WriteWarning(e.StackTrace);
                try
                {
                    File.Delete("output.rcmpatch");
                }
                catch (Exception z)
                {
                    RECOM_Toolkit.Program.WriteWarning("Failed to delete file: " + z.Message);
                }
            }
            if (!batch)
            {
                Console.Write("Press enter to exit...");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
    }
}