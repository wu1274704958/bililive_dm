using conf.Gift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs
{
    public interface IGiftMgr
    {
        bool VaildGift(string gift);
        bool ApplyGift(string gift,UserData user,int count = 1);
        bool GiveGift(string gift, UserData user,int count = 1);
        bool ApplyGift(Dictionary<string,int> gifts, UserData user);
        bool GiveGift(Dictionary<string, int> gifts, UserData user);
        bool GetItem(string gift,out conf.Gift.GiftItem item);
        void EnumerateGifts(Action<GiftItem> func);
        bool ApplyNotConfigGift(UserData user,string giftName, int price, int count = 1);
    }
}
