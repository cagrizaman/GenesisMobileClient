using UnityEngine;
using System.Collections;

public class ScreenShotter : MonoBehaviour
{

    private Texture2D texture;
    private int counter = 0;
    public delegate void OnImageAvailableCallbackFunc(Texture2D texture);

    /// <summary>
    /// Callback function handle for receiving the output images.
    /// </summary>
    public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;
    IEnumerator RecordFrame()
    {
        if (counter % 10 == 0)
        {
            yield return new WaitForEndOfFrame();
            texture = ScreenCapture.CaptureScreenshotAsTexture();
            texture.Resize(600,300,TextureFormat.RGBA32,false);

            // do something with texture
            // cleanup
            if (OnImageAvailableCallback != null)
            {
            //    OnImageAvailableCallback(texture);
            }

            Object.Destroy(texture);
            counter = 0;
        }
        counter++;
    }

    public void LateUpdate()
    {
        StartCoroutine(RecordFrame());
    }

}