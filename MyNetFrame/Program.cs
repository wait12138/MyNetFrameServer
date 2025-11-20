class Program
{
    public static ServerSocket serverSocket;
    static void Main(string[] args)
    {
        serverSocket = new ServerSocket();
        serverSocket.Start("127.0.0.1", 8080);
        while (true)
        {
            
        }
    }
}