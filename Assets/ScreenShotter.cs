using UnityEngine;
using System.Collections;

public class ScreenShotter : MonoBehaviour
{

    private Texture2D texture;
    public delegate void OnImageAvailableCallbackFunc(Texture2D texture);

        /// <summary>
        /// Callback function handle for receiving the output images.
        /// </summary>
        public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;
    IEnumerator RecordFrame()
    {
        yield return new WaitForEndOfFrame();
        texture = ScreenCapture.CaptureScreenshotAsTexture();
        // do something with texture
        // cleanup
        if(OnImageAvailableCallback!=null){
            OnImageAvailableCallback(texture);
        }

        Object.Destroy(texture);
    }

    public void LateUpdate()
    {
        StartCoroutine(RecordFrame());
    }

}