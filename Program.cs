using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GovanifY.Utility;
using System.Text.RegularExpressions;
using KHCompress;

namespace RECOM_Toolkit

{
    public static class Program
    {
        /// <summary>
        ///     <para>Sector size of the ISO</para>
        ///     <para>Always 2048 bytes</para>
        /// </summary>
        public const int SectorSize = 2048;
        /// <summary>List that will be used for Packages.</summary>
        private static List<DAT> datList = new List<DAT>();
        /// <summary>List that will be used for Sub Packages.</summary>
        private static List<DAT> datList2 = new List<DAT>();
        /// <summary>List used for ensure if we have or not to rebuild a Package.</summary>
        public static List<string> Parent1LIST = new List<string>();
        /// <summary>List used for ensure if we have or not to rebuild a Sub Package.</summary>
        public static List<string> Parent2LIST = new List<string>();
        /// <summary>
        /// Var used for getting the version and the author of the software.
        /// </summary>
        public static readonly FileVersionInfo program = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
        /// <summary>PatchManager used for accessing to the PatchManager class.</summary>
        private static readonly PatchManager Patches = new PatchManager();
        /// <summary>Bool used to know wether or not we should show advanced infos to the user while extracting.</summary>
        private static bool _advanced;
        /// <summary>Bool used to know wether or not we should launch the patch extractor.</summary>
        private static bool rce;
        private static DateTime Builddate { get; set; }
        /// <summary>
        /// This function is used to retrieve build date.
        /// </summary>
        private static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = Assembly.GetCallingAssembly().Location;
            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;
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

            int i = BitConverter.ToInt32(b, cPeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }
        /// <summary>
        /// This function is used to warn the user about something wrong.
        /// </summary>
        /// <param name="format">The string that is warning the user.</param>
        /// <param name="arg">Optional, Exception messages, if any.</param>
        public static void WriteWarning(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }
        /// <summary>
        /// This function is used to show to the user an error.
        /// </summary>
        /// <param name="format">The string used for showing the error to the user.</param>
        /// <param name="arg">Optional, Exception messages, if any.</param>
        public static void WriteError(string format, params object[] arg)
        {
            WriteWarning(format, arg);
            //Let the user see the error
            Console.Write(@"Press enter to continue anyway... ");
            Console.ReadLine();
        }

      /*  private static void KH2PATCHInternal(Substream KH2PFileStream, string fname2, bool Compressed,
            UInt32 UncompressedSize)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fname2));
            }
            catch
            {
            } //Creating folder
            FileStream fileStream = File.Create(fname2);
            var buffer = new byte[KH2PFileStream.Length];
            var buffer2 = new byte[UncompressedSize];
            var file3 = new MemoryStream();
            if (Compressed)
            {
                KH2PFileStream.CopyTo(file3);
                buffer = file3.ToArray();
                buffer2 = KH2Compressor.decompress(buffer, UncompressedSize);
                    // Will crash if the byte array is equal to void.
                file3 = new MemoryStream(buffer2);
            }
            else
            {
                KH2PFileStream.CopyTo(file3);
                buffer2 = file3.ToArray();
                file3 = new MemoryStream(buffer2);
            }
            file3.CopyTo(fileStream);
            fileStream.Close();
            Console.WriteLine("Done!");
        }*/

        private static void KH2PatchExtractor(Stream patch)
        {
        /*    using (var br = new BinaryStream(patch, Encoding.ASCII, leaveOpen: true))
            {
                if (br.ReadUInt32() != 0x5032484b)
                {
                    br.Close();
                    br.Close();
                    throw new InvalidDataException("Invalid KH2Patch file!");
                }
                uint oaAuther = br.ReadUInt32(),
                    obFileCount = br.ReadUInt32(),
                    num = br.ReadUInt32();
                string patchname = "";
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
                        //Console.WriteLine("Changelog:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            //Console.WriteLine(" * {0}", br.ReadCString());
                        }
                    }
                    br.Seek(oaAuther + os2, SeekOrigin.Begin);
                    num = br.ReadUInt32();
                    if (num > 0)
                    {
                        br.Seek(num*4, SeekOrigin.Current);
                        Console.ResetColor();
                        //Console.WriteLine("Credits:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            //Console.WriteLine(" * {0}", br.ReadCString());
                        }
                        Console.ResetColor();
                    }
                    br.Seek(oaAuther + os3, SeekOrigin.Begin);
                    author = br.ReadCString();
                    if (author.Length != 0)
                    {
                       // Console.WriteLine("Other information:\r\n");
                        Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine("{0}", author);
                    }
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error reading kh2patch header: {0}: {1}\r\nAttempting to continue files...",
                        e.GetType(), e.Message);
                    Console.ResetColor();
                }
                Console.WriteLine("");
                br.Seek(obFileCount, SeekOrigin.Begin);
                num = br.ReadUInt32();
                while (num > 0)
                {
                    --num;
                    uint Hash = br.ReadUInt32();
                    oaAuther = br.ReadUInt32();
                    uint CompressedSize = br.ReadUInt32();
                    uint UncompressedSize = br.ReadUInt32();
                    uint Parent = br.ReadUInt32();
                    uint Relink = br.ReadUInt32();
                    bool Compressed = br.ReadUInt32() != 0;
                    bool IsNew = br.ReadUInt32() == 1; //Custom
                    if (Relink == 0)
                    {
                        if (CompressedSize != 0)
                        {
                            var KH2PFileStream = new Substream(patch, oaAuther, CompressedSize);
                            string fname2;
                            if (HashList.HashList.pairs.TryGetValue(Hash, out fname2)) { Console.Write("Extracting {0}...", fname2); }
                            else
                            { fname2 = "@noname/" + Hash + ".bin"; Console.Write("Extracting {0}...", fname2); }
                            long brpos = br.Tell();
                            KH2PATCHInternal(KH2PFileStream, fname2, Compressed, UncompressedSize);
                            br.ChangePosition((int) brpos);
                                //Changing the original position of the BinaryReader for what's next
                        }
                        else
                        {
                            throw new InvalidDataException("File length is 0, but not relinking.");
                        }
                    }
                    else
                    {
                        string fname3;
                        if (!HashList.HashList.pairs.TryGetValue(Relink, out fname3))
                        {
                            fname3 = String.Format("@noname/{0:X8}.bin", Relink);
                        }
                        Console.WriteLine("File relinked to {0}, no need to extract", fname3);
                    }
                    br.Seek(60, SeekOrigin.Current);
                }
            } //End of br*/
        }//Patch thingy will be added into PatchManager
        /// <summary>
        /// Basic sub packages parser.
        /// </summary>
        /// <param name="data">MemoryStream of the sub package.</param>
        /// <param name="datList">List in which all files will be stored.</param>
        public static void getOtherData(MemoryStream data, List<DAT> datList)
        {
                string text = data.extractPiece(0, 24, -1L).extractName();
                int num = 0;
                while (text.Length > 0)
                {
                    data.Position = (long)(num * 48);
                    text = data.extractPiece(0, 24, -1L).extractName();
                    if (text.Length <= 0)
                    {
                        break;
                    }
                    DAT dAT = new DAT();
                    dAT.Name = text;
                    dAT.DecSize = data.extractPiece(0, 4, -1L).extractInt32(0);
                    data.Position += 8L;
                    dAT.Size = data.extractPiece(0, 4, -1L).extractInt32(0) * 2048;
                    dAT.Offset = data.extractPiece(0, 4, -1L).extractInt32(0) * 2048;
                    data.Position += 1L;
                    byte flag = (byte)data.ReadByte();
                    dAT.Flag = (flag != 0);
                    datList.Add(dAT);
                    num++;
                }
        }
        /// <summary>
        /// Basic DAT parser to a dictionnary.
        /// </summary>
        /// <param name="data">MemoryStream of the DAT file.</param>
        /// <param name="datList">List in which sub packages will be stored.</param>
        private static void getDatFiles(MemoryStream data, List <DAT> datList)
        {
                int i = data.extractPiece(0, 4, -1L).extractInt32(0);
                int num = 0;
                while (i > 0)
                {
                    data.Position = (long)(num * 32);
                    i = data.extractPiece(0, 4, -1L).extractInt32(0);
                    if (i <= 0)
                    {
                        break;
                    }
                    DAT dAT = new DAT();
                    dAT.Offset = data.extractPiece(0, 4, -1L).extractInt32(0) * 2048;
                    dAT.Size = data.extractPiece(0, 4, -1L).extractInt32(0) * 2048 + 2048;
                    dAT.Name = data.extractPiece(0, 16, (long)dAT.Offset).extractName();
                    datList.Add(dAT);
                    num++;
            }
        }
        /// <summary>
        /// This function is used to extract the ISO, including sub-packages of the game KH RE:COM and to decompress them.
        /// </summary>
        /// <param name="iso">FileStream of the ISO used.</param>
        private static void ExtractISO(FileStream iso)
        {
            var br = new BinaryStream(iso, Encoding.ASCII, leaveOpen: true);
            UInt16 count;
            br.Seek(0x122006, SeekOrigin.Begin);//Go to the count shit and read it
            count = br.ReadUInt16();
            br.Seek(0x122800, SeekOrigin.Begin);//Go to the LBA shit.
            string filename;
            string filename2 = "";
            UInt64 sector;
            UInt64 sectorSize;
            UInt64 headerSize;
            UInt64 fileAmount;
            for (int i = 0; i < count; i++)//Let's read the LBA
            {
                filename = System.Text.Encoding.UTF8.GetString(br.ReadBytes(16)).TrimEnd('\0');
                sector = br.ReadUInt32();
                sectorSize = br.ReadUInt32();
                headerSize = br.ReadUInt32();
                fileAmount = br.ReadUInt32();
                long brpos = br.Tell();//Gettin' the position for beeing back here later
                if (_advanced)
                {
                    Console.WriteLine("-----------File {0,3}/{1}\n, using ISO", i + 1, count);
                    Console.WriteLine("Name: {0}", filename);
                    Console.WriteLine("Sector: {0}", sector);
                    Console.WriteLine("Sector size: {0}", sectorSize);
                    Console.WriteLine("Header Size: {0}", headerSize);
                    Console.WriteLine("File amount: {0}", fileAmount);
                }
                else
                {
                    Console.WriteLine("[ISO: {0,3}/{2}]\tExtracting {1}", i + 1, filename, count);
                }
                sector *= SectorSize;//Gettin' real pos of the file
                br.Seek((long)sector, SeekOrigin.Begin);//Go to the position
                byte[] outputData;
                outputData = br.ReadBytes((int)sectorSize * SectorSize);//Let's read the file
                string completename = "export/" + "ISO/" + filename;
                #region Directory and File Creation
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName("export/"));
                }
                catch (IOException e)
                {
                    WriteError("Failed creating directory: {0}", e.Message);
                    continue;
                }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName("export/ISO/"));
                }
                catch (IOException e)
                {
                    WriteError("Failed creating directory: {0}", e.Message);
                    continue;
                }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName("export/DAT/"));
                }
                catch (IOException e)
                {
                    WriteError("Failed creating directory: {0}", e.Message);
                    continue;
                }
                try
                {
                    BinaryWriter output = new BinaryWriter(File.Open(completename, FileMode.Create));
                    output.Write(outputData);
                    output.Flush();
                    output.Close();
                }
                catch (IOException e)
                {
                    WriteError("Failed creating file: {0}", e.Message);
                    continue;
                }
                #endregion//Creating the dirs and shits
                if (fileAmount != 0)
                {
                    #region Dir creation
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName("export/DAT/" + filename + "/"));
                    }
                    catch (IOException e)
                    {
                        WriteError("Failed creating directory: {0}", e.Message);
                        continue;
                    }
                    #endregion
                    Stream DATFile = new MemoryStream(outputData);
                    MemoryStream DATStream = new MemoryStream(outputData);
                    datList.Clear();//Clearing da stuff!!!
                    getDatFiles(DATStream, datList);//Adding files of the archive into the list
                    DATStream.Flush();
                    DATStream.Close();//For optimizing memory
                    int y = 1;
                    int a = 1;
                    #region Sub files
                    foreach (DAT current in datList)
                    {
                        if (_advanced)
                        {
                            Console.WriteLine("-----------File {0,3}/{1}\n, using {2}", y, datList.Count, filename);
                            Console.WriteLine("Name: {0}", current.Name);
                            Console.WriteLine("Offset: {0}", current.Offset);
                            Console.WriteLine("Size: {0}", current.Size);
                            Console.WriteLine("Flags: {0}", current.Flag);
                            Console.WriteLine("Decompressed size: {0}", current.DecSize);
                        }
                        else
                        {
                            Console.WriteLine("[{0}: {1,3}/{2}]\tExtracting {3}", filename, y, datList.Count, current.Name);
                        }
                        BinaryStream data2 = new BinaryStream(DATFile);
                        data2.Seek(current.Offset, SeekOrigin.Begin);
                        byte[] outdat = data2.ReadBytes(current.Size);
                        if (current.Name == "") { current.Name = "@noname" + a + ".bin"; a++; }//We cannot use null names on windows or any OS!
                        string completenamedat = "export/" + "DAT/" + filename + "/" + current.Name;
                        #region File creation
                        try
                        {
                            BinaryWriter output2 = new BinaryWriter(File.Open(completenamedat, FileMode.Create));
                            output2.Write(outdat);
                            output2.Flush();
                            output2.Close();
                        }
                        catch (IOException e)
                        {
                            WriteError("Failed creating file: {0}", e.Message);
                            continue;
                        }
                        #endregion
                        y++;
                        MemoryStream outdatfile = new MemoryStream(outdat);
                        getOtherData(outdatfile, datList2);//Adding files of the archive into the list
                        filename2 = filename;
                        #region Sub Sub files
                        if (datList2.Count != 0)
                        {
                            filename = current.Name;
                            #region Dir creation
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName("export/DAT/" + filename2 + "/SUB/" + filename + "/"));
                            }
                            catch (IOException e)
                            {
                                WriteError("Failed creating directory: {0}", e.Message);
                                continue;
                            }
                            #endregion
                            int z = 1;
                            foreach (DAT current2 in datList2)
                            {
                                
                                if (_advanced)
                                {
                                    Console.WriteLine("-----------File {0,3}/{1}\n, using {2}", z, datList2.Count, filename);
                                    Console.WriteLine("Name: {0}", current2.Name);
                                    Console.WriteLine("Offset: {0}", current2.Offset);
                                    Console.WriteLine("Size: {0}", current2.Size);
                                    Console.WriteLine("Flags: {0}", current2.Flag);
                                    Console.WriteLine("Decompressed size: {0}", current2.DecSize);
                                }
                                else
                                {
                                    Console.WriteLine("[{0}: {1,3}/{2}]\tExtracting {3}", filename, z, datList2.Count, current2.Name);
                                }
                                BinaryStream data4 = new BinaryStream(outdatfile);
                                if (current.Flag) { data2 = RECOMCompressor.Decompress(data2); }
                                data4.Seek(current2.Offset, SeekOrigin.Begin);
                                byte[] outsubdat = data4.ReadBytes(current2.Size);
                                if (current2.Name == "") { current2.Name = "@noname" + a + ".bin"; a++; }//We cannot use null names on windows or any OS!
                                string completenamesubdat = "export/" + "DAT/" + filename2 + "/SUB/" + filename + "/" + current2.Name;
                                #region File creation
                                try
                                {
                                    BinaryWriter output3 = new BinaryWriter(File.Open(completenamesubdat, FileMode.Create));
                                    output3.Write(outsubdat);
                                    output3.Flush();
                                    output3.Close();
                                }
                                catch (IOException e)
                                {
                                    WriteError("Failed creating file: {0}", e.Message);
                                    continue;
                                }
                                #endregion
                                z++;
                            }
                            datList2.Clear();
                        }
                        #endregion
                        filename = filename2;
                    }
                    datList.Clear();
                    filename = filename2;
                    #endregion
                }
                else
                {
                    br.Seek(brpos, SeekOrigin.Begin);
                }
                br.Seek(brpos, SeekOrigin.Begin);
            }
        }
        /// <summary>
        /// Basic function used for reading a Stream and putting its datas in a byte array
        /// </summary>
        /// <param name="input">The input Stream.</param>
        /// <returns>The byte array tooked from the stream.</returns>
        public static byte[] StreamToByteArray(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        /// <summary>
        /// This function is used to patch the ISO of the game KH RE:COM with a custom patching method.
        /// </summary>
        /// <param name="iso">FilesStream of the ISO used.</param>
        /// <param name="newISO">FileStream of the "new" patched ISO.</param>
        private static void PatchISO(FileStream iso, FileStream newISO)
        {
            //WOW WOW WOW THIS IS CRAPPY I NEED TO RE-OPTIMIZE THAT SHIT
            UInt32 secthingy = 0x8fC * SectorSize;//We're gonna write new files there
            var br = new BinaryStream(iso, Encoding.ASCII, leaveOpen: true);
            UInt16 count;
            br.Seek(0x122006, SeekOrigin.Begin);//Go to the count shit.
            count = br.ReadUInt16();
            br.Seek(0x0, SeekOrigin.Begin);//Then back to the beginning
            var nr = new BinaryStream(newISO, Encoding.ASCII, leaveOpen: true);
            var header = br.ReadBytes(0x122800 + 0x20*count);
            nr.Write(header);//Let's copy the header constant part to the new ISO

            string filename;
            UInt32 sector;
			UInt32 sectorFileSize;
			UInt32 headerSize;
			UInt32 fileAmount;
            byte[] namecopy;
            byte[] padding = new byte[] { 0x00 };
            UInt32 position = 0x122800 + 0x20 * (UInt32)count;
            PatchManager.Patch patch;
            //Now let's rebuild the LBA
            br.Seek(0x122800, SeekOrigin.Begin);//Go to the LBA shit.
            for (int i = 0; i < count; i++)
            {
                namecopy = br.ReadBytes(16);
                filename = System.Text.Encoding.UTF8.GetString(namecopy).TrimEnd('\0');
                sector = br.ReadUInt32();
                sectorFileSize = br.ReadUInt32();
                headerSize = br.ReadUInt32();
                fileAmount = br.ReadUInt32();

                if(fileAmount != 0)
                {
                    long brpos4 = br.Tell();
                    br.Seek(0x122800 + 0x20 * count, SeekOrigin.Begin);//Go to the 2ndLBA shit.
                    for (int r = 0; r < (int)fileAmount * 0x20; )
                    {
                        nr.Write(padding);
                        r++;
                    }
                    //Write junks for later again
                    br.Seek(brpos4, SeekOrigin.Begin);
                }
                //We will just copy since we might rebuild DATs later, SO it is junk.
            }
            for (int r = 0; r < 8800; )
            {
                nr.Write(padding);
                r++;
            }//Padding
            br.Seek(0x13B000, SeekOrigin.Begin);//Go to the 2ndLBA shit.
            nr.Seek(0x13B000, SeekOrigin.Begin);
            nr.Write(br.ReadBytes(0x343000));
            for (int b = 0; b < count; b++)
			{
                long brpos = br.Tell();//Gettin' the position for beeing back here later
                br.Seek(0x122800 + 0x20 * b, SeekOrigin.Begin);//Go to the lba

                namecopy = br.ReadBytes(16);
                filename = System.Text.Encoding.UTF8.GetString(namecopy).TrimEnd('\0');
                sector = br.ReadUInt32();
                sectorFileSize = br.ReadUInt32();
                headerSize = br.ReadUInt32();
                fileAmount = br.ReadUInt32();//Read that damn lba again
				//Skip reinserting the first 4 files since they aren't packages(ISO files needs to be modded though!)
                //TODO: Change this, make it dynamic. for BETA scene.dat needs to be handled correctly! I need to use the ISO LBA and such...
				if (b > 3)
				{
                    if(Patches.patches.TryGetValue(filename, out patch))//if we have to search through patches
                    {
                        Console.WriteLine("[ISO: {0,3}/{2}]\t{1} Patching...", b + 1, filename, count);
                        nr.Write(patch.Data);//Just writing the byte array
                        //Just basic copy since if it's in that damn patch file then it means no sub files are in it.....Normally....?
                        long nrpos3 = nr.Tell();//Gettin' the position for beeing back here later
                        nr.Seek(0x122800 + 0x20 * b, SeekOrigin.Begin);//Go to the lba
                        sector = secthingy / (UInt32)SectorSize;
                        nr.Write(namecopy);//We don't change the name afaik
                        nr.Write(sector);//Writes to the new sector
                        sectorFileSize =(UInt32)(patch.Data.Length / SectorSize);
                        nr.Write(sectorFileSize);//Writes the correct size of da file
                        nr.Write(headerSize);
                        nr.Write(fileAmount);//MIGHT CHANGES LATER ON!!! I'll have to write datList datas instead
                        MemoryStream DATagain2 = new MemoryStream(patch.Data);
                        patch.Dispose();
                        BinaryStream DATagain = new BinaryStream(DATagain2);
                        DATagain2.Dispose();
                        nr.Seek(position, SeekOrigin.Begin);//Go to the 2ndLBA shit.
                        nr.Write(DATagain.ReadBytes((int)fileAmount * 0x20));
                        DATagain.Dispose();
                        position = (UInt32)nr.Tell();
                        nr.Seek(nrpos3, SeekOrigin.Begin);
                        secthingy = (UInt32)nrpos3;//Since the LBA is already copied let's just increase this for later.
                    }
                    else
                    {
                         sector *= SectorSize;
                         br.Seek((long)sector,SeekOrigin.Begin);//Go to the file
                         sectorFileSize *= SectorSize;
                         byte[] tocopy = br.ReadBytes((int)sectorFileSize);
                        if(Parent1LIST.Contains(filename))
                        {
                          Console.WriteLine("[ISO: {0,3}/{2}]\t{1} Rebuilding...", b + 1, filename, count);
                          MemoryStream ModdedDAT = new MemoryStream();//New rebuilded DAT
                          MemoryStream DATFile3 = new MemoryStream(tocopy);//Old and boring original DAT
                          BinaryStream DATFile = new BinaryStream(DATFile3);
                          datList.Clear();
                          getDatFiles(DATFile3, datList);//Adding files of the archive into the list
                          int z = 1;
                          if (datList.Count != 0)
                          {
                              ModdedDAT.Position = (long)datList[0].Offset;
                              int p = 0;
                              MemoryStream SubPackage5 = new MemoryStream(DATFile3.extractPiece(0, datList[0].Offset, 0L));
                              foreach (DAT current in datList)
                              {
                                  DATFile.Seek(current.Offset, SeekOrigin.Begin);
                                  byte[] SubPackage = DATFile.ReadBytes(current.Size);
                                  byte[] SubPackagenm = DATFile3.extractPiece(0, current.Size, (long)current.Offset);
                                  if(Patches.patches.TryGetValue(current.Name, out patch))
                                  {
                                      if(patch.NumberParent == 1)
                                      {
                                          if (patch.Parent1 == filename)
                                          {
                                          Console.WriteLine("[{0}: {1,3}/{2}]\t{3} Patching...", filename, z, datList.Count, current.Name);
                                          current.Replace = patch.Data;
                                          }
                                          else
                                          {
                                              Console.WriteLine("[{0}: {1,3}/{2}]\t{3}", filename, z, datList.Count, current.Name);
                                          }
                                      }
                                      else
                                      {
                                          Console.WriteLine("[{0}: {1,3}/{2}]\t{3}", filename, z, datList.Count, current.Name);
                                      }
                                  }
                                  else
                                  {
                                      #region Sub files
                                      if (Parent2LIST.Contains(current.Name))
                                          {
                                              string filename2 = filename;
                                              Console.WriteLine("[{0}: {1,3}/{2}]\t{3} Rebuilding...", filename, z, datList.Count, current.Name);
                                              MemoryStream ModdedSubPackage = new MemoryStream();
                                              MemoryStream SubPackage4 = new MemoryStream(SubPackage);
                                              BinaryStream SubPackage3 = new BinaryStream(SubPackage4);
                                              datList2.Clear();
                                              getOtherData(SubPackage4, datList2);//Adding files of the archive into the list
                                              ModdedSubPackage.Position = (long)datList2[0].Offset;
                                              filename = current.Name;
                                              MemoryStream SubFile2 = new MemoryStream();
                                              byte[] expiece = SubPackage4.extractPiece(0, datList2[0].Offset, 0L);
                                              SubFile2.Write(expiece, 0, expiece.Length);
                                              int y = 1;
                                              int o = 0;
                                              foreach (DAT current2 in datList2)
                                              {
                                                  SubPackage3.Seek(current.Offset, SeekOrigin.Begin);
                                                  byte[] SubFile = SubPackage3.ReadBytes(current.Size);
                                                 // MemoryStream SubFile2 = new MemoryStream(SubFile);
                                                  if (Patches.patches.TryGetValue(current2.Name, out patch))
                                                  {
                                                      if (patch.NumberParent == 2)
                                                      {
                                                      Console.WriteLine("[{0}: {1,3}/{2}]\t{3} Patching...", filename, y, datList2.Count, current2.Name);
                                                      current2.Replace = patch.Data;
                                                      current2.Flag = patch.Compressed;
                                                      }
                                                      else
                                                      {
                                                          Console.WriteLine("[{0}: {1,3}/{2}]\t{3}", filename, y, datList2.Count, current2.Name);
                                                      }
                                                  }
                                                  else
                                                  {
                                                      Console.WriteLine("[{0}: {1,3}/{2}]\t{3}", filename, y, datList2.Count, current2.Name);
                                                  }
                                                  y++;
                                                  SubFile2.Position = (long)(o * 48 + 24);
                                                  while (ModdedSubPackage.Position % 2048L != 0L)
                                                  {
                                                      ModdedSubPackage.WriteByte(0);
                                                  }
                                                  int num2 = (int)ModdedSubPackage.Position;
                                                  if (current2.__replace != string.Empty)
                                                  {
                                                      if (current2.Flag)
                                                      {
                                                                  ModdedSubPackage.Write(current2.Replace, 0, current2.ReplaceSize);
                                                                  int num3 = current2.ReplaceSize;
                                                                  while (num3 % 2048 != 0)
                                                                  {
                                                                      num3++;
                                                                  }
                                                                  SubFile2.Write(current2.ReplaceSize.int32ToByteArray(), 0, 4);
                                                                  SubFile2.Position += 8L;
                                                                  SubFile2.Write((num3 / 2048).int32ToByteArray(), 0, 4);
                                                      }
                                                      else
                                                      {
                                                          ModdedSubPackage.Write(current2.Replace, 0, current2.ReplaceSize);
                                                          SubFile2.Write(current2.ReplaceSize.int32ToByteArray(), 0, 4);
                                                          SubFile2.Position += 8L;
                                                          int num3 = current2.ReplaceSize;
                                                          while (num3 % 2048 != 0)
                                                          {
                                                              num3++;
                                                          }
                                                          SubFile2.Write((num3 / 2048).int32ToByteArray(), 0, 4);
                                                      }
                                                  }
                                                  else
                                                  {
                                                      ModdedSubPackage.Write(SubPackage4.extractPiece(0, current2.Size, (long)current2.Offset), 0, current2.Size);
                                                      SubFile2.Write(current2.DecSize.int32ToByteArray(), 0, 4);
                                                      SubFile2.Position += 8L;
                                                      SubFile2.Write((current2.Size / 2048).int32ToByteArray(), 0, 4);
                                                  }
                                                  SubFile2.Write((num2 / 2048).int32ToByteArray(), 0, 4);
                                                  SubFile2.Position += 1L;
                                                  SubFile2.WriteByte(current2.Flag ? (byte)1 : (byte)0);
                                                  o++;

                                                 /*FileStream test = File.Open("out/test" + o + ".bin", FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                                                  var test2 = new BinaryStream(test, Encoding.ASCII, leaveOpen: true);
                                                  test2.Write(SubFile2.ToArray());*/
                                                  
                                              }
                                              ModdedSubPackage.Position = 0L;
                                              ModdedSubPackage.Write(SubFile2.ToArray(), 0, (int)SubFile2.Length);
                                              filename = filename2;
                                              //oldtodotag: Rebuild Sub file! ----->SubPackage2 is the rebuilded file, add support for bigger files if not added yet -> Done ModdedSubPackage works, now we need to copy it to the DAT array. I think.
                                              //DATFile3 needs to be modded, he has been converted into a resizable MemoryStream. FINALLY NO MODDED ANOTHER THING SO NOT RESIZABLE
                                              SubPackagenm = ModdedSubPackage.ToArray();//Like this only if it needs to be modded this shit change
                                          }
                                          else
                                          {
                                          Console.WriteLine("[{0}: {1,3}/{2}]\t{3}", filename, z, datList.Count, current.Name);
                                          }
                                      #endregion
                                  }
                                  z++;
                                  SubPackage5.Position = (long)(p * 32 + 4);
                                  while (ModdedDAT.Position % 2048L != 0L)
                                  {
                                      ModdedDAT.WriteByte(0);
                                  }
                                  SubPackage5.Write(((int)(ModdedDAT.Position / 2048L)).int32ToByteArray(), 0, 4);
                                  if (current.__replace != string.Empty)
                                  {
                                      ModdedDAT.Write(current.Replace, 0, current.ReplaceSize);
                                      SubPackage5.Write(((current.ReplaceSize) / 2048).int32ToByteArray(), 0, 4);
                                  }
                                  else
                                  {
                                      //We cannot write to DATFile2 when we're at the end of the stream!!! -> create new extensible memorystream...? -> Done
                                      ModdedDAT.Write(SubPackagenm, 0, current.Size);
                                      SubPackage5.Write(((current.Size) / 2048).int32ToByteArray(), 0, 4);
                                  }
                                  p++;
                                  //oldtodotag: Rebuild DAT! ----> DATFile2 is the rebuilded one -> crash for the last object! TO CORRECT -> Done, ModdedDAT is the modded one now
                              }
                              ModdedDAT.Position = 0L;
                              ModdedDAT.Write(SubPackage5.ToArray(), 0, (int)SubPackage5.Length);
                              nr.Write(ModdedDAT.ToArray());
                              //oldtodotag: Replace junk LBA by new size and such! /!\ DON'T FORGET SECTHINGY
                              long nrpos2 = nr.Tell();//Gettin' the position for beeing back here later
                              nr.Seek(0x122800 + 0x20 * b, SeekOrigin.Begin);//Go to the lba
                              sector = secthingy / (UInt32)SectorSize;
                              nr.Write(namecopy);//We don't change the name afaik
                              nr.Write(sector);//Writes to the new sector
                              sectorFileSize = (UInt32)((int)ModdedDAT.Length / SectorSize);
                              nr.Write(sectorFileSize);//Writes the correct size of da file
                              nr.Write(headerSize);
                              nr.Write(fileAmount);//MIGHT CHANGES LATER ON!!! I'll have to write datList datas instead
                              ModdedDAT.Seek(0, SeekOrigin.Begin);
                              BinaryStream DATagain = new BinaryStream(ModdedDAT);
                              nr.Seek(position, SeekOrigin.Begin);//Go to the 2ndLBA shit.
                              nr.Write(DATagain.ReadBytes((int)fileAmount * 0x20));
                              position = (UInt32)nr.Tell();
                              secthingy = (UInt32)nrpos2;//ofc for rebuilding the rest
                              nr.Seek(nrpos2, SeekOrigin.Begin);
                          }
                        }
                        else
                        {
                        Console.WriteLine("[ISO: {0,3}/{2}]\t{1}", b + 1, filename, count);
                        nr.Write(tocopy);//and copy it
                        long nrpos = nr.Tell();//Gettin' the position for beeing back here later
                        nr.Seek(0x122800 + 0x20 * b , SeekOrigin.Begin);//Go to the lba
                        sector = secthingy / (UInt32)SectorSize;
                        nr.Write(namecopy);//We don't change the name afaik
                        nr.Write(sector);//Writes to the new sector
                        sectorFileSize = (UInt32)(tocopy.Length / SectorSize);
                        nr.Write(sectorFileSize);//Writes the correct size of da file
                        nr.Write(headerSize);
                        nr.Write(fileAmount);//MIGHT CHANGES LATER ON!!! I'll have to write datList datas instead
                        MemoryStream DATagain2 = new MemoryStream(tocopy);
                        BinaryStream DATagain = new BinaryStream(DATagain2);
                        nr.Seek(position, SeekOrigin.Begin);//Go to the 2ndLBA shit.
                        nr.Write(DATagain.ReadBytes((int)fileAmount * 0x20));
                        position = (UInt32)nr.Tell();
                        nr.Seek(nrpos, SeekOrigin.Begin);
                        secthingy = (UInt32)nrpos;//Since the LBA is already copied let's just increase this for later.
                        }

                    }
                    br.Seek(brpos, SeekOrigin.Begin);//Go back to read the LBA and copy the next file
                }
                else
                {
                    Console.WriteLine("[ISO: {0,3}/{2}]\t{1}", b + 1, filename, count);
                }
            }//This could actually be more optimized; I'll check that later.

            br.Seek(0x106A20000, SeekOrigin.Begin);
            var lastconstant = br.ReadBytes(0x1408000);
            nr.Write(lastconstant);//Let's copy last constant part to the ISO.


        }

        /// <summary>The main entry point for the application.</summary>
        /// <exception cref="Exception">Cannot delete debug.log</exception>
        private static void Main(string[] args)
        {
            bool log = false;
            Console.Title = program.ProductName + " " + program.FileVersion + " [" + program.CompanyName + "]";
#if DEBUG
            try
            {
                File.Delete("debug.log");
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot delete debug.log!: {0}", e);
                throw new Exception();
            }
            Debug.AutoFlush = true;
            Debug.Listeners.Add(new TextWriterTraceListener("debug.log"));
#endif
            //Arguments
            string isoname = null;
            bool batch = false, extract = false;
            bool verify = false;
            #region Extract DLL
            string dllPath = "RECOM.dll";
            if (!File.Exists(dllPath))
            {
                using (Stream outFile = File.Create(dllPath))
                {
                    outFile.Write(RECOM_Toolkit.Properties.Resources.RECOM, 0, RECOM_Toolkit.Properties.Resources.RECOM.Length);
                }
            }
#endregion

            #region Arguments

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-exit":
                        return;
                    case "-batch":
                        batch = true;
                        break;
                    case "-extractor":
                        extract = true;
                        break;
                    case "-advancedinfo":
                        _advanced = true;
                        break;
                    case "-log":
                        log = true;
                        break;
#if DEBUG
                    case "-rcmpatchextractor":
                    case "-rce":
                        rce = true;
                        break;
#endif
                    case "-verifyiso":
                        verify = true;
                        break;
                    case "-help":
                        byte[] buffer = Encoding.ASCII.GetBytes(Properties.Resources.Readme);
                        File.WriteAllBytes("Readme.txt", buffer);
                        Console.Write("Help extracted as a Readme\nPress enter to leave the software...");
                        Console.Read();
                        return;
                    case "-patchmaker":
                        RECOM_PatchMaker.Program.Mainp(args);
                        break;
                    default:
                        if (File.Exists(arg))
                        {
                            if (rce)
                            {
                                if (isoname == null && arg.EndsWith(".rcmpatch", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    isoname = arg;
                                }
                            }
                            else if (isoname == null && arg.EndsWith(".iso", StringComparison.InvariantCultureIgnoreCase))
                            {
                                isoname = arg;
                            }
                            else if (arg.EndsWith(".rcmpatch", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Patches.AddPatch(arg);
                            }
                        }

                        break;
                }
            }

            #endregion Arguments

            #region Description

            if (log)
            {
                var filestream = new FileStream("log.log", FileMode.Create);
                var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
                //TODO Redirect to a txt, but problem: make disappear the text on the console. Need to mirror the text OR make a complete log
            }
            if (isoname == null)
            {
                isoname = "RECOM.ISO";
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Builddate = RetrieveLinkerTimestamp();
            Console.Write("{0}\nBuild Date: {2}\nVersion {1}", program.ProductName, program.FileVersion, Builddate);
            Console.ResetColor();
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nPRIVATE RELEASE\n");
            Console.ResetColor();
#else
                Console.Write("\nPUBLIC RELEASE\n");
#endif
#if NODECOMPRESS
                                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nNODECOMPRESS edition: Decompress algo is returning the input.\n");
                Console.ResetColor();
#else
#if extract
                                           Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nOLD EXTRACTING FEATURE, WARNING:NOT PATCHING CORRECTLY THE ISO\n");
                Console.ResetColor();
#endif
#endif

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(
                "\nProgrammed by {0}\nhttp://www.govanify.com\n\nSoftware under GPL 2 license, for more info, use the command -license",
                program.CompanyName);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (extract)
            {
                Console.Write(
                    "\n\nThis tool is able to extract the files of the game Kingdom Hearts Re: Chain of Memories.\n\n");
            }
            else
            {
                if (rce)
                {
                    Console.Write(
                        "\n\nThis tool is able to extract rcmpatch files.\n Please use this tool only if you losted your original files!\n\n");
                }
                if (!rce)
                {
                    Console.Write(verify
                        ? "\n\nThis tool will calculate the sha-1 hash of your iso for verify if it's a good dump of RECOM or not.\n\n"
                        : "\n\nThis tool is able to patch the game Kingdom Hearts Re: Chain of Memories.\nIt can modify iso files and rebuild a correct one.\n\n");
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nPress enter to run using the file:");
            Console.ResetColor();
            Console.Write(" {0}", isoname);
            if (!batch)
            {
                Console.ReadLine();
            }

            #endregion Description

            #region SHA1

            if (verify)
            {
                Console.Write("Calculating the SHA1 hash of your iso. Please wait...\n");
                using (SHA1 sha1 = SHA1.Create())
                {
                    using (FileStream stream = File.OpenRead(isoname))
                    {
                        //List of all SHA1 hashes of KH2 games
                        string isouser = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
                        const string RECOMJAPiso = "75d8b1b39a55cbc59077141c7828624970450dc0";
                        const string RECOMENGiso = "5f6851d6f60b1a7a83a6c5b1b2f938c3bf36af17";
                        const string RECOMBETAiso = "48a805a1fa28952cd35d006e38d8c1129fdbf517"; 
                        Console.Write("The SHA1 hash of the your iso is: {0}", isouser);
                        //I'm sure I can make those checks liter but too lazy to do it
                        if (isouser == RECOMJAPiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game RE:COM JAP!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == RECOMENGiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game RE:COM ENG!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == RECOMBETAiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game RECOM PROTOTYPE!\n");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Wait you SERIOUSLY HAVE ONE? o.O");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("\nYou don't have a correct dump! Please make a new one!");
                            Console.ResetColor();
                        }
                        EOF:
                        Console.ReadLine();
                        return;
                    }
                }
            }

            #endregion

            try
            {
                using (FileStream iso = File.Open(isoname, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (extract)
                    {
                        ExtractISO(iso);
                    }
                    else
                    {
                        if (rce)
                        {
                           //TODO: Add the patch extractor!
                        }
                        else
                        {
                            if (Patches.patches.Count == 0)
                            {
                                WriteWarning("No patches loaded!");
                            }//Beautiful warning that I DIDN'T forgotten this time
                            else
                            {
                                isoname = Path.ChangeExtension(isoname, ".NEW.ISO");
                                try
                                {
                                    using (FileStream NewISO = File.Open(isoname, FileMode.Create, FileAccess.ReadWrite,FileShare.None))
                                    {
                                        PatchISO(iso, NewISO);
                                    }
                                }
                                catch (Exception)
                                {
                                    //Delete the new "incomplete" iso
                                    File.Delete(isoname);
                                    throw;
                                }
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                WriteWarning("Failed to open file: " + e.Message);
            }
            catch (Exception e)
            {
                WriteWarning(
                    "An error has occured when trying to open your iso:\n{1}: {0}\n{2}",
                    e.Message, e.GetType().FullName, e.StackTrace);
            }
            Patches.Dispose();
            if (!batch)
            {
                Console.Write("\nPress enter to exit...");
                Console.ReadLine();
            }
        }
    }
}