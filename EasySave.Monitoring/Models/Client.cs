using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using EasySave.Monitoring.ViewModels;

namespace EasySave.Monitoring.Models
{
    public class Client
    {
        private IPAddress serverIP = IPAddress.Loopback;
        private int serverPort = 8888;
        private string serverPasswordHash = "";

        private TcpClient? client;
        private NetworkStream? stream;

        private MainWindowViewModel mainWindowViewModel;

        public Client(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
        }

        public void Connect(IPAddress ipAddress, int port, string password)
        {
            if (ipAddress == null)
            {
                Console.WriteLine("Invalid IP address");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("No password provided");
                return;
            }

            serverIP = ipAddress;
            serverPort = port;
            serverPasswordHash = GetPasswordHash(password);

            mainWindowViewModel.ShowConnection("DISCONNECTED");
            Task.Run(ClientMain);
        }

        public async Task ClientMain()
        {
            try
            {
                mainWindowViewModel.ShowConnection("WAITING");

                client = new TcpClient();
                
                await client.ConnectAsync(serverIP, serverPort);
                mainWindowViewModel.ShowConnection("LOGGEDOUT");
                Console.WriteLine("Connected to the server!");

                stream = client.GetStream();

                // Start listening for incoming messages
                _ = Task.Run(ListenForMessages);

                // Allow sending messages from the console
                while (IsConnected())
                {
                    string message = Console.ReadLine() ?? "";
                    if (!string.IsNullOrEmpty(message))
                    {
                        SendMessage(message);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            mainWindowViewModel.ShowConnection("DISCONNECTED");
        }

        public async Task ListenForMessages()
        {
            if (stream == null) return;
            StreamReader reader = new StreamReader(stream, new UTF8Encoding(false));

            SendMessage($"LOGIN|{serverPasswordHash}");

            while (IsConnected())
            {
                try
                {
                    if (stream == null) break;

                    string message = await reader.ReadLineAsync() ?? "";
                    message = message.Trim().TrimStart('?');
                    if (message == "") continue;

                    if (message.Length > 70) Console.WriteLine($"[Server]: {message[0..70]}...");
                    else Console.WriteLine($"[Server]: {message}");

                    ProcessCommand(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine("Disconnected from server.");
            client?.Close();
        }

        private void ProcessCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            if (command.StartsWith("STATE|"))
            {
                string stateString = command.Split("STATE|")[1];

                try
                {
                    List<Save>? saves = JsonConvert.DeserializeObject<List<Save>>(stateString);
                    if (saves != null)
                    {
                        mainWindowViewModel.UpdateState(saves);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing state: {ex.Message}");
                }
            }
            else if (command.StartsWith("RESETCREATE|"))
            {
                mainWindowViewModel.ResetCreateSaveForm();
            }
            else if (command.StartsWith("LOGIN|"))
            {
                if (command.Split("LOGIN|")[1].Trim().Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    mainWindowViewModel.ShowConnection("CONNECTED");
                    Console.WriteLine("Login successful");
                }
                else
                {
                    mainWindowViewModel.ShowConnection("LOGGEDOUT");
                    Console.WriteLine("Login failed");
                }
            }
        }

        public void SendMessage(string message)
        {
            if (client == null || !client.Connected || stream == null)
            {
                Console.WriteLine("Client is not connected");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);

            Console.WriteLine($"[Client]: {message}");
        }

        public static string GetPasswordHash(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }

        public bool IsConnected()
        {
            return client != null && client.Connected;
        }

        public void CreateSave(string SaveName, string SaveSource, string SaveDestination, string MySaveType)
        {
            SendMessage($"CREATE|{SaveName}|{SaveSource}|{SaveDestination}|{MySaveType}");
        }
    }
}
