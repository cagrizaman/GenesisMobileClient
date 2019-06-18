using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Grpc.Core;
using Tensorflow.Serving;
using GoogleARCore;
public class DetectObjects : MonoBehaviour
{
    string server = "18.90.5.7:8081";
    Channel channel;
    PredictionService.PredictionServiceClient client;

    // Start is called before the first frame update
    void Start()
    {
        ScreenShotter imageSource = GetComponent<ScreenShotter>();
        imageSource.OnImageAvailableCallback+=new ScreenShotter.OnImageAvailableCallbackFunc(recognize);
      
        channel = new Channel(server, ChannelCredentials.Insecure,
        new List<Grpc.Core.ChannelOption> {
                    new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
                    new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue)
            });
        client = new PredictionService.PredictionServiceClient(channel);
        var response = client.GetModelMetadata(new GetModelMetadataRequest()
        {
            ModelSpec = new ModelSpec() { Name = "default" },
            MetadataField = { "signature_def" }
        });

        Debug.Log("CONNECTING TO TENSORFLOW SERVER");
        Debug.Log(response.ModelSpec.Name + " " + response.ModelSpec.Version);


    }

    // Update is called once per frame
    void Update()
    {

    }


    
    public void recognize(Texture2D texture)
    {


        Debug.Log("We are sending image to the server");

        var request = new PredictRequest()
        {
            ModelSpec = new ModelSpec() { Name = "default" }
        };

        request.Inputs.Add("inputs", TensorBuilder.CreateTensorFromImage(texture.GetPixels32(),texture.height,texture.width,3));


        // Run the prediction
        var predictResponse = client.Predict(request);
        Debug.Log("NUm PREDICTIONS");
        Debug.Log(predictResponse.Outputs["num_detections"]);

    }

    public static Color32 YUVtoRGB(double y, double u, double v)
    {
        Color32 rgb = new Color32();

        rgb.r = Convert.ToByte((y + 1.139837398373983740 * v) * 255);
        rgb.g = Convert.ToByte((
            y - 0.3946517043589703515 * u - 0.5805986066674976801 * v) * 255);
        rgb.b = Convert.ToByte((y + 2.032110091743119266 * u) * 255);

        return rgb;
    }

}
