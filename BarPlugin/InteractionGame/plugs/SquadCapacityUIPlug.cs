using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    [ProtoBuf.ProtoContract]
    public class GroupSquadCapacityUIData
    {
        [ProtoBuf.ProtoMember(1)] public int Group;
        [ProtoBuf.ProtoMember(2)] public int Count;
    }
    public class SquadCapacityUIPlug : IPlug<EGameAction>,ISquadCountObserver
    {
        public override void OnReceiveNotify(EGameAction m,object args = null)
        {
            
        }

        public override void Tick()
        {
            
        }

        public override void Init()
        {
            base.Init();
            
        }

        public override void Start()
        {
            base.Start();
            Locator.Get<IGameState>().AddObserver(this);
            Locator.Get<IContext>().SendMsgToOverlay((short)EMsgTy.SquadCountChanged, new GroupSquadCapacityUIData() { Group = -1, Count = Locator.Get<IConstConfig>().SquadCountLimit });
        }

        public override void Dispose()
        {
            //Locator.Get<IGameStateObserver<EAoe4State, Aoe4StateData>>().RemoveObserver(this);
            base.Dispose();
        }

        public void SquadCountChanged(int g, int old, int n)
        {
            Locator.Get<IContext>().SendMsgToOverlay((short)EMsgTy.SquadCountChanged,new GroupSquadCapacityUIData(){  Group = g,Count = n});
        }
    }
}