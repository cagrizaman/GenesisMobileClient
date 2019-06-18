using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Grpc.Core;
using GoogleARCore;
public class GrpcClient : MonoBehaviour
{
    Channel channel;
    GenesisSimulator.GenesisSimulatorClient client;
    public string host;
    public string port;
    public bool connected = false;

    private List<int> pointIds;

    private List<PointCloudPoint> pointList;
    private bool readyForNextFrame = true;
    // Start is called before the first frame update
    void Start()
    {
        pointIds = new List<int>();
        pointList = new List<PointCloudPoint>();
        channel = new Channel(host + ":" + port, ChannelCredentials.Insecure);
        client = new GenesisSimulator.GenesisSimulatorClient(channel);
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

            var reply = client.UpdateCamera(update);
            if (Session.Status != SessionStatus.Tracking)
                return;

            if (Frame.PointCloud.PointCount > 0 && Frame.PointCloud.IsUpdatedThisFrame && readyForNextFrame)
            {
                for (int i = 0; i < Frame.PointCloud.PointCount; i++)
                {
                    PointCloudPoint pcpoint = Frame.PointCloud.GetPointAsStruct(i);
                    if (pcpoint.Id > 0)
                    {
                        pointList.Add(pcpoint);
                    }
                }
                if (pointList.Count > 0)
                {
                    sendPoints();
                    readyForNextFrame = false;

                }
            }
        }
    }


    public async Task<string> sendPoints()
    {
        try
        {
            using (var call = client.RecordPoints())
            {
                for (int i = 0; i < pointList.Count; i++)
                {
                    PointCloudPoint pcpoint = Frame.PointCloud.GetPointAsStruct(i);
                    await call.RequestStream.WriteAsync(new Point
                    {
                        X = pcpoint.Position.x,
                        Y = pcpoint.Position.y,
                        Z = pcpoint.Position.z,
                        Id = pcpoint.Id
                    });

                }
                await call.RequestStream.CompleteAsync();

                Confirmation result = await call.ResponseAsync;
                pointList.Clear();
                readyForNextFrame = true;
                return result.Message;
            }
        }
        catch (RpcException e)
        {
            Debug.Log("RPC failed");
            throw;
        }
    }

    public void SaveMap()
    {
        client.SaveMap(new Empty { });
    }

    public void recoverMap(){
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
