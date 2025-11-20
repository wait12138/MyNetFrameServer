using System.Text;

public class NewClientJoinMsg : BaseMsg
{
    // 新加入客户端的稳定ID 通知其他客户端时方便他们维护
    public string clientID;
    public override int GetBytesNum()
    {
        return 8 + 4 + Encoding.UTF8.GetBytes(clientID).Length;
    }
    public override int Reading(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;
        clientID = ReadString(bytes, ref index);
        return index - beginIndex;
    }
    public override byte[] Writing()
    {
        int index = 0;
        byte[] bytes = new byte[GetBytesNum()];
        WriteInt(bytes, GetID(), ref index);
        WriteInt(bytes, 0, ref index);
        WriteString(bytes, clientID, ref index);
        return bytes;
    }
    public override int GetID()
    {
        return 1004;
    }
}