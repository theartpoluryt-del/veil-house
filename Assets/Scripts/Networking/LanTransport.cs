using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VeilHouse {
    /// <summary>Bounded TCP framing. All socket work happens on background threads;
    /// callers drain immutable text events on Unity's main thread.</summary>
    public sealed class LanTransport : IDisposable {
        public enum EventKind { Connected, Message, Disconnected, Error }
        public sealed class Event { public EventKind kind; public int peer; public string text; }
        const int MaxFrameBytes=1048576, MaxIncoming=768, MaxOutgoing=96;
        sealed class Peer {
            public int id,closed,queued;
            public TcpClient socket;
            public readonly ConcurrentQueue<byte[]> outgoing=new ConcurrentQueue<byte[]>();
            public readonly AutoResetEvent wake=new AutoResetEvent(false);
        }
        readonly ConcurrentQueue<Event> incoming=new ConcurrentQueue<Event>();
        readonly Dictionary<int,Peer> peers=new Dictionary<int,Peer>();
        readonly object gate=new object();
        TcpListener listener;
        volatile bool stopped;
        int nextId,incomingCount;

        public void Host(int port) {
            listener=new TcpListener(IPAddress.Any,port);
            listener.Start(6);
            Run(AcceptLoop,"Veil accept");
        }
        public void Join(string address,int port) {
            Run(()=> {
                var client=new TcpClient();
                try {
                    var pending=client.BeginConnect(address,port,null,null);
                    bool connected=pending.AsyncWaitHandle.WaitOne(8000);
                    pending.AsyncWaitHandle.Close();
                    if(!connected) throw new TimeoutException("Сервер не отвечает. Проверьте адрес и порт.");
                    client.EndConnect(pending);
                    if(stopped) { client.Close(); return; }
                    AddPeer(0,client);
                } catch(Exception ex) { client.Close(); Enqueue(EventKind.Error,0,"Подключение не удалось: "+ex.Message); }
            },"Veil connect");
        }
        public bool TryDequeue(out Event value) {
            if(incoming.TryDequeue(out value)) { Interlocked.Decrement(ref incomingCount); return true; }
            return false;
        }
        public bool Send(int id,string json) {
            if(stopped || json==null) return false;
            byte[] data=Encoding.UTF8.GetBytes(json+"\n");
            if(data.Length>MaxFrameBytes) return false;
            Peer p; lock(gate) { if(!peers.TryGetValue(id,out p)) return false; }
            if(p.closed!=0) return false;
            if(Interlocked.Increment(ref p.queued)>MaxOutgoing) { Interlocked.Decrement(ref p.queued); Close(p,"Соединение слишком медленное"); return false; }
            p.outgoing.Enqueue(data); p.wake.Set(); return true;
        }
        public void Kick(int id,string reason) { Peer p; lock(gate) peers.TryGetValue(id,out p); if(p!=null) Close(p,reason); }
        void AcceptLoop() {
            while(!stopped) {
                try {
                    var client=listener.AcceptTcpClient();
                    // Leave queue capacity for lifecycle events when a peer floods frames.
                    bool full; lock(gate) full=peers.Count>=6 || Volatile.Read(ref incomingCount)>MaxIncoming/2;
                    if(full) {
                        try { client.SendTimeout=1000; var data=Encoding.UTF8.GetBytes("{\"type\":\"error\",\"text\":\"Лобби заполнено (7 игроков)\"}\n"); client.GetStream().Write(data,0,data.Length); } catch { }
                        client.Close(); continue;
                    }
                    AddPeer(Interlocked.Increment(ref nextId),client);
                } catch(Exception ex) { if(!stopped) Enqueue(EventKind.Error,0,"Ошибка сервера: "+ex.Message); }
            }
        }
        void AddPeer(int id,TcpClient client) {
            client.NoDelay=true; client.ReceiveTimeout=30000; client.SendTimeout=6000;
            var p=new Peer {id=id,socket=client};
            lock(gate) { if(stopped) {client.Close();return;} peers.Add(id,p); }
            Enqueue(EventKind.Connected,id,"");
            Run(()=>ReadLoop(p),"Veil receive "+id);
            Run(()=>WriteLoop(p),"Veil send "+id);
        }
        void ReadLoop(Peer p) {
            var buffer=new byte[8192]; var frame=new byte[MaxFrameBytes]; int length=0;
            try {
                var stream=p.socket.GetStream();
                while(!stopped && p.closed==0) {
                    int count=stream.Read(buffer,0,buffer.Length);
                    if(count==0) break;
                    for(int i=0;i<count;i++) {
                        if(buffer[i]==10) {
                            if(length==0) continue;
                            string json=new UTF8Encoding(false,true).GetString(frame,0,length);
                            length=0;
                            if(!Enqueue(EventKind.Message,p.id,json)) throw new InvalidOperationException("Слишком много сетевых сообщений");
                        } else {
                            if(length>=frame.Length) throw new InvalidOperationException("Превышен размер сообщения");
                            frame[length++]=buffer[i];
                        }
                    }
                }
                Close(p,"Соединение закрыто");
            } catch(Exception ex) { Close(p,ex is System.IO.IOException ? "Соединение потеряно или истекло время ожидания" : ex.Message); }
        }
        void WriteLoop(Peer p) {
            try {
                var stream=p.socket.GetStream();
                while(!stopped && p.closed==0) {
                    byte[] data;
                    while(p.outgoing.TryDequeue(out data)) { Interlocked.Decrement(ref p.queued); stream.Write(data,0,data.Length); }
                    p.wake.WaitOne(1000);
                }
            } catch { Close(p,"Не удалось отправить данные"); }
        }
        bool Enqueue(EventKind kind,int peer,string text) {
            if(stopped) return false;
            int bound=MaxIncoming+(kind==EventKind.Message?0:32);
            if(Interlocked.Increment(ref incomingCount)>bound) { Interlocked.Decrement(ref incomingCount); return false; }
            incoming.Enqueue(new Event {kind=kind,peer=peer,text=text}); return true;
        }
        void Close(Peer p,string reason) {
            if(Interlocked.Exchange(ref p.closed,1)!=0) return;
            lock(gate) peers.Remove(p.id);
            try {p.socket.Close();} catch { }
            p.wake.Set();
            Enqueue(EventKind.Disconnected,p.id,reason);
        }
        static void Run(ThreadStart action,string name) { new Thread(action) {IsBackground=true,Name=name}.Start(); }
        public void Dispose() {
            stopped=true;
            try {listener?.Stop();} catch { }
            Peer[] list; lock(gate) {list=new Peer[peers.Count];peers.Values.CopyTo(list,0);}
            foreach(var p in list) Close(p,"");
            while(incoming.TryDequeue(out _)) { }
            incomingCount=0;
        }
    }
}
