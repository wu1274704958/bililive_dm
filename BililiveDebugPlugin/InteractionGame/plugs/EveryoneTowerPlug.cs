using System;
using System.Collections.Concurrent;
using Interaction;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class EveryoneTowerPlug : IPlug<EGameAction>,IPlayerParserObserver
    {
        private ConcurrentDictionary<long,int> _group = new ConcurrentDictionary<long, int>();
        private ConcurrentDictionary<int,long> _mapUid = new ConcurrentDictionary<int, long>();
        private IAoe4Bridge<DebugPlugin> _bridge;
        private Aoe4GameState _state;
        private int LastMsgId = 0;
        public Action<long,int> OnTowerStateChange;
        public override void Tick()
        {
            var state = _state.CheckState(EAoe4State.TowerState);
            if (state.R != LastMsgId)
            {
                LastMsgId = state.R;
                if(_mapUid.TryGetValue(state.G, out var uid))
                {
                    if (_group.TryGetValue(uid, out var oldSt) && oldSt != state.B)
                    {
                        _group.TryUpdate(uid, state.B, oldSt);
                        _bridge.AppendExecCode($"TFE_OnResposon({state.R})");
                        OnTowerStateChange?.Invoke(uid,state.B);
                    }
                }
            }
        }

        public override void Start()
        {
            base.Start();
            _bridge = Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge();
            _state = Locator.Instance.Get<Aoe4GameState>();
            Locator.Instance.Get<IDyPlayerParser<DebugPlugin>>().AddObserver(this);
            Locator.Instance.Deposit(this);
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    break;
                case EGameAction.GameStop:
                    _group.Clear();
                    _mapUid.Clear();
                    LastMsgId = 0;
                    break;
            }
        }
        
        public bool IsTowerAlive(long uid)
        {
            return _group.TryGetValue(uid,out var v) && v == 1;
        }

        public void OnAddGroup(UserData userData, int g)
        {
            var mid = GenMid(userData);
            _group.TryAdd(userData.Id, 1);
            _mapUid.TryAdd(mid, userData.Id);
            _bridge.AppendExecCode($"TFE_AddTower({userData.Id},{mid},{g + 1},'{userData.Name}',0)");
        }

        private int GenMid(UserData userData)
        {
            return _group.Count + 1;
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {
            
        }

        public void OnClear()
        {
            
        }
    }
}