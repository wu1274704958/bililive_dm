using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Resource;
using Interaction;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class EveryoneTowerPlug : IPlug<EGameAction>,IPlayerParserObserver,IPlayerPreJoinObserver
    {
        public struct TowerData
        {
            public int State;
            public int Mid;

            public TowerData(int state, int mid)
            {
                State = state;
                Mid = mid;
            }
        }
        private ConcurrentDictionary<string, TowerData> _group = new ConcurrentDictionary<string, TowerData>();
        private ConcurrentDictionary<int,string> _mapUid = new ConcurrentDictionary<int, string>();
        private HashSet<string> _isStoneSet = new HashSet<string>();
        private IAoe4Bridge<DebugPlugin> _bridge;
        private IResourceMgr<DebugPlugin> _resMgr;
        private Aoe4GameState _state;
        private int LastMsgId = 0;
        public Action<string, TowerData> OnTowerStateChange;
        private DebugPlugin _cxt;

        public override void Tick()
        {
            var state = _state.CheckState(EAoe4State.TowerState);
            if (state.R != LastMsgId)
            {
                LastMsgId = state.R;
                if(_mapUid.TryGetValue(state.G, out var uid))
                {
                    if (_group.TryGetValue(uid, out var d) && d.State != state.B)
                    {
                        var @new = new TowerData(state.B, d.Mid);
                        _group.TryUpdate(uid, @new, d);
                        _bridge.AppendExecCode($"TFE_OnResposon({state.R})");
                        OnTowerStateChange?.Invoke(uid,@new);
                    }
                }
            }
        }

        public override void Start()
        {
            base.Start();
            _bridge = (_cxt = Locator.Instance.Get<DebugPlugin>()).messageDispatcher.GetBridge();
            _resMgr = _cxt.messageDispatcher.GetResourceMgr();
            _state = Locator.Instance.Get<Aoe4GameState>();
            var playerParser = Locator.Instance.Get<IDyPlayerParser<DebugPlugin>>();
            playerParser.AddObserver(this);
            playerParser.AddPreJoinObserver(this);
            Locator.Instance.Deposit(this);

            OnTowerStateChange += _onTowerStateChange;
        }

        private void _onTowerStateChange(string id, TowerData st)
        {
            if(st.State == 2)
            {
                var u = _cxt.messageDispatcher.MsgParser.GetUserData(id);
                if (u == null)
                    return;
                var gold = _resMgr.GetResource(id);
                if (gold > 0) 
                    _resMgr.RemoveResource(id, gold);
                _cxt.PrintGameMsg($"{u.NameColored}哨塔被摧毁");
            }
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
                    _isStoneSet.Clear();
                    LastMsgId = 0;
                    break;
            }
        }
        
        public bool IsTowerAlive(string uid)
        {
            return _group.TryGetValue(uid,out var v) && v.State == 1;
        }

        public void OnAddGroup(UserData userData, int g)
        {
            var mid = GenMid(userData);
            _group.TryAdd(userData.Id, new TowerData(1,mid));
            _mapUid.TryAdd(mid, userData.Id);
            int op = userData.GuardLevel & 255;
            if (_isStoneSet.Contains(userData.Id))
                op |= (1 << 8);
            _bridge.AppendExecCode($"TFE_AddTower('{userData.Id}',{mid},{g + 1},'{userData.Name}',{op})");
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

        public override void Dispose()
        {
            OnTowerStateChange -= _onTowerStateChange;
            base.Dispose();
        }

        public DanmakuModel OnPreJoin(DanmakuModel m)
        {
            if(m.CommentText != null && m.CommentText.IndexOf("石") >= 0)
            {
                _isStoneSet.Add(m.OpenID);
            }
            return m;
        }

        public void PopGoldTips(UserData u,int gold)
        {
            if(_group.TryGetValue(u.Id, out var d))
            {
                _bridge.AppendExecCode($"TFE_PopupTips({d.Mid},'{u.Name}有{gold}金')");
            }
        }
    }
}