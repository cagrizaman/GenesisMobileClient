using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Grpc.Core;
using GoogleARCore;
public class GrpcClient : MonoBehaviour
{
    Channel channel;
    GenesisSimulator.GenesisSimulatorClient client;
    public string host;
    public string port;
    public bool connected = false;
    private bool isCameraStreaming = false;
    private List<int> pointIds;
    private static Dictionary<int, Point> pointCache;
    private static Dictionary<int, DetectionPoint> detectionCache;

    private uint[] colors;

    private int rgb_height = 0;
    private int rgb_width = 0;
    private List<Point> pointList;
    private bool readyForNextFrame = false;
    private int pointLocation = 0;

    private bool isCallComplete = true;

    public Text cameraButtonText;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        //Subsrcibe to the RGB Image provider.
        RGB_Image_Provider imageSource = GetComponent<RGB_Image_Provider>();
        imageSource.OnImageAvailableCallback += new RGB_Image_Provider.OnImageAvailableCallbackFunc(onImageAvailable);

        DetectObjects objectDetector = GetComponent<DetectObjects>();
        objectDetector.onDetectionAvailableCallback += new DetectObjects.OnDetectionAvailableCallbackFunc(onDetectionAvailable);
        pointIds = new List<int>();
        pointList = new List<Point>();
        pointCache = new Dictionary<int, Point>();
        detectionCache = new Dictionary<int, DetectionPoint>();
        channel = new Channel(host + ":" + port, ChannelCredentials.Insecure);
        client = new GenesisSimulator.GenesisSimulatorClient(channel);
        cam = GetComponent<Camera>();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }


    public void onImageAvailable(uint[] rgb_image, int height, int width)
    {

        if (colors == null)
        {
            rgb_height = height;
            rgb_width = width;
            colors = new uint[width * height];
        }

        if (readyForNextFrame)
            return;
        rgb_image.CopyTo(colors, 0);
        readyForNextFrame = true;

    }

    public void onDetectionAvailable(PointCloudPoint hit, DetectObjects.Detection d)
    {

        detectionCache[hit.Id] = new DetectionPoint
        {
            HitId = hit.Id,
            HitOffset = 0,
            ObjectClass = d.objectClass,
            Confidence = d.confidence,
            Pos = new Position { X = hit.Position.x, Y = hit.Position.y, Z = hit.Position.z },
            Rot = new ProtoQuaternion { X = 0, Y = 0, Z = 0, W = 0 },
            Ex = new Extent { X = 1, Y = 1 }
        };
        //Confirmation result = client.AddPointObject();

        // PREVIOUS IMPLEMENTATION WITH Parameters (TrackableHit hit, Detection.Detection d)
        // int hit_id = -1;
        // float hit_offset = 0.5f;

        // if (Frame.PointCloud.PointCount > 0)
        // {
        //     for (var i = 0; i < Frame.PointCloud.PointCount; i++)
        //     {
        //         PointCloudPoint p = Frame.PointCloud.GetPointAsStruct(i);
        //         float dist = Vector3.Distance(hit.Pose.position, p.Position);
        //         if (dist < hit_offset)
        //         {
        //             hit_id = p.Id;
        //             hit_offset = dist;
        //         }
        //     }
        // }

        // if (hit.Trackable is DetectedPlane)
        // {
        //     Pose p = ((DetectedPlane)(hit.Trackable)).CenterPose;
        //     Vector3 hp = hit.Pose.position;
        //     float exx = ((DetectedPlane)(hit.Trackable)).ExtentX;
        //     float exz = ((DetectedPlane)(hit.Trackable)).ExtentZ;
        //     Confirmation result = client.AddPlaneObject(new DetectionPlane
        //     {
        //         HitId = hit_id,
        //         HitOffset = hit_offset,
        //         ObjectClass = d.objectClass,
        //         Confidence = d.confidence,
        //         Pos = new Position { X = p.position.x, Y = p.position.y, Z = p.position.z },
        //         HitPos = new Position {X=hp.x,Y=hp.y,Z=hp.z},
        //         Rot = new ProtoQuaternion { X = p.rotation.x, Y = p.rotation.y, Z = p.rotation.z, W = p.rotation.w },
        //         Ex = new Extent { X = exx, Y = exz }
        //     });

        // }

        // else if (hit.Trackable is FeaturePoint)
        // {
        //     FeaturePoint fp = (FeaturePoint)hit.Trackable;

        //     Pose p = fp.Pose;
        //     Vector3 lower_left = cam.ScreenToWorldPoint(new Vector3(d.boundingBox[1], d.boundingBox[0], hit.Distance));
        //     Vector3 upper_right = cam.ScreenToWorldPoint(new Vector3(d.boundingBox[3], d.boundingBox[2], hit.Distance));
        //     Vector3 upper_left = cam.ScreenToWorldPoint(new Vector3(d.boundingBox[1], d.boundingBox[2], hit.Distance));
        //     float exx = cam.pixelWidth * (upper_left - upper_right).magnitude;
        //     float exz = cam.pixelHeight * (upper_left - lower_left).magnitude;
        //     Confirmation result = client.AddPointObject(new DetectionPoint
        //     {
        //         HitId = hit_id,
        //         HitOffset = hit_offset,
        //         ObjectClass = d.objectClass,
        //         Confidence = d.confidence,
        //         Pos = new Position { X = p.position.x, Y = p.position.y, Z = p.position.z },
        //         Rot = new ProtoQuaternion { X = p.rotation.x, Y = p.rotation.y, Z = p.rotation.z, W = p.rotation.w },
        //         Ex = new Extent { X = exx, Y = exz }
        //     });
        // }

    }
    // Update is called once per frame
    void Update()
    {
        if (connected)
        {
            string user = "Cagri";
            //var reply = client.GetCameraTransform(new CameraRequest { Message = user });

            var update = new CameraUpdate
            {
                X = transform.position.x,
                Y = transform.position.y,
                Z = transform.position.z,
                Q = transform.rotation.w,
                W = transform.rotation.x,
                R = transform.rotation.y,
                T = transform.rotation.z
            };

            if (isCameraStreaming)
            {
                var reply = client.UpdateCamera(update);
            }
            if (Session.Status != SessionStatus.Tracking)
                return;

            if (Frame.PointCloud.PointCount > 0 && Frame.PointCloud.IsUpdatedThisFrame && readyForNextFrame && isCallComplete)
            {
                for (var i = 0; i < Frame.PointCloud.PointCount; i++)
                {

                    PointCloudPoint pcpoint = Frame.PointCloud.GetPointAsStruct(i);
                    Vector3 screenCoordinate = cam.WorldToScreenPoint(pcpoint.Position);

                    if (Vector3.Dot(transform.forward, pcpoint - cam.transform.position) >= 0)
                    {
                        // checking if screenPoint is inside the screen area:
                        if (Rect.MinMaxRect(0, 0, Screen.width, Screen.height).Contains(screenCoordinate))
                        {
                            Debug.Log("Cam Width" + cam.pixelWidth);
                            int x_index = (int)screenCoordinate.x * rgb_width / cam.pixelWidth;
                            int y_index = (int)screenCoordinate.y * rgb_height / cam.pixelHeight;


                            //BELOW IMPLEMENTATION DOES NOT WORK...l.
                            uint color_r = 0;
                            uint color_g = 0;
                            uint color_b = 0;

                            for (int p = -1; p < 2; p++)
                            {
                                for (int t = -1; t < 2; t++)
                                {
                                    color_r += (0xFF & (colors[(rgb_height * (y_index + p)) + x_index + t] >> 16));
                                    color_g += (0xFF & (colors[(rgb_height * (y_index + p)) + x_index + t] >> 8));
                                    color_b += ((0xFF & colors[(rgb_height * (y_index + p)) + x_index + t] >> 0));
                                }
                            }
                            color_r = color_r / 9;
                            color_b = color_b / 9;
                            color_g = color_g / 9;
                            uint color = (color_r << 16) | (color_g << 8) | (color_b << 0);
                            //uint color = colors[(rgb_height * y_index) + x_index];

                            if (pcpoint.Id > 0)
                            {
                                pointCache[pcpoint.Id] = new Point
                                {
                                    X = pcpoint.Position.x,
                                    Y = pcpoint.Position.y,
                                    Z = pcpoint.Position.z,
                                    Id = pcpoint.Id,
                                    Color = color
                                };
                            }
                        }
                    }
                    Debug.Log("POINT CLOUD SIZE: " + Frame.PointCloud.PointCount);
                }
                readyForNextFrame = false;

            }
        }

    }


    public void changeCameraState(){
        isCameraStreaming=!isCameraStreaming;
        cameraButtonText.text=isCameraStreaming? "Disconnect":"Stream Camera";
    }
    public async void sendAsync()
    {
        //BroadcastMessage("SessionComplete", true);
        isCallComplete = false;
        await sendCachedPoints();
    }

    void SessionComplete(bool isComplete)
    {
        connected = !isComplete;
    }
    public async Task sendPoints()
    {
        try
        {
            using (var call = client.RecordPoints())
            {
                bool requestSent = false;
                int startIndex = 0;
                if (Frame.PointCloud.PointCount > 1000)
                {
                    startIndex = Frame.PointCloud.PointCount - 1000;
                }
                for (int i = 0; i < Frame.PointCloud.PointCount; i++)
                {

                    PointCloudPoint pcpoint = Frame.PointCloud.GetPointAsStruct(i);
                    Vector3 screenCoordinate = cam.WorldToScreenPoint(pcpoint.Position);

                    if (Vector3.Dot(transform.forward, pcpoint - cam.transform.position) >= 0)
                    {
                        // checking if screenPoint is inside the screen area:
                        if (Rect.MinMaxRect(0, 0, Screen.width, Screen.height).Contains(screenCoordinate))
                        {
                            // screenPoint is a valid screen point

                            int x_index = (int)screenCoordinate.x * rgb_width / cam.pixelWidth;
                            int y_index = (int)screenCoordinate.y * rgb_height / cam.pixelHeight;
                            uint color = colors[(rgb_width * y_index) + x_index];

                            if (pcpoint.Id > 0)
                            {
                                requestSent = true;
                                await call.RequestStream.WriteAsync(new Point
                                {
                                    X = pcpoint.Position.x,
                                    Y = pcpoint.Position.y,
                                    Z = pcpoint.Position.z,
                                    Id = pcpoint.Id,
                                    Color = color
                                });
                            }
                        }
                    }
                }

                if (requestSent)
                {
                    await call.RequestStream.CompleteAsync();

                    Confirmation result = await call.ResponseAsync;
                }
                pointList.Clear();

                isCallComplete = true;
            }
        }
        catch (RpcException e)
        {
            Debug.Log("RPC failed");
            throw;
        }
    }


   
    public async Task sendCachedPoints()
    {
        PointList plist = new PointList();
        DetectionPointList dplist = new DetectionPointList();
        foreach (var p in pointCache)
        {
            plist.Points.Add(p.Value);

        }
        try
        {
            var result = client.AddPointList(plist);
        }
        catch (RpcException e)
        {
            Debug.Log("RPC failed");
            throw;
        }

        foreach (var dp in detectionCache)
        {
            dplist.Dpoints.Add(dp.Value);
        }

        try
        {
            var result = client.AddDetectionPointList(dplist);
        }
        catch (RpcException e)
        {
            Debug.Log("RPC failed");
            throw;
        }
        pointCache.Clear();
        detectionCache.Clear();
        isCallComplete = true;


        // try
        // {
        //     using (var call = client.RecordPoints())
        //     {
        //         foreach (var p in pointCache)
        //         {
        //             await call.RequestStream.WriteAsync(p.Value);

        //         }

        //         await call.RequestStream.CompleteAsync();

        //         Confirmation result = await call.ResponseAsync;
        //         pointCache.Clear();
        //         isCallComplete = true;
        //     }
        // }
        // catch (RpcException e)
        // {
        //     Debug.Log("RPC failed");
        //     throw;
        // }
    }
    public void SaveMap()
    {
        client.SaveMap(new Empty { });
    }

    public void recoverMap()
    {
        LoadMap();
    }

    public async Task LoadMap()
    {
        List<Point> pointCloud = new List<Point>();
        try
        {

            using (var call = client.LoadMap(new Empty { }))
            {
                var responseStream = call.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    Point pcpoint = responseStream.Current;
                    pointCloud.Add(pcpoint);
                }
            }
            Debug.Log("Map recovered");
        }
        catch (RpcException e)
        {
            Debug.Log("RPC failed " + e);
            throw;
        }
    }
}
