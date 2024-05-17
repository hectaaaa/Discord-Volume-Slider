using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DiscordVolumeMixer
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            AllocConsole(); // Allocates a console window for logging
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}