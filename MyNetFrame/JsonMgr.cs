using System.Text;
using System.Text.Json;

public static class JsonMgr
{
    private class Snapshot
    {
        public DateTime timeStamp{get;set;}
        public List<PlayerInfo> Players{get;set;} = new();
        public List<ObjectInfo> Objects{get;set;} = new();
    }
    public static void SaveSnapshot(string filePath, List<PlayerInfo> players, List<ObjectInfo> objects)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            var json = JsonSerializer.Serialize(new Snapshot
            {
                timeStamp = DateTime.Now,
                Players = players ?? new List<PlayerInfo>(),
                Objects = objects ?? new List<ObjectInfo>()
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true // 关键：序列化字段
            });
            var temp = filePath + ".tmp";
            File.WriteAllText(temp, json, Encoding.UTF8);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Replace(temp, filePath, null);
                }
                catch
                {
                    File.Delete(filePath);
                    File.Move(temp, filePath);
                }
            }
            else
            {
                File.Move(temp, filePath);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("保存快照时出错:" + e.Message);
        }
    }
    public static List<PlayerInfo> LoadPlayerSnapshot(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return new List<PlayerInfo>();
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var snapshot = JsonSerializer.Deserialize<Snapshot>(json, new JsonSerializerOptions
            {
                IncludeFields = true // 关键：反序列化字段
            });
            return snapshot?.Players ?? new List<PlayerInfo>();
        }
        catch (Exception e)
        {
            Console.WriteLine("加载快照时出错:" + e.Message);
            return new List<PlayerInfo>();
        }
    }
    public static List<ObjectInfo> LoadObjectSnapshot(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return new List<ObjectInfo>();
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var snapshot = JsonSerializer.Deserialize<Snapshot>(json, new JsonSerializerOptions
            {
                IncludeFields = true // 关键：反序列化字段
            });
            return snapshot?.Objects ?? new List<ObjectInfo>();
        }
        catch (Exception e)
        {
            Console.WriteLine("加载快照时出错:" + e.Message);
            return new List<ObjectInfo>();
        }
    }
}