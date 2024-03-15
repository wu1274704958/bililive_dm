using System;
using System.Collections.Generic;
using BililiveDebugPlugin.InteractionGame.Data;
using conf.Squad;
using ProtoBuf;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    [ProtoContract]
    public class SquadConfigData
    {
        [ProtoMember(1)] public Dictionary<System.Int32, SquadData> Squads;
        [ProtoMember(2)] public int RandomIdx;
        [ProtoMember(3)] public List<System.String> ContryList;
        [ProtoMember(4)] public int GroupCount;
    }

    public class SyncSquadConfig : IPlug<EGameAction>
    {
        public override void Tick()
        {
            
        }

        public override void Notify(EGameAction m)
        {
            
        }

        public override void Init()
        {
            base.Init();
            Locator.Instance.Deposit(this);
            SendMsg();
        }

        public void SendMsg()
        {
            Locator.Instance.Get<DebugPlugin>().SendMsg.SendMsg((short)EMsgTy.SyncSquadConfig, new SquadConfigData()
            {
                Squads = (Dictionary<System.Int32, SquadData>)SquadDataMgr.GetInstance().Dict,
                RandomIdx = Aoe4DataConfig.RandomIdx,
                ContryList = SettingMgr.GetInstance().Get(1).Country,
                GroupCount = Aoe4DataConfig.GroupCount
            });
        }
    }
}