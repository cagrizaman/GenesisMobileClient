using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Grpc.Core;
using Tensorflow.Serving;
using GoogleARCore;
using TensorFlowServing.Utils;
using System.Threading.Tasks;
public class RGB_Image_Provider : MonoBehaviour
{

    public static uint[] sourceRGB;
    public static int height;

    public static int width;
    public delegate void OnImageAvailableCallbackFunc(uint[] rgb, int height, int width);

    /// <summary>
    /// Callback function handle for receiving the output images.
    /// </summary>
    public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;

    private bool connected=true;
    private void Start()
    {


    }

    private void Update()
    {
        if (Session.Status != SessionStatus.Tracking || !connected)
            return;
            
        using (var bytes = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!bytes.IsAvailable)
            {
                return;
            }
                
             if (sourceRGB == null)
            {
                width = bytes.Width;
                height = bytes.Height;
                sourceRGB = new uint[bytes.Height * bytes.Width];
            }
            bool result = ImageUtils.convertYUV420SPToARGB8888(sourceRGB, bytes.Width, bytes.Height, bytes.
                YRowStride, bytes.UVRowStride, bytes.UVPixelStride, bytes.Y, bytes.U, bytes.V);
            bytes.Release();
            if (result && OnImageAvailableCallback != null)
            {
                OnImageAvailableCallback(sourceRGB, height, width);
            }

        }
    }

    void SessionComplete(bool isComplete){
        connected=!isComplete;
    }


}