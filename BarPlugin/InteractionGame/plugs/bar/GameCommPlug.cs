using InteractionGame.Context;
using InteractionGame.plugs.config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Utils;

namespace InteractionGame.plugs.bar
{
    public class NoArgs { }
    public class GameBaseMsg<T>
    {
        public string cmd;
        public T args;

        public GameBaseMsg(string cmd, T args)
        {
            this.cmd = cmd;
            this.args = args;
        }
    }

    public class GameBaseMsgNoArgs
    {
        public string cmd;

        public GameBaseMsgNoArgs(string cmd)
        {
            this.cmd = cmd;
        }
    }
    public class GameCommPlug : IPlug<EGameAction>
    {
        private LocalMemComm comm;
        private ConcurrentDictionary<string, (Type, ConcurrentBag<Action<string, object>>)> listenerTable
            = new ConcurrentDictionary<string, (Type, ConcurrentBag<Action<string, object>>)>();

        public override void Init()
        {
            base.Init();
            comm = new LocalMemComm();
        }

        public override void Start()
        {
            base.Start();
            var conf = Locator.Get<IConstConfig>();
            comm.Init(conf.GameCommKey, conf.GameCommSize);
        }

        public override void Dispose()
        {
            comm.Release();
            base.Dispose();
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    break;
                case EGameAction.GameStop:
                    comm.Release();
                    var conf = Locator.Get<IConstConfig>();
                    comm.Init(conf.GameCommKey, conf.GameCommSize);
                    break;
            }
        }

        public override void Tick()
        {
            if(comm.Tick())
            {
                RecvMsg(comm.PopRecv());
            }
        }

        private void RecvMsg(string json)
        {
            JObject obj = JObject.Parse(json);
            if(obj == null || !json.Contains("cmd"))
            {
                Locator.Get<IContext>().Log($"Game comm recv msg parse failed!!! msg = {json}");
                return;
            }
            string key = (string)obj["cmd"];
           
            if (listenerTable.TryGetValue(key,out var v))
            {
                if(v.Item1 == typeof(NoArgs))
                {
                    foreach (var it in v.Item2)
                        it.Invoke(key, null); 
                }
                else
                {
                    var msgTy = typeof(GameBaseMsg<>);
                    msgTy = msgTy.MakeGenericType(v.Item1);
                    var msgArgs = JsonConvert.DeserializeObject(json,msgTy);
                    if(msgArgs == null)
                    {
                        Locator.Get<IContext>().Log($"Game comm recv msg deserialize failed!!! msg = {json}");
                        return;
                    }
                    var argsField = msgTy.GetField("args");
                    var args = argsField.GetValue(msgArgs);
                    if (msgArgs == null)
                    {
                        Locator.Get<IContext>().Log($"Game comm recv msg get args failed!!! msg = {json}");
                        return;
                    }
                    foreach (var it in v.Item2)
                        it.Invoke(key, args);
                }
            }
            else
            {
                Locator.Get<IContext>().Log($"Game comm recv msg not listener!! msg = {json}");
            }
        }

        public void SendMsgToGame<T>(string id, T msg)
        {
            string jsonString = null;
            if (typeof(T) == typeof(NoArgs))
                jsonString = JsonConvert.SerializeObject(new GameBaseMsgNoArgs(id), Formatting.None);
            else
                jsonString = JsonConvert.SerializeObject(new GameBaseMsg<T>(id,msg), Formatting.None);
            if(jsonString != null && jsonString.Length > 0) {
                comm.Send(jsonString);
            }
        }

        public void RegisterOnRecvGameMsg<T>(string key, Action<string, object> callback)
        {
            if (listenerTable.TryGetValue(key,out var v))
            {
                Debug.Assert(v.Item1 == typeof(T));
                v.Item2.Add(callback);
            }
            else
            {
                if(listenerTable.TryAdd(key, (typeof(T), new ConcurrentBag<Action<string, object>>())))
                    listenerTable[key].Item2.Add(callback);
                else
                    RegisterOnRecvGameMsg<T>(key, callback);
            }
        }
    }
}
