using System.Text;

public class OnlinePlayerListMsg : BaseMsg
{
    public List<string> onlinePlayerIDs = new List<string>();
    public override int GetBytesNum()
    {
        return 8 + 4 + onlinePlayerIDs.Sum(id => 4 + Encoding.UTF8.GetBytes(id).Length);
    }
    public override int Reading(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;
        onlinePlayerIDs = ReadListString(bytes, ref index);
        return index - beginIndex;
    }
    public override byte[] Writing()
    {
        int index = 0;
        byte[] bytes = new byte[GetBytesNum()];
        WriteInt(bytes, GetID(), ref index);
        WriteInt(bytes, 0, ref index);
        WriteInt(bytes, onlinePlayerIDs.Count, ref index);
        foreach (string id in onlinePlayerIDs)
        {
            WriteString(bytes, id, ref index);
        }
        return bytes;
    }
    public override int GetID()
    {
        return 1005;
    }
}