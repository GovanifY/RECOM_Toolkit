using System;
using System.Diagnostics;
using System.IO;
using GovanifY.Utility;
using System.Text;
using System.Runtime.InteropServices;
using RECOM_Toolkit;

namespace KHCompress
{
    //PLEASE SOMEONE WHO HAVE ENOUGH TIME REDO THIS SHIT PLEASE IT HURTS MY EYES SO CRAPPY.(fyi I had a week max to do all that shit, so ofc didn't had the time to finish for original release all aspects)
    public class RECOMCompressor
    {
        [DllImport("RECOM.dll")]
        public static extern byte[] Encode(byte[] input);
        [DllImport("RECOM.dll")]
        public static extern byte[] Decode(byte[] input);

        static string input = Path.GetTempFileName();
        static string output = Path.GetTempFileName();
        /// <summary>
        /// Function used by the extractor to extract files with RE:COM custom algorithm.
        /// </summary>
        /// <param name="file">Stream of the inputted file.</param>
        /// <returns>Stream of the decompressed file.</returns>
        public static BinaryStream Decompress(BinaryStream file)
        {
            byte[] output = Decode(RECOM_Toolkit.Program.StreamToByteArray(file.BaseStream));
            MemoryStream file2 = new MemoryStream();
            BinaryStream file3 = new BinaryStream(file2);
            file3.Seek(0, SeekOrigin.Begin);
            file3.Write(output);
            return file3;
        }
        /// <summary>
        /// Function used by the patchmaker to compress files with RE:COM custom algorithm.
        /// </summary>
        /// <param name="file">Stream of the inputted file.</param>
        /// <returns>Stream of the compressed file.</returns>
        public static Stream Compress(Stream file)
        {
            byte[] output = Decode(RECOM_Toolkit.Program.StreamToByteArray(file));
            MemoryStream file2 = new MemoryStream();
            file2.Seek(0, SeekOrigin.Begin);
            file2.Write(output, 0, output.Length);
            return file2;
        }
    }
}