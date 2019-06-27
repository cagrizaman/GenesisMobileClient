using System;

using System.Drawing;

using System.IO;
using GoogleARCore;
using UnityEngine;
using UnityEngine.XR;
public class ImageUtils
{

    static int kMaxChannelValue = 262143;

    private static byte[] s_YImageBuffer = new byte[0];
    private static byte[] s_UImageBuffer = new byte[0];

    private static byte[] s_VImageBuffer = new byte[0];

    private static int s_YImageBufferSize = 0;
    private static int s_UVImageBufferSize = 0;


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
                for (int channel = 0; channel < dimArray[row][col].GetUpperBound(0) + 1; channel++)
                {
                    byteOut[t] = (byte)(dimArray[row][col][channel]);
                    t++;
                }
            }
        }
        return byteOut;
    }


    public static bool convertYUV420SPToARGB8888(
             uint[] output, int width, int height, int YStride, int UVStride, int UVPixelStride, IntPtr Ybuffer, IntPtr Ubuffer, IntPtr Vbuffer)
    {
        //Get buffer copy for Y
        int YbufferSize = YStride * height;
        if (YbufferSize != s_YImageBufferSize || s_YImageBuffer.Length == 0)
        {
            s_YImageBufferSize = YbufferSize;
            s_YImageBuffer = new byte[YbufferSize];
        }
        // Move raw data into managed buffer.

        System.Runtime.InteropServices.Marshal.Copy(Ybuffer, s_YImageBuffer, 0, s_YImageBufferSize);

        //Get buffer copy for U and V

        if (UVStride != width || UVPixelStride != 2)
        {

            return false;
        }
        int UVBufferSize = UVStride * height / 2;
        if (UVBufferSize != s_UVImageBufferSize || s_UImageBuffer.Length == 0 || s_VImageBuffer.Length == 0)
        {
            s_UVImageBufferSize = UVBufferSize;
            s_UImageBuffer = new byte[UVBufferSize];
            s_VImageBuffer = new byte[UVBufferSize];
        }

        System.Runtime.InteropServices.Marshal.Copy(Ubuffer, s_UImageBuffer, 0, s_UVImageBufferSize);
        System.Runtime.InteropServices.Marshal.Copy(Vbuffer, s_VImageBuffer, 0, s_UVImageBufferSize);

        int yp = 0;
        for (int j = 0; j < height; j++)
        {
            int pY = YStride * j;
            int pUV = UVStride * (j >> 1);

            for (int i = 0; i < width; i++)
            {
                int uv_offset = pUV + (i >> 1) * UVPixelStride;

                output[yp++] = YUV2RGB(
                       s_YImageBuffer[pY + i],
                       s_UImageBuffer[uv_offset],
                       s_VImageBuffer[uv_offset]);
            }
        }

        return true;

    }





    // ADOPTED FROM TENSORFLOW LITE ANDROID EXAMPLE JAVA CODE. 
    private static uint YUV2RGB(int y, int u, int v)
    {
        // Adjust and check YUV values
        y = (y - 16) < 0 ? 0 : (y - 16);
        u -= 128;
        v -= 128;

        // This is the floating point equivalent. We do the conversion in integer
        // because some Android devices do not have floating point in hardware.
        // nR = (int)(1.164 * nY + 2.018 * nU);
        // nG = (int)(1.164 * nY - 0.813 * nV - 0.391 * nU);
        // nB = (int)(1.164 * nY + 1.596 * nV);
        int y1192 = 1192 * y;
        int r = (y1192 + 1634 * v);
        int g = (y1192 - 833 * v - 400 * u);
        int b = (y1192 + 2066 * u);

        // Clipping RGB values to be inside boundaries [ 0 , kMaxChannelValue ]
        r = r > kMaxChannelValue ? kMaxChannelValue : (r < 0 ? 0 : r);
        g = g > kMaxChannelValue ? kMaxChannelValue : (g < 0 ? 0 : g);
        b = b > kMaxChannelValue ? kMaxChannelValue : (b < 0 ? 0 : b);
        return 0xff000000 | (((uint)r << 6) & 0xff0000) | (((uint)g >> 2) & 0xff00) | (((uint)b >> 10) & 0xff);
        // return new Color32(red,green,blue,alpha);
    }/*  */
     //convert byte array to bitmap

}
