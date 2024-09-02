using BililiveDebugPlugin.InteractionGame;
using InteractionGame.plugs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.Parser
{
    public class PopularityTicketActivityParser : ISubMsgParser
    {
        IGiftMgr _giftMgr;
        IContext _context;
        private IDyMsgParser _owner;
        private ConcurrentDictionary<string,(int,int)> SendTicketNumDict = new ConcurrentDictionary<string,(int,int)>();
        public static string PopularityTicket => Aoe4DataConfig.PopularityTicket;

        private int MaxCount = 0;
        private List<int> ConfigGift = new List<int>();

        public void Init(IDyMsgParser owner)
        {
            _owner = owner;
            _giftMgr = Locator.Instance.Get<IGiftMgr>();
            _context = Locator.Instance.Get<IContext>();

            InitData();
        }

        private void InitData()
        {
            _giftMgr.EnumerateGifts((it) =>
            {
                if(it.Id.Length > PopularityTicket.Length && it.Id.StartsWith(PopularityTicket) && int.TryParse(it.Id.Substring(PopularityTicket.Length),out var count))
                {
                    if(count > MaxCount)
                        MaxCount = count;
                    ConfigGift.Add(count);
                }
            });
            ConfigGift.Sort((a, b) => a - b);
        }

        public void OnClear()
        {
            SendTicketNumDict.Clear();
        }

        public void OnStartGame()
        {

        }

        public void OnTick(float delat)
        {

        }

        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == BilibiliDM_PluginFramework.MsgTypeEnum.GiftSend && msg.msg.GiftName == PopularityTicket)
            {
                lock (this)
                {
                    AddTicket(msg.msg.OpenID, msg.msg.GiftCount);
                    CheckGiftSatisfy(msg.msg.OpenID);
                }
            }
            return false;
        }

        private void CheckGiftSatisfy(string uid)
        {
            var satisfy = -1;
            var user = _owner.GetUserData(uid);
            if (user == null) return;
            if(SendTicketNumDict.TryGetValue(uid,out var v))
            {
                foreach(var c in ConfigGift)
                {
                    if(v.Item2 < c && v.Item1 >= c)
                    {
                        satisfy = c;
                        _context.PrintGameMsg($"{user.NameColored}触发满{PopularityTicket}*{c}");
                        _giftMgr.ApplyGift($"{PopularityTicket}{c}", user);
                    }
                }
            }
            if (satisfy > 0)
            {
                if (satisfy >= MaxCount)
                {
                    SendTicketNumDict[uid] = (v.Item1 - MaxCount, 0);
                    CheckGiftSatisfy(uid);
                }
                else
                    SendTicketNumDict[uid] = (v.Item1, satisfy);
            }
        }

        private void AddTicket(string uid, int count)
        {
            if (SendTicketNumDict.TryGetValue(uid, out var c))
                SendTicketNumDict[uid] = (c.Item1 + count,c.Item2);
            else
                SendTicketNumDict[uid] = (count,0);
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }
    }
}
