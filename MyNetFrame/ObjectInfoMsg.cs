using System.Text;

public class ObjectInfoMsg : BaseMsg
{
    public string objectID;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public override int GetBytesNum()
    {
        return 8 + 4 + Encoding.UTF8.GetBytes(objectID).Length + 4 * 6;
    }
    public override int Reading(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;
        objectID = ReadString(bytes, ref index);
        posX = ReadFloat(bytes, ref index);
        posY = ReadFloat(bytes, ref index);
        posZ = ReadFloat(bytes, ref index);
        rotX = ReadFloat(bytes, ref index);
        rotY = ReadFloat(bytes, ref index);
        rotZ = ReadFloat(bytes, ref index);
        return index - beginIndex;
    }
    public override byte[] Writing()
    {
        int index = 0;
        byte[] bytes = new byte[GetBytesNum()];
        WriteInt(bytes, GetID(), ref index);
        WriteInt(bytes, 0, ref index);
        WriteString(bytes, objectID, ref index);
        WriteFloat(bytes, posX, ref index);
        WriteFloat(bytes, posY, ref index);
        WriteFloat(bytes, posZ, ref index);
        WriteFloat(bytes, rotX, ref index);
        WriteFloat(bytes, rotY, ref index);
        WriteFloat(bytes, rotZ, ref index);
        return bytes;
    }
    public override int GetID()
    {
        return 3001;
    }
}