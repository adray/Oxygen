using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

// +Support clients connecting (blocking listener)
// +Support clients sending a request and response
// +

namespace Oxygen
{
    public class ServerException : Exception
    {
        public ServerException(string message, Exception inner) : base(message, inner)
        {
            
        }
    }

    public class Client
    {
        private readonly EventWaitHandle waitHandle;
        private readonly object msgLock = new object();
        private readonly Queue<Message> msgs;
        private readonly Dictionary<string, object> properies = new Dictionary<string, object>();

        public Client(Queue<Message> msgs, object msgLock, EventWaitHandle waitHandle)
        {
            this.msgs = msgs;
            this.msgLock = msgLock;
            this.waitHandle = waitHandle;
        }

        /// <summary>
        /// Sends a message, safe to be called from any thread. 
        /// </summary>
        /// <param name="msg">The message to send.</param>
        public void Send(Message msg)
        {
            Logger.Instance.Log(">> {0}/{1}", msg.NodeName, msg.MessageName);

            lock (msgLock)
            {
                this.msgs.Enqueue(msg);
            }

            // Sets the wait handle for Read thread to consume the message queue.
            waitHandle.Set();
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
            public EventWaitHandle MsgHandle { get; private set; }
            public Queue<Message> Messages { get; private set; }
            public object MsgLock { get; private set; }
            private int threadEndCount;

            public ClientConnection(TcpClient connection, Client client, EventWaitHandle msgHandle, object msgLock, Queue<Message> messages)
            {
                Connection = connection;
                Client = client;
                MsgHandle = msgHandle;
                MsgLock = msgLock;
                Messages = messages;
            }

            public void ExitClientThread()
            {
                // if the we have finished write+read threads then safe to dispose the handle.
                int result = Interlocked.Increment(ref threadEndCount);
                if (result == 2)
                {
                    MsgHandle.Dispose();
                }
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

        public Server(int port) { this.port = port; }

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

            //NetworkStream? stream = null;

            //try
            //{
            //    stream = cli.Connection.GetStream();
            //}
            //catch (Exception)
            //{
            //    success = false;
            //    OnDisconnect(cli);
            //}

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

        const int INCOMING_HEADER_SIZE = 4;

        private void ClientReadThread(object? state)
        {
            var cli = state as ClientConnection;

            if (cli != null)
            {
                var stream = cli.Connection.GetStream();

                byte[] buffer = new byte[2048];
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

                            string name = msg.NodeName;
                            Log($"Message: {name}/{msg.MessageName} {len} bytes");

                            QueueEvent(new MessageRecievedEvent(cli.Client, name, msg, cli.MsgHandle));
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

                        WriteToStream(cli, stream, new byte[]
                        {
                            (byte)(payloadSize & 0xFF),
                            (byte)((payloadSize >> 8) & 0xFF),
                            (byte)((payloadSize >> 16) & 0xFF),
                            (byte)((payloadSize >> 24) & 0xFF)
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

            while (this.running)
            {
                TcpClient client = listener.AcceptTcpClient();
                var msgs = new Queue<Message>();
                var handle = new EventWaitHandle(false, EventResetMode.AutoReset);
                var msgLock = new object();
                ClientConnection cli = new ClientConnection(client,
                    new Client(msgs, msgLock, handle),
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
                    foreach (var node in nodes)
                    {
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
                    }

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
