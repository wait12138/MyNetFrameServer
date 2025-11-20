using System.Net;

public class Client
{
    public IPEndPoint clientIPandPort;
    public string clientStrID;
    public string stableID;
    public Client(string ip, int port)
    {
        clientStrID = ip + port;
        stableID = clientStrID;
        clientIPandPort = new IPEndPoint(IPAddress.Parse(ip), port);
    }
    public void ReceiveMsg(byte[] bytes, IPEndPoint remoteEp)
    {
        var state = (bytes, remoteEp);
        ThreadPool.QueueUserWorkItem(ReceiveHandle, state);
    }
    private void ReceiveHandle(object obj)
    {
        try
        {
            //取出传进来的字节
            var (bytes, remoteEp) = ((byte[], IPEndPoint))obj;
            int nowIndex = 0;
            //先处理 ID
            int msgID = BitConverter.ToInt32(bytes, nowIndex);
            nowIndex += 4;
            //再处理 长度
            int msgLength = BitConverter.ToInt32(bytes, nowIndex);
            nowIndex += 4;
            //再解析消息体
            switch (msgID)
            {
                case 1000:
                    FirstConnectMsg firstConnectMsg = new FirstConnectMsg();
                    firstConnectMsg.Reading(bytes, nowIndex);
                    Console.WriteLine("收到新客户端连接请求:" + this.clientStrID);
                    // 首次连接 把该客户端写入历史字典 以达到永久维护
                    if (!Program.serverSocket.historyClientDic.ContainsKey(stableID))
                    {
                        Program.serverSocket.historyClientDic[stableID] = this;
                    }
                    // 回复客户端 已经连接成功 并且发送唯一ID
                    FirstReplyMsg firstReplyMsg = new FirstReplyMsg();
                    firstReplyMsg.clientID = stableID;
                    Program.serverSocket.SendTo(firstReplyMsg, this.clientIPandPort);
                    // 告知其它客户端 有新客户端加入
                    NewClientJoinMsg newClientJoinMsg = new NewClientJoinMsg();
                    newClientJoinMsg.clientID = stableID;
                    Program.serverSocket.BroadcastExceptSender(newClientJoinMsg, this.stableID);
                    // 给当前客户端发送在线玩家列表
                    OnlinePlayerListMsg onlinePlayerListMsg = new OnlinePlayerListMsg();
                    onlinePlayerListMsg.onlinePlayerIDs = Program.serverSocket.GetOnlinePlayerList();
                    Program.serverSocket.SendTo(onlinePlayerListMsg, this.clientIPandPort);
                    // 给这个客户端发送玩家的信息 以便同步场景（加锁做快照）
                    List<PlayerInfo> snapshotPlayers0;
                    lock (Program.serverSocket.playerInfoDic)
                    {
                        snapshotPlayers0 = new List<PlayerInfo>(Program.serverSocket.playerInfoDic.Values);
                    }
                    foreach (var info in snapshotPlayers0)
                    {
                        PlayerInfoMsg playerInfoMsg1 = new PlayerInfoMsg();
                        playerInfoMsg1.clientID = info.clientID;
                        playerInfoMsg1.posX = info.posX;
                        playerInfoMsg1.posY = info.posY;
                        playerInfoMsg1.posZ = info.posZ;
                        playerInfoMsg1.rotX = info.rotX;
                        playerInfoMsg1.rotY = info.rotY;
                        playerInfoMsg1.rotZ = info.rotZ;
                        Program.serverSocket.SendTo(playerInfoMsg1, this.clientIPandPort);
                        Console.WriteLine("发送" + info.clientID + "玩家信息给新连接客户端" + this.stableID);
                    }
                    // 给这个客户端发送物体的信息 以便同步场景（加锁做快照）
                    List<ObjectInfo> snapshotObjects0;
                    lock (Program.serverSocket.objectInfoDic)
                    {
                        snapshotObjects0 = new List<ObjectInfo>(Program.serverSocket.objectInfoDic.Values);
                    }
                    foreach (var info in snapshotObjects0)
                    {
                        ObjectInfoMsg objectInfoMsg1 = new ObjectInfoMsg();
                        objectInfoMsg1.objectID = info.objectID;
                        objectInfoMsg1.posX = info.posX;
                        objectInfoMsg1.posY = info.posY;
                        objectInfoMsg1.posZ = info.posZ;
                        objectInfoMsg1.rotX = info.rotX;
                        objectInfoMsg1.rotY = info.rotY;
                        objectInfoMsg1.rotZ = info.rotZ;
                        Program.serverSocket.SendTo(objectInfoMsg1, this.clientIPandPort);
                        Console.WriteLine("发送" + info.objectID + "物体信息给新连接客户端" + this.stableID);
                    }
                    break;
                case 1002:
                    ReConnectMsg reConnectMsg = new ReConnectMsg();
                    reConnectMsg.Reading(bytes, nowIndex);
                    string oldClientID = reConnectMsg.clientID;
                    string newClientID = remoteEp.Address.ToString() + remoteEp.Port;
                    // 历史中存在 => 合并历史客户端并更新端点
                    if (Program.serverSocket.historyClientDic.ContainsKey(oldClientID))
                    {
                        Console.WriteLine("客户端{0}请求重连，允许重连", oldClientID);
                        Client oldClient = Program.serverSocket.historyClientDic[oldClientID];
                        if (Program.serverSocket.clientDic.ContainsKey(newClientID))
                        {
                            Program.serverSocket.clientDic.Remove(newClientID);
                        }
                        oldClient.clientIPandPort = new IPEndPoint(IPAddress.Parse(remoteEp.Address.ToString()), remoteEp.Port);
                        oldClient.clientStrID = newClientID;
                        Program.serverSocket.clientDic[newClientID] = oldClient;

                        // 回应重连成功（保持稳定ID）
                        ReConnectReplyMsg reConnectReplyMsg = new ReConnectReplyMsg();
                        reConnectReplyMsg.clientID = oldClient.stableID;
                        Program.serverSocket.SendTo(reConnectReplyMsg, remoteEp);

                        // 在线列表与入场广播
                        OnlinePlayerListMsg onlinePlayerListMsg2 = new OnlinePlayerListMsg();
                        onlinePlayerListMsg2.onlinePlayerIDs = Program.serverSocket.GetOnlinePlayerList();
                        Program.serverSocket.SendTo(onlinePlayerListMsg2, oldClient.clientIPandPort);

                        NewClientJoinMsg newClientJoinMsg2 = new NewClientJoinMsg();
                        newClientJoinMsg2.clientID = oldClient.stableID;
                        Program.serverSocket.BroadcastExceptSender(newClientJoinMsg2, oldClient.stableID);

                        // 同步历史位姿
                        List<PlayerInfo> snapshotPlayers1;
                        lock (Program.serverSocket.playerInfoDic)
                        {
                            snapshotPlayers1 = new List<PlayerInfo>(Program.serverSocket.playerInfoDic.Values);
                        }
                        foreach (var info in snapshotPlayers1)
                        {
                            PlayerInfoMsg playerInfoMsg1 = new PlayerInfoMsg();
                            playerInfoMsg1.clientID = info.clientID;
                            playerInfoMsg1.posX = info.posX;
                            playerInfoMsg1.posY = info.posY;
                            playerInfoMsg1.posZ = info.posZ;
                            playerInfoMsg1.rotX = info.rotX;
                            playerInfoMsg1.rotY = info.rotY;
                            playerInfoMsg1.rotZ = info.rotZ;
                            Program.serverSocket.SendTo(playerInfoMsg1, this.clientIPandPort);
                            Console.WriteLine("发送" + info.clientID + "玩家信息给重连客户端" + this.stableID);
                        }
                        List<ObjectInfo> snapshotObjects1;
                        lock (Program.serverSocket.objectInfoDic)
                        {
                            snapshotObjects1 = new List<ObjectInfo>(Program.serverSocket.objectInfoDic.Values);
                        }
                        foreach (var info in snapshotObjects1)
                        {
                            ObjectInfoMsg objectInfoMsg1 = new ObjectInfoMsg();
                            objectInfoMsg1.objectID = info.objectID;
                            objectInfoMsg1.posX = info.posX;
                            objectInfoMsg1.posY = info.posY;
                            objectInfoMsg1.posZ = info.posZ;
                            objectInfoMsg1.rotX = info.rotX;
                            objectInfoMsg1.rotY = info.rotY;
                            objectInfoMsg1.rotZ = info.rotZ;
                            Program.serverSocket.SendTo(objectInfoMsg1, this.clientIPandPort);
                            Console.WriteLine("发送" + info.objectID + "物体信息给重连客户端" + this.stableID);
                        }
                    }
                    else
                    {
                        // 历史不存在 => 尝试用快照识别为“已存在玩家”的重连
                        bool knownBySnapshot;
                        lock (Program.serverSocket.playerInfoDic)
                        {
                            knownBySnapshot = Program.serverSocket.playerInfoDic.ContainsKey(oldClientID);
                        }

                        // 统一回 1003（重连回复），并沿用旧稳定ID
                        this.stableID = oldClientID;
                        this.clientIPandPort = new IPEndPoint(IPAddress.Parse(remoteEp.Address.ToString()), remoteEp.Port);
                        this.clientStrID = newClientID;

                        // 放入当前/历史字典，建立维护
                        Program.serverSocket.clientDic[newClientID] = this;
                        Program.serverSocket.historyClientDic[this.stableID] = this;

                        ReConnectReplyMsg reReply = new ReConnectReplyMsg();
                        reReply.clientID = this.stableID;
                        Program.serverSocket.SendTo(reReply, remoteEp);

                        // 在线列表与入场广播（无论是否来自快照，其他客户端都需要感知）
                        OnlinePlayerListMsg onlineList = new OnlinePlayerListMsg();
                        onlineList.onlinePlayerIDs = Program.serverSocket.GetOnlinePlayerList();
                        Program.serverSocket.SendTo(onlineList, this.clientIPandPort);

                        NewClientJoinMsg joinMsg = new NewClientJoinMsg();
                        joinMsg.clientID = this.stableID;
                        Program.serverSocket.BroadcastExceptSender(joinMsg, this.stableID);

                        // 若存在快照，给该客户端补发历史位姿（便于立即恢复场景）
                        if (knownBySnapshot)
                        {
                            List<PlayerInfo> snapshotPlayers2;
                            lock (Program.serverSocket.playerInfoDic)
                            {
                                snapshotPlayers2 = new List<PlayerInfo>(Program.serverSocket.playerInfoDic.Values);
                            }
                            foreach (var info in snapshotPlayers2)
                            {
                                PlayerInfoMsg pMsg = new PlayerInfoMsg();
                                pMsg.clientID = info.clientID;
                                pMsg.posX = info.posX;
                                pMsg.posY = info.posY;
                                pMsg.posZ = info.posZ;
                                pMsg.rotX = info.rotX;
                                pMsg.rotY = info.rotY;
                                pMsg.rotZ = info.rotZ;
                                Program.serverSocket.SendTo(pMsg, this.clientIPandPort);
                                Console.WriteLine("发送" + info.clientID + "玩家信息给重连客户端" + this.stableID + "（来自快照）");
                            }
                            List<ObjectInfo> snapshotObjects2;
                            lock (Program.serverSocket.objectInfoDic)
                            {
                                snapshotObjects2 = new List<ObjectInfo>(Program.serverSocket.objectInfoDic.Values);
                            }
                            foreach (var info in snapshotObjects2)
                            {
                                ObjectInfoMsg oMsg = new ObjectInfoMsg();
                                oMsg.objectID = info.objectID;
                                oMsg.posX = info.posX;
                                oMsg.posY = info.posY;
                                oMsg.posZ = info.posZ;
                                oMsg.rotX = info.rotX;
                                oMsg.rotY = info.rotY;
                                oMsg.rotZ = info.rotZ;
                                Program.serverSocket.SendTo(oMsg, this.clientIPandPort);
                                Console.WriteLine("发送" + info.objectID + "物体信息给重连客户端" + this.stableID + "（来自快照）");
                            }
                        }
                        else
                        {
                            Console.WriteLine("客户端{0}请求重连，既无历史也无快照，按首次接入建立维护（仍复用ID）", oldClientID);
                        }
                    }
                    break;
                case 2001:
                    // 处理玩家相关信息 也就是转发给除了发送者以外的其他客户端
                    PlayerInfoMsg playerInfoMsg = new PlayerInfoMsg();
                    playerInfoMsg.Reading(bytes, nowIndex);
                    // Console.WriteLine("收到{0}玩家信息（稳定ID）" + this.stableID + "，位置：" + playerInfoMsg.posX + "," + playerInfoMsg.posY + "," + playerInfoMsg.posZ);
                    // Console.WriteLine("收到{0}玩家信息（稳定ID）" + this.stableID + "，旋转：" + playerInfoMsg.rotX + "," + playerInfoMsg.rotY + "," + playerInfoMsg.rotZ);
                    // 转发给其他客户端
                    Program.serverSocket.BroadcastExceptSender(playerInfoMsg, this.stableID);
                    // 将信息存入服务器玩家信息字典 以便重连恢复数据
                    PlayerInfo playerInfo = new PlayerInfo()
                    {
                        clientID = this.stableID,
                        posX = playerInfoMsg.posX,
                        posY = playerInfoMsg.posY,
                        posZ = playerInfoMsg.posZ,
                        rotX = playerInfoMsg.rotX,
                        rotY = playerInfoMsg.rotY,
                        rotZ = playerInfoMsg.rotZ  
                    };
                    lock (Program.serverSocket.playerInfoDic)
                    {
                        Program.serverSocket.playerInfoDic[this.stableID] = playerInfo;
                    }
                    break;
                case 3001:
                    // 处理物体信息 也就是转发给除了发送者以外的其他客户端
                    ObjectInfoMsg objectInfoMsg = new ObjectInfoMsg();
                    objectInfoMsg.Reading(bytes, nowIndex);

                    // 防止将玩家稳定ID误当作物体ID
                    if (Program.serverSocket.clientDic.Values.Any(c => c.stableID == objectInfoMsg.objectID) ||
                        Program.serverSocket.historyClientDic.ContainsKey(objectInfoMsg.objectID))
                    {
                        Console.WriteLine("忽略疑似玩家ID的物体更新包:" + objectInfoMsg.objectID);
                        break;
                    }

                    Program.serverSocket.BroadcastExceptSender(objectInfoMsg, this.stableID);
                    // 未来会增加物体状态的服务器存储功能
                    ObjectInfo objectInfo = new ObjectInfo()
                    {
                        objectID = objectInfoMsg.objectID,
                        posX = objectInfoMsg.posX,
                        posY = objectInfoMsg.posY,
                        posZ = objectInfoMsg.posZ,
                        rotX = objectInfoMsg.rotX,
                        rotY = objectInfoMsg.rotY,
                        rotZ = objectInfoMsg.rotZ  
                    };
                    lock (Program.serverSocket.objectInfoDic)
                    {
                        Program.serverSocket.objectInfoDic[objectInfoMsg.objectID] = objectInfo;
                    }
                    break;
                case 9999:
                    // 玩家退出消息
                    QuitMsg quitMsg = new QuitMsg();
                    // 有一个客户端玩家退出 需要通知其他客户端 以便销毁该玩家的游戏对象
                    // 新定义一种消息类型 内容是退出客户端的稳定ID
                    QuitPlayerIDMsg quitPlayerIDMsg = new QuitPlayerIDMsg();
                    quitPlayerIDMsg.clientID = this.stableID;
                    Program.serverSocket.BroadcastExceptSender(quitPlayerIDMsg, this.stableID);
                    Program.serverSocket.RemoveClient(this.clientStrID);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("处理客户端消息出错:" + ex.Message);
        }
    }
}