using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame.Context;
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
        public override void Notify(EGameAction m)
        {
            
        }

        public override void Tick()
        {
            var gst = Locator.Instance.Get<IGameStateObserver<EAoe4State, Aoe4StateData>>();
            if (gst == null) return;
            for (int i = 0;i < Aoe4DataConfig.GroupCount;++i)
            {
                var c = gst.GetSquadCount(i);
                SquadCountChanged(i, 0, c);
            }
        }

        public override void Init()
        {
            base.Init();
            //Locator.Instance.Get<IGameStateObserver<EAoe4State, Aoe4StateData>>().AddObserver(this);
            Locator.Instance.Get<SM_SendMsg>().SendMsg((short)EMsgTy.SquadCountChanged,new GroupSquadCapacityUIData(){  Group = -1,Count = Aoe4DataConfig.SquadLimit});
        }

        public override void Dispose()
        {
            //Locator.Instance.Get<IGameStateObserver<EAoe4State, Aoe4StateData>>().RemoveObserver(this);
            base.Dispose();
        }

        public void SquadCountChanged(int g, int old, int n)
        {
            Locator.Instance.Get<SM_SendMsg>().SendMsg((short)EMsgTy.SquadCountChanged,new GroupSquadCapacityUIData(){  Group = g,Count = n});
        }
    }
}