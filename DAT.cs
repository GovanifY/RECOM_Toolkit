using System;
using System.IO;
namespace RECOM_Toolkit
{
    /// <summary>
    /// Custom class used for modding Packages.
    /// </summary>
	public class DAT
	{
		public string Name;
		public int Offset;
		public int Size;
		public bool Flag;
		public int DecSize;
        public byte[] _replace;
        public string __replace = string.Empty;
		public int ReplaceSize;
        /// <summary>Function used for replacing a file into a package.</summary>
		public byte[] Replace
		{
			get
			{
				return this._replace;
			}
			set
			{
				this._replace = value;
                this.__replace = "NotNULL";
				this.getReplaceData(value);
			}
		}
        /// <summary>
        /// Function used for adding the new size of the file.
        /// </summary>
        /// <param name="s">Byte array of the file.</param>
		private void getReplaceData(byte[] s)
		{
			int replaceSize = (int)s.Length;
			this.ReplaceSize = replaceSize;
		}
	}
}
