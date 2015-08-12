using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using GovanifY.Utility;
using RECOM_Toolkit;

namespace RECOM_Toolkit
{
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
    public sealed class PatchManager : IDisposable
    {

        /// <summary>Mapping of name->Patch</summary>
        internal Dictionary<string, Patch> patches = new Dictionary<string, Patch>();
        /// <summary>
        /// Function for disposing the patches dictionnary and the patchms list.
        /// </summary>
        public void Dispose()
        {
            foreach (var patch in patches)
            {
                patch.Value.Dispose();
            }
            patches.Clear();
        }
        /// <summary>
        /// Basic function made for XORing a byte array with a certain key.
        /// </summary>
        /// <param name="buffer">The byte array that needs to be XORed</param>
        public static void NGYXor(byte[] buffer)
        {
            byte[] v84 = { 164, 28, 107, 129, 48, 13, 35, 91, 92, 58, 167, 222, 219, 244, 115, 90, 160, 194, 112, 209, 40, 72, 170, 114, 98, 181, 154, 124, 124, 32, 224, 199, 34, 32, 114, 204, 38, 198, 188, 128, 45, 120, 181, 149, 219, 55, 33, 116, 6, 17, 181, 125, 239, 137, 72, 215, 1, 167, 110, 208, 110, 238, 124, 204 };
            int i = -1, l = buffer.Length;
            while (l > 0)
            {
                buffer[++i] ^= v84[(--l & 63)];
            }//The XOR key. It allows you to not download DIRECTLY copyrighted material. So you only get copyrighted material if you apply the patch. So the patch AND the soft aren't containing any of those materials, though the user will create it.
        }//If the key is long it is because it'll actually take more place in memory but it'll XOR in a quicker way. That's a 64 bytes random key.
        
        /// <summary>
        /// Function used for reading patches already decrypted.
        /// </summary>
        /// <param name="ms">MemoryStream of the decrypted patch.</param>
        /// <param name="patchname">Filename to the patch, if needed.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void AddPatch(Stream ms, string patchname = "")
        {
            using (var br = new BinaryStream(ms, Encoding.ASCII, leaveOpen: true))
            {
                if (br.ReadUInt32() != 0x504D4352)
                {
                    br.Close();
                    ms.Close();
                    throw new InvalidDataException("Invalid RCMPatch file!");
                }
                uint oaAuther = br.ReadUInt32(),
                    obFileCount = br.ReadUInt32(),
                    num = br.ReadUInt32();
                patchname = Path.GetFileName(patchname);
                try
                {
                    string author = br.ReadCString();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Loading patch {0} version {1} by {2}", patchname, num, author);
                    Console.ResetColor();
                    br.Seek(oaAuther, SeekOrigin.Begin);
                    uint os1 = br.ReadUInt32(),
                        os2 = br.ReadUInt32(),
                        os3 = br.ReadUInt32();
                    br.Seek(oaAuther + os1, SeekOrigin.Begin);
                    num = br.ReadUInt32();
                    if (num > 0)
                    {
                        br.Seek(num*4, SeekOrigin.Current);
                        Console.WriteLine("Changelog:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            Console.WriteLine(" * {0}", br.ReadCString());
                        }
                    }
                    br.Seek(oaAuther + os2, SeekOrigin.Begin);
                    num = br.ReadUInt32();
                    if (num > 0)
                    {
                        br.Seek(num*4, SeekOrigin.Current);
                        Console.ResetColor();
                        Console.WriteLine("Credits:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            Console.WriteLine(" * {0}", br.ReadCString());
                        }
                        Console.ResetColor();
                    }
                    br.Seek(oaAuther + os3, SeekOrigin.Begin);
                    author = br.ReadCString();
                    if (author.Length != 0)
                    {
                        Console.WriteLine("Other information:\r\n");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("{0}", author);
                    }
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error reading rcmpatch header: {0}: {1}\r\nAttempting to continue files...",
                        e.GetType(), e.Message);
                    Console.ResetColor();
                }
                Console.WriteLine("");
                br.Seek(obFileCount, SeekOrigin.Begin);
                num = br.ReadUInt32();
                while (num > 0)
                {
                    --num;
                    var nPatch = new Patch();
                    nPatch.NumberParent = br.ReadUInt32();
                    if (nPatch.NumberParent == 2)
                    {
                        nPatch.Parent1Size = br.ReadUInt32();
                        nPatch.Parent1 = System.Text.Encoding.UTF8.GetString(br.ReadBytes((int)nPatch.Parent1Size)).TrimEnd('\0');
                        RECOM_Toolkit.Program.Parent1LIST.Add(nPatch.Parent1);
                        nPatch.Parent2Size = br.ReadUInt32();
                        nPatch.Parent2 = System.Text.Encoding.UTF8.GetString(br.ReadBytes((int)nPatch.Parent2Size)).TrimEnd('\0');
                        RECOM_Toolkit.Program.Parent2LIST.Add(nPatch.Parent2);
                    }
                    else
                    {
                       if (nPatch.NumberParent == 1)
                       {
                           nPatch.Parent1Size = br.ReadUInt32();
                           nPatch.Parent1 = System.Text.Encoding.UTF8.GetString(br.ReadBytes((int)nPatch.Parent1Size)).TrimEnd('\0');
                           RECOM_Toolkit.Program.Parent1LIST.Add(nPatch.Parent1);
                       }
                    }
                    nPatch.nameSize = br.ReadUInt32();
                    nPatch.name = System.Text.Encoding.UTF8.GetString(br.ReadBytes((int)nPatch.nameSize)).TrimEnd('\0');
                    nPatch.Compressed = Convert.ToBoolean(br.ReadUInt32());
                    br.ReadUInt32();
                    br.ReadUInt32();
                    br.ReadUInt32();
                    br.ReadUInt32();
                    br.ReadUInt32();//Reserved
                    nPatch.Size = (long)br.ReadUInt32();
                    nPatch.CSize = (long)br.ReadUInt32();
                    if (nPatch.Size != 0)
                    {
                        nPatch.Stream = new Substream(ms, br.Tell(), nPatch.Size);
                        nPatch.Data = RECOM_Toolkit.Program.StreamToByteArray(nPatch.Stream);
#if EXTRACTPATCH
                        FileStream iso = File.Open("@out/" + nPatch.Parent2 + nPatch.name, FileMode.Create, FileAccess.ReadWrite,  FileShare.None);
                        var br2 = new BinaryStream(iso, Encoding.ASCII, leaveOpen: true);
                        br2.Write(nPatch.Data);
#endif
                        nPatch.Stream.Dispose();
                    }
                    else
                    {
                        throw new InvalidDataException("File length is 0!");
                    }
                    // Use the last file patch
                    if (patches.ContainsKey(nPatch.name))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        patches[nPatch.name].Dispose();
                        patches.Remove(nPatch.name);
                        Console.ResetColor();
                    }
                    patches.Add(nPatch.name, nPatch);
                }
            }
        }
        /// <summary>
        /// Function used for loading through a filename a patch.
        /// </summary>
        /// <param name="patchname">The filename of the patch that needs to be loaded.</param>
        public void AddPatch(string patchname)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(patchname, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.ReadByte() == 0x52 && fs.ReadByte() == 0x43 && fs.ReadByte() == 0x4D && fs.ReadByte() == 0x50)
                {
                    fs.Position = 0;
                    AddPatch(fs, patchname);//If the patch's decrypted then add it w/o decryption
                    return;
                }
                if (fs.Length > int.MaxValue)
                {
                    throw new OutOfMemoryException("File too large");
                }

                try
                {
                    fs.Position = 0;
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int) fs.Length);
                    NGYXor(buffer);
                    AddPatch(new MemoryStream(buffer), patchname);//Otherwise decrypt it and try to load it
                }
                    catch (Exception e) { Console.WriteLine("An error happened when trying to open your patch!: {0}", e); }
             }
            catch (Exception e)
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
                Console.WriteLine("Failed to parse patch: {0}", e.Message);
            }
             finally
             {
                    fs.Dispose();
                    fs = null;
             }
        }

        /// <summary>
        /// Custom class made for loading patches.
        /// </summary>
        internal class Patch : IDisposable
        {
            public UInt32 NumberParent;
            public string Parent1;
            public UInt32 Parent1Size;
            public string Parent2;
            public UInt32 Parent2Size;
            public bool Compressed;
            public long CSize;
            public string name;
            public long Size;
            public UInt32 nameSize;
            public Substream Stream;
            public byte[] Data;

            public void Dispose()
            {
                if (Stream != null)
                {
                    Stream.Dispose();
                    Stream = null;
                }
            }
        }
    }
}