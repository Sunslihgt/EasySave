using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using EasySave.Monitoring.ViewModels;
using System.IO;

namespace EasySave.Monitoring.Models
{
    public class Client
    {
        private TcpClient client = new TcpClient();
        private NetworkStream? stream;

        private MainWindowViewModel mainWindowViewModel;

        public Client(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            Task.Run(ClientMain);
        }

        public async Task ClientMain()
        {
            client = new TcpClient();

            try
            {
                await client.ConnectAsync(IPAddress.Loopback, 8888);
                Console.WriteLine("Connected to the server!");

                stream = client.GetStream();

                // Start listening for incoming messages
                _ = Task.Run(ListenForMessages);

                // Allow sending messages from the console
                while (client.Connected)
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
        }

        public async Task ListenForMessages()
        {
            if (stream == null) return;
            StreamReader reader = new StreamReader(stream, new UTF8Encoding(false));

            while (client.Connected)
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
            client.Close();
        }

        private void ProcessCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            if (command.StartsWith("STATE|"))
            {
                string stateString = command.Split("STATE|")[1];
                Console.WriteLine($"Received state");

                try
                {
                    List<Save>? saves = JsonConvert.DeserializeObject<List<Save>>(stateString);
                    if (saves != null)
                    {
                        foreach (Save save in saves)
                        {
                            Console.WriteLine($"Save: {save.Name} has Transfering={save.Transfering}, Pause={save.PauseTransfer}");
                        }
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

        public bool IsConnected()
        {
            return client.Connected;
        }

        public void CreateSave(string SaveName, string SaveSource, string SaveDestination, string MySaveType)
        {
            SendMessage($"CREATE|{SaveName}|{SaveSource}|{SaveDestination}|{MySaveType}");
        }
    }
}
