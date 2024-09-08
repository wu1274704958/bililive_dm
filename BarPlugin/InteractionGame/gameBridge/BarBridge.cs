using conf.Squad;
using InteractionGame.Context;
using InteractionGame.plugs.bar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.gameBridge
{
    public class UnitData
    {
        public string Squad;
        public int Count;
        public int BornOp;

        public UnitData(string v, int c, int bornOp)
        {
            this.Squad = v;
            this.Count = c;
            BornOp = bornOp;
        }
    }
    public class SpawnSquadData
    {
        public string Id;
        public int Target = -1;
        public List<UnitData> SquadGroup = new List<UnitData>();
    }
    class ChangeTowerData
    {
        public string id;
        public string tower;
    }
    public class BarBridge : IGameBridge
    {
        private IContext _context;
        private List<SpawnSquadData> spawnSquadDatas = new List<SpawnSquadData>();
        private DateTime _lastSendSpawnTime = DateTime.Now;
        private TimeSpan SpawnDuration = TimeSpan.FromMilliseconds(200);
        private int NeedSpawnCount = 50;

        public void ChangeTower(UserData user, SquadData squad, object op = null)
        {
            var data = new ChangeTowerData
            {
                id = user.Id,
                tower = squad.GetBlueprint(user.Group)
            };
            _context.SendMsgToGame<ChangeTowerData>(EGameMsg.SChangeTower, data);
        }

        public int ExecSpawnGroup(UserData user, List<(SquadData, int)> group, int target = -1, double multiple = 1, object opt = null)
        {
            var data = new SpawnSquadData
            {
                Id = user.Id,
                Target = target
            };
            int res = 0;
            foreach (var it in group)
            {
                int c = (int)Math.Round(it.Item2 * multiple);
                res += c;
                data.SquadGroup.Add(new UnitData(it.Item1.GetBlueprint(user.Group), c, GetBornOp(it.Item1)));
            }
            lock(spawnSquadDatas)
            {
                spawnSquadDatas.Add(data);
            }
            return res;
        }

        private int GetBornOp(SquadData squad)
        {
            return squad.SquadType;
        }

        public void ExecSpawnSquad(UserData user, SquadData squad, int count, int target = -1, object opt = null)
        {
            var data = new SpawnSquadData
            {
                Id = user.Id,
                Target = target
            };
            data.SquadGroup.Add(new UnitData(squad.GetBlueprint(user.Group), count,GetBornOp(squad)));
            lock(spawnSquadDatas)
            {
                spawnSquadDatas.Add(data);
            }
        }

        public void ForceFinish()
        {
            _context.SendMsgToGame<NoArgs>(EGameMsg.SForceFinish, null);
        }

        public void Init(IContext it)
        {
            _context = it;
        }

        public void OnClear()
        {

        }

        public void OnTick(float delta)
        {
            var now = DateTime.Now;
            if (spawnSquadDatas.Count > 0 && (now - _lastSendSpawnTime) >= SpawnDuration)
            {
                _context.SendMsgToGame(EGameMsg.SSpawn, spawnSquadDatas);
                spawnSquadDatas.Clear();
                _lastSendSpawnTime = now;
            }
        }

        public void Stop()
        {

        }
    }
}
