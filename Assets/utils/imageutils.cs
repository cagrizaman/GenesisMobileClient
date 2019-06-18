using System;

using System.Drawing;

using System.IO;
using System.Runtime.InteropServices;
	public class ImageUtils
	{


		private static int[][][] ConvertImageDataToDimArrays(int numRows, int numCols, int numChannels, MemoryStream stream)
		{
			var imageMatrix = new int[numRows][][];
			for (int row = 0; row < numRows; row++)
			{
				imageMatrix[row] = new int[numCols][];
				for (int col = 0; col < numCols; col++)
				{
					imageMatrix[row][col] = new int[numChannels];
					for (int channel = 0; channel < numChannels; channel++)
					{
						imageMatrix[row][col][channel] = stream.ReadByte();
					}
				}
			}
			return imageMatrix;
		}


		private static byte[] ConvertDimArraysToImageData(int[][][] dimArray, int length, int width, int heigth)
		{
			var byteOut = new byte[length];
			var t = 0;
			for (int row = 0; row < width; row++)
			{
				for (int col = 0; col < heigth; col++)
				{
					for (int channel = 0; channel < dimArray[row][col].GetUpperBound(0)+1; channel++)
					{
						byteOut[t] = (byte)(dimArray[row][col][channel]);
						t++;
					}
				}
			}
			return byteOut;
		}

		 //convert byte array to bitmap

	}
