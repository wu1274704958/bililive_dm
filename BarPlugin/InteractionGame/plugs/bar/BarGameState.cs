using InteractionGame.Context;
using System;
using System.Collections.Concurrent;
using Utils;

namespace InteractionGame.plugs.bar
{
    public struct UnitDestroyedData
    {
        public int group;
        public int count;
    }
    public class BarGameState : IPlug<EGameAction>, IGameState
    {
        private ConcurrentDictionary<int, ISquadCountObserver> _squadCountObservers = new ConcurrentDictionary<int, ISquadCountObserver>();
        private int _groupCount = 0;
        private string _mapName;
        private ConcurrentDictionary<int, int> _squadCountDict = new ConcurrentDictionary<int, int>();

        public int GroupCount => _groupCount;

        public string MapName => _mapName;

        public void AddObserver(ISquadCountObserver observer)
        {
            _squadCountObservers.TryAdd(observer.GetHashCode(), observer);
        }
        public void RemoveObserver(ISquadCountObserver observer)
        {
            _squadCountObservers.TryRemove(observer.GetHashCode(),out _);
        }

        public int GetSquadCount(int group)
        {
            if(_squadCountDict.TryGetValue(group, out var squadCount))
                return squadCount;
            return 0;
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    break;
                case EGameAction.GameStop:
                    _groupCount = 0;
                    _squadCountDict.Clear();
                    break;
            }
        }

        public void OnSpawnSquad(int group, int count)
        {
            if (_squadCountDict.TryGetValue(group, out var v))
            {
                _squadCountDict[group] = v + count;
                NotifySquadCountChanged(group,v);
            }
        }

        private void NotifySquadCountChanged(int group,int old)
        {
            foreach(var v in _squadCountObservers)
            {
                v.Value.SquadCountChanged(group, old, _squadCountDict[group]);
            }
        }

        public override void Init()
        {
            base.Init();
            Locator.Deposit<IGameState>(this);
            Locator.Get<IContext>().RegisterOnRecvGameMsg<GamePreStartData>(EGameMsg.BPreStart, OnGamePreStart);
            Locator.Get<IContext>().RegisterOnRecvGameMsg<UnitDestroyedData>(EGameMsg.BUnitDestroyed, OnUnitDestroyer);
        }

        public override void Start()
        {
            base.Start();
        }

        private void OnUnitDestroyer(string arg1, object arg2)
        {
            if(arg2 is UnitDestroyedData data && data.count > 0)
            {
                OnSpawnSquad(data.group, -data.count);
            }
        }

        private void OnGamePreStart(string arg1, object arg2)
        {
            if(arg2 is GamePreStartData data)
            {
                _groupCount = data.teamCount;
                _mapName = data.mapName;
                for(int i = 0;i < _groupCount;i++)
                {
                    _squadCountDict.TryAdd(i, 0);
                    NotifySquadCountChanged(i, 0);
                }
            }
        }

        public override void Tick()
        {
            throw new NotImplementedException();
        }
    }
}
