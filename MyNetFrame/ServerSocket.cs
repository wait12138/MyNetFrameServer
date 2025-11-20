using System.Net;
using System.Net.Sockets;

public class ServerSocket
{
    private Socket socket;
    private bool isClose;
    // 正在连入的客户端字典 键：当前连接的key（ip + port） 值：Client
    public Dictionary<string, Client> clientDic = new Dictionary<string, Client>();
    // 曾经连入的客户端字典 键：唯一标识符（服务器下发的稳定id，这个稳定id是第一次连入时候生成的） 值：Client
    public Dictionary<string, Client> historyClientDic = new Dictionary<string, Client>();
    // 将玩家的各种信息存入字典 以便重连时恢复数据
    public Dictionary<string, PlayerInfo> playerInfoDic = new Dictionary<string, PlayerInfo>();
    // 将场景上物体信息存入字典 以便重连时恢复数据
    public Dictionary<string, ObjectInfo> objectInfoDic = new Dictionary<string, ObjectInfo>();
    private readonly string snapshotFilePath = Path.Combine(AppContext.BaseDirectory, "server_snapshot.json");
    private Timer snapshotTimer;
    public void Start(string ip, int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            socket.Bind(ipPoint);
            Console.WriteLine("UDP服务器启动成功，监听端口：" + port);
            // 加载快照文件 恢复历史玩家信息
            LoadSnapshot();
            isClose = false;
            // 接收消息
            ThreadPool.QueueUserWorkItem(ReceiveMsg);
            // 定时打印在线客户端信息
            ThreadPool.QueueUserWorkItem(ShowClientInfo);

            // 周期性快照（每10秒）
            snapshotTimer = new Timer(_ => SaveSnapshot(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            Console.WriteLine("UDP服务器启动失败:" + ex.Message);
        }
    }
    // 接收消息
    private void ReceiveMsg(object obj)
    {
        try
        {
            byte[] bytes = new byte[1024];
            EndPoint ipPoint = new IPEndPoint(IPAddress.Any, 0);
            while (!isClose)
            {
                if (socket.Available > 0)
                {
                    int receiveLength = 0;
                    lock (socket)
                    {
                        receiveLength = socket.ReceiveFrom(bytes, ref ipPoint);
                    }
                    var remoteEp = (IPEndPoint)ipPoint;
                    string clientID = remoteEp.Address.ToString() + remoteEp.Port;
                    if (receiveLength < 8) continue;
                    int msgID = BitConverter.ToInt32(bytes, 0);
                    
                    if (clientDic.ContainsKey(clientID))
                    {
                        clientDic[clientID].ReceiveMsg(bytes, remoteEp);
                    }
                    else
                    {
                        // 新端点第一次来包（可能是重连包，也可能是首次连接）
                        clientDic[clientID] = new Client(remoteEp.Address.ToString(), remoteEp.Port);
                        clientDic[clientID].ReceiveMsg(bytes, remoteEp);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("接收消息异常:" + ex.Message);
        }
    }
    public void AddClient(Client client)
    {
        if (!clientDic.ContainsKey(client.clientStrID))
        {
            clientDic.Add(client.clientStrID, client);
        }
    }
    public void RemoveClient(string clientID)
    {
        if (clientDic.TryGetValue(clientID, out Client c))
        {
            RemoveClient(c);
        }
    }
    public void RemoveClient(Client c)
    {
        if (c == null) return;
        if (clientDic.ContainsKey(c.clientStrID))
        {
            // 把移除的客户端信息存入历史记录字典（键：稳定ID）
            historyClientDic[c.stableID] = c;
            clientDic.Remove(c.clientStrID);
            Console.WriteLine("移除客户端 当前Key:" + c.clientStrID + " (稳定ID:" + c.stableID + ") 并存入历史记录");
        }
    }
    // 将历史客户端移回当前客户端字典 更新这个客户端对象的端点
    public void ReAddClient(string stableID, IPEndPoint newEndPoint)
    {
        if (historyClientDic.ContainsKey(stableID))
        {
            clientDic.Add(stableID, historyClientDic[stableID]);
            clientDic[stableID].clientIPandPort = newEndPoint;
            Console.WriteLine("历史客户端{0}已重新连接回当前客户端列表" + clientDic[stableID].clientStrID);
            historyClientDic.Remove(stableID);
        }
    }
    public void Close()
    {
        if (socket != null)
        {
            isClose = true;
            snapshotTimer?.Dispose();
            snapshotTimer = null;

            SaveSnapshot();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;
        }
    }
    // 发送消息给指定目标
    public void SendTo(BaseMsg msg, IPEndPoint ipPoint)
    {
        try
        {
            lock (socket)
            {
                socket.SendTo(msg.Writing(), ipPoint);
            }
        }
        catch (SocketException s)
        {
            Console.WriteLine("发消息出现问题" + s.SocketErrorCode + s.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("发送消息出问题（可能是序列化问题）" + e.Message);
        }
    }
    public void Broadcast(BaseMsg msg)
    {
        //广播消息 给谁广播
        foreach (Client c in clientDic.Values)
        {
            SendTo(msg, c.clientIPandPort);
        }
    }
    public void BroadcastExceptSender(BaseMsg msg, string senderStableID)
    {
        // 由于客户端是发送稳定ID 所以这里用稳定ID来排除发送者 因为ip + port形式的key如果是重连会改变 但稳定ID不会变
        foreach (Client c in clientDic.Values)
        {
            if (c.stableID != senderStableID)
            {
                SendTo(msg, c.clientIPandPort);
            }
        }
    }
    private void ShowClientInfo(object obj)
    {
        while (!isClose)
        {
            foreach (Client c in clientDic.Values)
            {
                Console.WriteLine("客户端(稳定ID)" + c.stableID + " 当前Key:" + c.clientStrID + " IP和端口" + c.clientIPandPort.ToString());
            }
            foreach (Client c in historyClientDic.Values)
            {
                Console.WriteLine("历史客户端(稳定ID)" + c.stableID + " 当前Key:" + c.clientStrID + " IP和端口" + c.clientIPandPort.ToString());
            }
            Thread.Sleep(10000);
        }
    }
    public List<string> GetOnlinePlayerList()
    {
        List<string> onlinePlayers = new List<string>();
        foreach (Client c in clientDic.Values)
        {
            onlinePlayers.Add(c.stableID);
        }
        return onlinePlayers;
    }
    // 保存快照
    private void SaveSnapshot()
    {
        try
        {
            List<PlayerInfo> playersSnapshot;
            lock (playerInfoDic)
            {
                playersSnapshot = new List<PlayerInfo>(playerInfoDic.Values);
            }
            List<ObjectInfo> objectsSnapshot;
            lock (objectInfoDic)
            {
                objectsSnapshot = new List<ObjectInfo>(objectInfoDic.Values);
            }
            JsonMgr.SaveSnapshot(snapshotFilePath, playersSnapshot, objectsSnapshot);
        }
        catch (Exception ex)
        {
            Console.WriteLine("保存快照出错: " + ex.Message);
        }
    }
    // 加载快照
    private void LoadSnapshot()
    {
        try
        {
            var players = JsonMgr.LoadPlayerSnapshot(snapshotFilePath);
            var objects = JsonMgr.LoadObjectSnapshot(snapshotFilePath);
            if (players.Count == 0) return;
            if (objects.Count == 0) return;
            lock (playerInfoDic)
            {
                playerInfoDic.Clear();
                foreach (var p in players)
                {
                    if (!string.IsNullOrEmpty(p.clientID))
                    {
                        playerInfoDic[p.clientID] = p;
                    }
                }
            }
            Console.WriteLine("已从快照恢复玩家数量: " + players.Count);
            lock (objectInfoDic)
            {
                objectInfoDic.Clear();
                foreach (var o in objects)
                {
                    objectInfoDic[o.objectID] = o;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("加载快照时出错:" + e.Message);
        }
    }
}