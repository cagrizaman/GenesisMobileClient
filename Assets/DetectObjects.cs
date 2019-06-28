using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.IO;
using System;
using Grpc.Core;
using Tensorflow.Serving;
using GoogleARCore;
using TensorFlowServing.Utils;
using System.Threading.Tasks;
public class DetectObjects : MonoBehaviour
{
    string server = "18.90.5.7:8081";
    Channel channel;

    //public Color32[] rgb_image;
    public Text viewHitText;
    PredictionService.PredictionServiceClient client;

    public TextAsset labels;

    private Camera cam;

    string[] label_list;

    private static bool readyForNextFrame = true;

    private List<Detection> detectedObjects;

    private List<PointCloudPoint> containedPoints;
    List<Trackable> sessionTrackables;
    private bool connected=true;

    // Start is called before the first frame update

    public delegate void OnDetectionAvailableCallbackFunc(PointCloudPoint hit, Detection obj);

    /// <summary>
    /// Callback function handle for receiving the output images.
    /// </summary>
    public event OnDetectionAvailableCallbackFunc onDetectionAvailableCallback = null;
    public class Detection
    {
        public float[] boundingBox;

        public float[] center;
        public float confidence;
        public string objectClass;

    }
    void Start()
    {
        detectedObjects = new List<Detection>();
        cam = GetComponent<Camera>();
        sessionTrackables = new List<Trackable>();
        //Subsrcibe to the RGB Image provider.
        RGB_Image_Provider imageSource = GetComponent<RGB_Image_Provider>();
        imageSource.OnImageAvailableCallback += new RGB_Image_Provider.OnImageAvailableCallbackFunc(onImageAvailable);

        // Read object detector labels. Currently I use Open Images Dataset with 600 images. 
        label_list = labels.text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Create GRPC client to connect to TF Serving.
        channel = new Channel(server, ChannelCredentials.Insecure,
        new List<Grpc.Core.ChannelOption> {
                    new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
                    new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue)
            });

        client = new PredictionService.PredictionServiceClient(channel);

        //Read Model Meta Data. 
        var response = client.GetModelMetadata(new GetModelMetadataRequest()
        {
            ModelSpec = new ModelSpec() { Name = "default" },
            MetadataField = { "signature_def" }
        });

        Debug.Log("Connected to Tensorflow Serving Model:");
        Debug.Log(response.ModelSpec.Name + " " + response.ModelSpec.Version);


    }

    // Update is called once per frame
    void Update()
    {
        if (Session.Status != SessionStatus.Tracking || !connected)
            return;

        if (readyForNextFrame)
        {
            foreach (Detection d in detectedObjects)
            {
                if (classPoints(d.boundingBox, out containedPoints))
                {
                    foreach (PointCloudPoint p in containedPoints)
                    {
                        onDetectionAvailableCallback(p, d);
                    }

                }
                // TrackableHit hit;
                // float x = cam.pixelWidth * (2 - d.boundingBox[1] - d.boundingBox[3]) / 2;
                // float y = cam.pixelHeight * (2 - d.boundingBox[0] - d.boundingBox[2]) / 2;

                // if (Frame.Raycast(x, y, TrackableHitFlags.Default, out hit))
                // {
                //     onDetectionAvailableCallback(hit, d);
                //     //viewHitText.text = d.objectClass + " at " + hit.Distance + " meters.";
                //     Session.GetTrackables(sessionTrackables );
                //     viewHitText.text=sessionTrackables.Count.ToString();
                // }

            }
            detectedObjects.Clear();
        }
    }

    private bool classPoints(float[] bbox, out List<PointCloudPoint> points)
    {
        points = new List<PointCloudPoint>();

        var ymax = cam.pixelHeight * (1-bbox[0]);
        var xmax = cam.pixelWidth * (1-bbox[1]);
        var ymin = cam.pixelHeight * (1-bbox[2]);
        var xmin = cam.pixelWidth * (1-bbox[3]);
        Vector3 centroid = new Vector3(0, 0, 0);
        if (Frame.PointCloud.PointCount > 0)
        {
            for (var i = 0; i < Frame.PointCloud.PointCount; i++)
            {
                PointCloudPoint pp =Frame.PointCloud.GetPointAsStruct(i);
                centroid += pp;
                Vector3 screenpoint = cam.WorldToScreenPoint(pp);
                if (Rect.MinMaxRect(xmin, ymin, xmax, ymax).Contains(screenpoint) &&
                    Vector3.Distance(centroid/(i+1),pp)<0.5)
                {
                    points.Add(pp);
                }
            }
        }
        return points.Count > 0;

    }

    public void onImageAvailable(uint[] rgb_image, int height, int width)
    {

        if (readyForNextFrame)
        {

            readyForNextFrame = false;
            Task t = Task.Run(() => recognize(rgb_image, height, width));
            t.ContinueWith((t1) =>
            {
                readyForNextFrame = true;
            });


        }
    }

    public async Task recognize(uint[] rgb_image, int height, int width)
    {


        var request = new PredictRequest()
        {
            ModelSpec = new ModelSpec() { Name = "default" }
        };
        request.Inputs.Add("inputs", TensorBuilder.CreateTensorFromImage(rgb_image, height, width, 3));


        // Run the prediction
        var predictResponse = await client.PredictAsync(request);

        //float num_classes= TensorProtoDecoder.TensorProtoToFloat(predictResponse.Outputs["num_classes"]);
        int num_detections = (int)TensorProtoDecoder.TensorProtoToFloat(predictResponse.Outputs["num_detections"]);
        float[] classes = TensorProtoDecoder.TensorProtoToFloatArray(predictResponse.Outputs["detection_classes"]);
        float[] bboxes = TensorProtoDecoder.TensorProtoToFloatArray(predictResponse.Outputs["detection_boxes"]);
        float[] scores = TensorProtoDecoder.TensorProtoToFloatArray(predictResponse.Outputs["detection_scores"]);


        for (var i = 0; i < num_detections; i++)
        {
            float[] bbox = new float[4];
            Array.Copy(bboxes, i * 4, bbox, 0, 4);
            detectedObjects.Add(new Detection
            {
                boundingBox = bbox,
                objectClass = label_list[(int)classes[i]],
                confidence = scores[i]
            });
        }
        readyForNextFrame = true;

    }

    void SessionComplete(bool isComplete){
        connected=!isComplete;
    }


}
