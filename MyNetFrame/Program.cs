using System;
using System.Windows.Forms;

class Program
{
    public static ServerSocket serverSocket;
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new UI.MainForm());
    }
}