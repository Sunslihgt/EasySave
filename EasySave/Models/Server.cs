using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using EasySave.ViewModels;

namespace EasySave.Models
{
    public class Server
    {
        private TcpListener server;
        private bool running = false;
        private List<TcpClient> clients = new List<TcpClient>();

        private MainWindowViewModel mainWindowViewModel;

        public Server(MainWindowViewModel mainWindowViewModel, int port = 8888) {
            this.mainWindowViewModel = mainWindowViewModel;

            server = new TcpListener(IPAddress.Any, port);
            StartServer();
        }

        public void StartServer()
        {
            server.Start();
            running = true;
            ConsoleLogger.Log($"Server started on port {server.LocalEndpoint}", ConsoleColor.Cyan);

            Task.Run(() => ListenForClients());
        }

        private async Task ListenForClients()
        {
            while (running)
            {
                var client = await server.AcceptTcpClientAsync();

                lock (clients) {
                    clients.Add(client);
                }

                ConsoleLogger.Log($"New client connected: {client.Client.RemoteEndPoint}", ConsoleColor.Cyan);

                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, new UTF8Encoding(false));
                StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                SendState(client);

                while (true)
                {
                    string request = await reader.ReadLineAsync() ?? "";
                    if (request == "") continue;

                    ConsoleLogger.Log($"[Client]: {request}", ConsoleColor.Cyan);
                    ProcessCommand(client, request);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex.ToString());
            }
            finally
            {
                lock (clients) {
                    clients.Remove(client);
                }
                client.Close();
                
                ConsoleLogger.Log($"Client disconnected", ConsoleColor.Yellow);
            }
        }

        private void ProcessCommand(TcpClient client, string command)
        {
            if (string.IsNullOrEmpty(command) || !command.Contains('|') || command.IndexOf('|') + 1 > command.Length)
            {
                ConsoleLogger.LogError($"Invalid command: {command}");
                return;
            }

            string operation = command[0..command.IndexOf('|')].Trim().ToUpper();
            string data = command[(command.IndexOf('|') + 1)..].Trim();

            switch (operation)
            {
                case "CREATE":
                    ConsoleLogger.Log(data, ConsoleColor.DarkCyan);
                    if (data.Length > 0 && data.Count(c => c == '|') == 3)
                    {
                        string name = data.Split('|')[0];
                        string source = data.Split('|')[1];
                        string destination = data.Split('|')[2];
                        string typeString = data.Split('|')[3];

                        ConsoleLogger.Log($"{name} {source} {destination} {typeString}", ConsoleColor.DarkCyan);
                        if (Enum.TryParse(typeString, out Save.SaveType saveType))
                        {
                            ConsoleLogger.Log($"{saveType}", ConsoleColor.DarkCyan);
                            if (name.Length > 0 && source.Length > 0 && destination.Length > 0)
                            {
                                bool createSuccess = mainWindowViewModel.CreateSave(name, source, destination, saveType);
                                ConsoleLogger.Log($"CreateSucess={createSuccess}", ConsoleColor.DarkCyan);
                                if (createSuccess)
                                {
                                    SendClient(client, $"RESETCREATE|");
                                }
                            }
                        }
                    }
                    break;
                case "UPLOAD":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.UpdateSave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
                case "DOWNLOAD":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.LoadSave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
                case "DELETE":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.DeleteSave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
                case "PAUSE":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.PauseSave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
                case "RESUME":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.PlaySave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
                case "ABORT":
                    if (data.Length > 0 && mainWindowViewModel.Saves.Any((save) => save.Name == data))
                    {
                        mainWindowViewModel.StopSave(mainWindowViewModel.Saves.First((save) => save.Name == data));
                    }
                    break;
            }
        }

        public void SendState(string state = "")
        {
            if (string.IsNullOrEmpty(state))
            {
                state = mainWindowViewModel.StateLogger.GetStateString(mainWindowViewModel.Saves.ToList());
            }
            Broadcast($"STATE|{state}");
        }

        public void SendState(TcpClient client, string state = "")
        {
            if (string.IsNullOrEmpty(state))
            {
                state = mainWindowViewModel.StateLogger.GetStateString(mainWindowViewModel.Saves.ToList());
            }
            SendClient(client, $"STATE|{state}");
        }

        public void Broadcast(string message)
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    SendClient(client, message);
                }
            }
        }

        private void SendClient(TcpClient client, string message)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(message + "\n");

                if (message.Length > 70) ConsoleLogger.Log($"Sent {client.Client.RemoteEndPoint}: {message[0..70]}...", ConsoleColor.Cyan);
                else ConsoleLogger.Log($"Sent {client.Client.RemoteEndPoint}: {message}", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Send failed: {ex.Message}");
            }
        }
    }
}
