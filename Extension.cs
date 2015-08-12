using System;
using System.IO;
namespace RECOM_Toolkit
{
	internal static class Extension
	{
		public static string extractName(this byte[] data)
		{
			string text = string.Empty;
			for (int i = 0; i < data.Length; i++)
			{
				byte b = data[i];
				if (b == 0)
				{
					break;
				}
				text += Convert.ToChar(b);
			}
			return text;
		}
		public static int extractInt32(this byte[] bytes, int index = 0)
		{
			return (int)bytes[index + 3] << 24 | (int)bytes[index + 2] << 16 | (int)bytes[index + 1] << 8 | (int)bytes[index];
		}
		public static byte[] extractPiece(this MemoryStream ms, int offset, int length, long changeOffset = -1L)
		{
			if (changeOffset > -1L)
			{
				ms.Position = changeOffset;
			}
			byte[] array = new byte[length];
			ms.Read(array, 0, length);
			return array;
		}
		public static byte[] int32ToByteArray(this int value)
		{
			byte[] array = new byte[4];
			for (int i = 0; i < 4; i++)
			{
				array[i] = (byte)(value >> i * 8 & 255);
			}
			return array;
		}
	}
}
