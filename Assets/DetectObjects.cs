using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using Grpc.Core;
using Tensorflow.Serving;

public class DetectObjects : MonoBehaviour
{
    string dataFile = "img.jpg";
    string FILE_PATH;
    string server = "localhost:8081";
    Channel channel;
    PredictionService.PredictionServiceClient client;
    // Start is called before the first frame update
    void Start()
    {
        FILE_PATH = Path.Combine(Application.dataPath, dataFile);

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

        Debug.Log(response.ModelSpec.Name +  " " + response.ModelSpec.Version);

                    var request = new PredictRequest()
            {
                ModelSpec = new ModelSpec() {Name = "model", SignatureName = "predict_image"}
            };

            //Add image tensor
            using (Stream stream = new FileStream(FILE_PATH, FileMode.Open))
				{
					request.Inputs.Add("image", TensorBuilder.CreateTensorFromImage(stream, 1.0f));
				}

            // Run the prediction
            var predictResponse = client.Predict(request);
            Debug.Log(predictResponse.Outputs["num_detections"]);
    }

}
