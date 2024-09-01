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

        public UnitData(string v, int c)
        {
            this.Squad = v;
            this.Count = c;
        }
    }
    public class SpawnSquadData
    {
        public string Id;
        public int Target = -1;
        public List<UnitData> SquadGroup = new List<UnitData>();
    }
    public class BarBridge : IGameBridge
    {
        private IContext _context;
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
                data.SquadGroup.Add(new UnitData(it.Item1.GetBlueprint(user.Group), c));
            }
            _context.SendMsgToGame<SpawnSquadData>(EGameMsg.SSpawn, data);
            return res;
        }

        public void ExecSpawnSquad(UserData user, SquadData squad, int count, int target = -1, object opt = null)
        {
            var data = new SpawnSquadData
            {
                Id = user.Id,
                Target = target
            };
            data.SquadGroup.Add(new UnitData(squad.GetBlueprint(user.Group), count));
            _context.SendMsgToGame<SpawnSquadData>(EGameMsg.SSpawn, data);
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

        }

        public void Stop()
        {

        }
    }
}
