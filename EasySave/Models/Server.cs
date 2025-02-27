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
        private List<Client> clients = new List<Client>();

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
                TcpClient tcpClient = await server.AcceptTcpClientAsync();
                Client client = new Client(tcpClient);

                lock (clients) {
                    clients.Add(client);
                }

                ConsoleLogger.Log($"New client connected: {tcpClient.Client.RemoteEndPoint}", ConsoleColor.Cyan);

                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(Client client)
        {
            try
            {
                NetworkStream stream = client.TcpClient.GetStream();
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
                ConsoleLogger.LogError(ex.Message);
            }
            finally
            {
                lock (clients) {
                    clients.Remove(client);
                    client.Close();
                }
                
                ConsoleLogger.Log($"Client disconnected", ConsoleColor.Yellow);
            }
        }

        private void ProcessCommand(Client client, string command)
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
                                bool createSuccess = mainWindowViewModel.CreateSaveThreaded(name, source, destination, saveType);
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
                        mainWindowViewModel.DeleteSaveThreaded(mainWindowViewModel.Saves.First((save) => save.Name == data));
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
                case "LOGIN":
                    if (data.Length > 0 && !client.LoggedIn)
                    {
                        client.Login(data);

                        if (client.LoggedIn)
                        {
                            ConsoleLogger.Log($"Login successful: {client.TcpClient.Client.RemoteEndPoint}", ConsoleColor.Cyan);
                            SendClient(client, $"LOGIN|SUCCESS");
                            SendState(client);
                        }
                        else
                        {
                            SendClient(client, $"LOGIN|FAILED", false);
                            ConsoleLogger.Log($"Login failed: {client.TcpClient.Client.RemoteEndPoint}", ConsoleColor.Cyan);
                        }
                    }
                    break;
            }
        }

        public void BroadcastState(string state, bool needLoggedIn = true)
        {
            if (string.IsNullOrEmpty(state))
            {
                return;
            }
            Broadcast($"STATE|{state}", needLoggedIn);
        }

        public void Broadcast(string message, bool needLoggedIn = true)
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (needLoggedIn && !client.LoggedIn) continue;
                    SendClient(client, message, needLoggedIn);
                }
            }
        }

        public void SendState(Client client, bool needLoggedIn = true)
        {
            if (needLoggedIn && !client.LoggedIn) return;

            string state = mainWindowViewModel.StateLogger.GetStateString(mainWindowViewModel.Saves.ToList());
            if (string.IsNullOrEmpty(state))
            {
                return;
            }
            SendClient(client, $"STATE|{state}", needLoggedIn);
        }

        private void SendClient(Client client, string message, bool needLoggedIn = true)
        {
            if (needLoggedIn && !client.LoggedIn) return;

            try
            {
                NetworkStream stream = client.TcpClient.GetStream();
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(message + "\n");

                if (message.Length > 70) ConsoleLogger.Log($"Sent {client.TcpClient.Client.RemoteEndPoint}: {message[0..70]}...", ConsoleColor.Cyan);
                else ConsoleLogger.Log($"Sent {client.TcpClient.Client.RemoteEndPoint}: {message}", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Send failed: {ex.Message}");
            }
        }
    }
}
