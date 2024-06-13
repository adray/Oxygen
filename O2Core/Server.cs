using System.Diagnostics;
using System.Net.Sockets;

namespace Oxygen
{
    public class ServerException : Exception
    {
        public ServerException(string message, Exception inner) : base(message, inner)
        {
            
        }
    }

    public class Request
    {
        public Message Message { get; }
        public Client Client { get; }

        public Request(Message message, Client client)
        {
            this.Message = message;
            this.Client = client;
        }

        public void Send(Message message)
        {
            message.Id = this.Message.Id;
            Client.Send(message);
        }
    }

    public class ClientWaitHandle : EventWaitHandle
    {
        private int refCount;

        public ClientWaitHandle(bool initialState, EventResetMode mode) : base(initialState, mode)
        {
        }

        public ClientWaitHandle(bool initialState, EventResetMode mode, string? name) : base(initialState, mode, name)
        {
        }

        public ClientWaitHandle(bool initialState, EventResetMode mode, string? name, out bool createdNew) : base(initialState, mode, name, out createdNew)
        {
        }

        public void AddRef()
        {
            Interlocked.Increment(ref refCount);
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref refCount) == 0)
            {
                this.Dispose();
            }
        }
    }

    public class Client
    {
        private readonly ClientWaitHandle waitHandle;
        private readonly object msgLock = new object();
        private readonly Queue<Message> msgs;
        private readonly Dictionary<string, object> properies = new Dictionary<string, object>();
        private readonly long id;
        private bool connected;

        public Client(Queue<Message> msgs, object msgLock, ClientWaitHandle waitHandle, long id)
        {
            this.msgs = msgs;
            this.msgLock = msgLock;
            this.waitHandle = waitHandle;
            this.id = id;
            this.connected = true;
            this.waitHandle.AddRef();
        }

        public long ID => this.id;

        /// <summary>
        /// Callable on the event queue thread.
        /// </summary>
        internal void Disconnect()
        {
            this.waitHandle.Release();
            Interlocked.MemoryBarrier();
            this.connected = false;
        }

        /// <summary>
        /// Sends a message, safe to be called from any thread. 
        /// </summary>
        /// <param name="msg">The message to send.</param>
        internal void Send(Message msg)
        {
            if (this.connected)
            {
                Logger.Instance.Log(">> {0}/{1} {2} bytes", msg.NodeName, msg.MessageName, msg.Length);

                lock (msgLock)
                {
                    this.msgs.Enqueue(msg);
                }

                // Sets the wait handle for Read thread to consume the message queue.
                waitHandle.Set();
            }
        }

        public void RemoveProperty(string name)
        {
            properies.Remove(name);
        }

        public void SetProperty(string name, object value)
        {
            properies[name] = value;
        }

        public object? GetProperty(string name)
        {
            properies.TryGetValue(name, out object? result);
            return result;
        }
    }

    public class Server
    {
        private class ClientConnection
        {
            public bool Running { get; set; }
            public TcpClient Connection { get; private set; }
            public Client Client { get; private set; }
            public ClientWaitHandle MsgHandle { get; private set; }
            public Queue<Message> Messages { get; private set; }
            public object MsgLock { get; private set; }

            public ClientConnection(TcpClient connection, Client client, ClientWaitHandle msgHandle, object msgLock, Queue<Message> messages)
            {
                Connection = connection;
                Client = client;
                MsgHandle = msgHandle;
                MsgLock = msgLock;
                Messages = messages;
            }

            public void ExitClientThread()
            {
                MsgHandle.Release();
            }
        }

        private class ServerTimer
        {
            public Node? Node { get; set; }
            public NodeTimer? NodeTimer { get; set; }
            public Timer? Timer { get; set; }
        }

        private int port;
        private bool running;
        private object clientLock = new object();
        private object eventLock = new object();
        private readonly List<ClientConnection> clients = new List<ClientConnection>();
        private readonly List<Node> nodes = new List<Node>();
        private Queue<Event> events = new Queue<Event>();
        private EventWaitHandle eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private List<ServerTimer> timers = new List<ServerTimer>();

        private readonly GaugeMetric peakEventTime = new GaugeMetric("oxygen_server_peak_event_time", string.Empty);
        private readonly CounterMetric eventsProcessed = new CounterMetric("oxygen_server_events_processed_counter", string.Empty);

        public Server(int port)
        {
            this.port = port;
        }

        public void AddNode(Node node)
        {
            if (running)
            {
                throw new InvalidOperationException();
            }

            this.nodes.Add(node);

            foreach (var timer in node.Timers)
            {
                var serverTimer = new ServerTimer() { Node = node, NodeTimer = timer };
                serverTimer.Timer = new Timer(OnTimer, serverTimer, 0, serverTimer.NodeTimer.ElapsedMs);

                this.timers.Add(serverTimer);

                Log("Timer registered for {0}", node.Name);
            }

            node.AddMetric(peakEventTime);
            node.AddMetric(eventsProcessed);
        }

        private void OnTimer(object? state)
        {
            var timer = state as ServerTimer;

            if (timer != null)
            {
                QueueEvent(new TimerEvent(timer.NodeTimer, timer.Node));
            }
        }

        private void OnDisconnect(ClientConnection cli)
        {
            // Disconnected
            cli.Running = false;
            bool removed;
            lock (this.clientLock)
            {
                removed = this.clients.Remove(cli);
            }

            if (removed)
            {
                cli.MsgHandle.Set();
                QueueEvent(new DisconnectionEvent(cli.Client));
            }
        }

        private void QueueEvent(Event e)
        {
            lock (this.eventLock)
            {
                this.events.Enqueue(e);
                this.eventHandle.Set();
            }
        }

        private int ReadFromStream(ClientConnection cli, NetworkStream stream, byte[] buffer, int index, int size)
        {
            bool success = true;

            if (stream != null)
            {
                while (cli.Running && index < size)
                {
                    int count = 0;
                    try
                    {
                        count = stream.Read(buffer, index, size);
                    }
                    catch (Exception)
                    {
                        success = false;
                        OnDisconnect(cli);
                    }

                    index += count;
                }
            }

            return success ? index : -1;
        }

        private void WriteToStream(ClientConnection cli, NetworkStream stream, byte[] buffer)
        {
            try
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                OnDisconnect(cli);
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
            Logger.Instance.Log(message);
        }

        private void Log(string message, params object[] args)
        {
            Console.WriteLine(message, args);
            Logger.Instance.Log(message, args);
        }

        const int INCOMING_HEADER_SIZE = 8;

        private void ClientReadThread(object? state)
        {
            var cli = state as ClientConnection;

            if (cli != null)
            {
                cli.MsgHandle.AddRef();
                var stream = cli.Connection.GetStream();

                byte[] buffer = new byte[2048 * 32];
                while (cli.Running)
                {
                    // Read header
                    int index = ReadFromStream(cli, stream, buffer, 0, INCOMING_HEADER_SIZE);
                    if (index > -1)
                    {
                        //Log($"Header Index {index}");

                        //string bufferString = string.Empty;
                        //for (int i = 0; i < index; i++)
                        //{
                        //    bufferString += buffer[i] + " ";
                        //}
                        //Log(bufferString);

                        int len = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
                        int id = buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24);
                        index -= INCOMING_HEADER_SIZE;
                        if (index <= len && len <= buffer.Length)
                        {
                            Array.Copy(buffer, INCOMING_HEADER_SIZE, buffer, 0, len - INCOMING_HEADER_SIZE);

                            index = ReadFromStream(cli, stream, buffer, index, len - index);
                            //Log($"Index {index}");
                        }

                        if (index > 0)
                        {
                            //string bufferString = string.Empty;
                            //bufferString = string.Empty;
                            //for (int i = 0; i < len; i++)
                            //{
                            //    bufferString += buffer[i] + " ";
                            //}

                            //Log(bufferString);

                            byte[] copy = new byte[len];
                            Array.Copy(buffer, copy, len);

                            Message msg = new Message(copy);
                            msg.Id = id;
                            Request request = new Request(msg, cli.Client);

                            string name = msg.NodeName;
                            Logger.Instance.Log($"<< {name}/{msg.MessageName} {len} bytes");

                            QueueEvent(new MessageRecievedEvent(cli.Client, name, request, cli.MsgHandle));
                        }
                    }
                }

                cli.ExitClientThread();
                Log("Client disconnected");
            }
        }

        private void ClientWriteThread(object? state)
        {
            var cli = state as ClientConnection;

            if (cli != null)
            {
                cli.MsgHandle.AddRef();
                var stream = cli.Connection.GetStream();
                var messages = cli.Messages;

                while (cli.Running)
                {
                    // Wait for the signal that a message has been enqueued.
                    // This will also be set in the case the client has been disconnected.
                    cli.MsgHandle.WaitOne();

                    while (messages.Count > 0)
                    {
                        // Write response
                        Message response;
                        
                        lock (cli.MsgLock)
                        {
                            response = messages.Dequeue();
                        }
                        byte[] payload = response.GetData();

                        int payloadSize = payload.Length;
                        int id = response.Id;

                        WriteToStream(cli, stream, new byte[]
                        {
                            (byte)(payloadSize & 0xFF),
                            (byte)((payloadSize >> 8) & 0xFF),
                            (byte)((payloadSize >> 16) & 0xFF),
                            (byte)((payloadSize >> 24) & 0xFF)
                        });
                        WriteToStream(cli, stream, new byte[]
                        {
                            (byte)(id & 0xFF),
                            (byte)((id >> 8) & 0xFF),
                            (byte)((id >> 16) & 0xFF),
                            (byte)((id >> 24) & 0xFF)
                        });
                        WriteToStream(cli, stream, payload);
                    }
                }

                cli.ExitClientThread();
            }
        }

        private void Listen()
        {
            var listener = TcpListener.Create(this.port);

            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                throw new ServerException("Failed to start the TcpListener.", ex);
            }

            long clientID = 0;
            while (this.running)
            {
                TcpClient client = listener.AcceptTcpClient();
                var msgs = new Queue<Message>();
                var handle = new ClientWaitHandle(false, EventResetMode.AutoReset);
                var msgLock = new object();
                ClientConnection cli = new ClientConnection(client,
                    new Client(msgs, msgLock, handle, clientID++),
                    handle,
                    msgLock,
                    msgs)
                {
                    Running = true
                };

                string address = client.Client.LocalEndPoint?.ToString() ?? "Unknown";

                Log("Client connected {0}", address);

                client.GetStream().ReadTimeout = 60000;

                var read = new Thread(ClientReadThread);
                read.Name = "ReadThread";

                var write = new Thread(ClientWriteThread);
                write.Name = "WriteThread";

                lock (clientLock)
                {
                    this.clients.Add(cli);
                }

                read.Start(cli);
                write.Start(cli);
            }

            listener.Stop();
        }

        private void EventQueue()
        {
            while (this.running)
            {
                this.eventHandle.WaitOne();

                Event? ev = null;
                lock (this.eventLock)
                {
                    if (this.events.Count > 0)
                    {
                        ev = this.events.Dequeue();
                    }
                    else
                    {
                        this.eventHandle.Reset();
                    }
                }

                if (ev != null)
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    foreach (var node in nodes)
                    {
#if DEBUG
                        try
                        {
                            ev.HandleEvent(node);
                        }
                        catch (Exception e)
                        {
                            Log(e.Message);
                            if (e.StackTrace != null)
                            {
                                Log(e.StackTrace);
                            }
                        }
#else
                        ev.HandleEvent(node);
#endif
                    }

                    watch.Stop();
                    double elapsed = watch.Elapsed.TotalSeconds;
                    peakEventTime.Value = Math.Max(elapsed, peakEventTime.Value);

                    eventsProcessed.Value++;

                    ev.EventEnd();
                }
            }
        }


        public void Start()
        {
            this.running = true;

            var ev = new Thread(EventQueue);
            ev.Name = "EventQueue";
            ev.Start();

            this.Listen();
        }
    }
}
