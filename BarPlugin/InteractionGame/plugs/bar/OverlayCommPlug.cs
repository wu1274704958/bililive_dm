using InteractionGame.Context;
using InteractionGame.plugs.config;
using Newtonsoft.Json;
using System;
using Utils;

namespace InteractionGame.plugs.bar
{
    public class OverlayBaseMsg<T>
    {
        public int cmd;
        public T args;

        public OverlayBaseMsg(int cmd, T args)
        {
            this.cmd = cmd;
            this.args = args;
        }
    }
    public class OverlayCommPlug : IPlug<EGameAction>
    {
        private LocalMemComm comm;

        public override void Init()
        {
            base.Init();
            comm = new LocalMemComm();
        }

        public override void Start()
        {
            base.Start();
            var conf = Locator.Instance.Get<IConstConfig>();
            comm.Init(conf.OverlayCommKey, conf.OverlayCommSize);
        }

        public override void Dispose()
        {
            comm.Release();
            base.Dispose();
        }

        public override void Notify(EGameAction m)
        {

        }

        public override void Tick()
        {
            comm.Tick();
        }

        public void SendMsgToOverlay<T>(short id, T msg)
        {
            string jsonString = JsonConvert.SerializeObject(new OverlayBaseMsg<T>(id,msg), Formatting.Indented);
            if(jsonString != null && jsonString.Length > 0) {
                comm.Send(jsonString);
            }
        }
    }
}
