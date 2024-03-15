
using BililiveDebugPlugin.InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    
    [ProtoBuf.ProtoContract]
    public class LargePopTipsData
    {
        [ProtoBuf.ProtoMember(1)]
        public string Left;
        [ProtoBuf.ProtoMember(2)]
        public string Right;
        [ProtoBuf.ProtoMember(3)]
        public string Bottom;
        [ProtoBuf.ProtoMember(4)]
        public int LeftColor;
        [ProtoBuf.ProtoMember(5)]
        public int RightColor;
        [ProtoBuf.ProtoMember(6)]
        public int BottomColor;
        [ProtoBuf.ProtoMember(7)] 
        public float ShowTime;

        public void Show()
        {
            LargeTips.Show(this);
        }
    }
    

    public static class LargePopTipsDataBuilder
    {
        public static LargePopTipsData Create(string left, string right)
        {
            return new LargePopTipsData()
            {
                Left = left,
                Right = right,
                Bottom = "",
                LeftColor = LargeTips.White,
                RightColor = LargeTips.White,
                BottomColor = LargeTips.White,
                ShowTime = 3.0f
            };
        }
        public static LargePopTipsData SetBottom(this LargePopTipsData d, string bottom)
        {
            d.Bottom = bottom;
            return d;
        }
        public static LargePopTipsData SetLeftColor(this LargePopTipsData d, int color)
        {
            d.LeftColor = color;
            return d;
        }
        public static LargePopTipsData SetRightColor(this LargePopTipsData d, int color)
        {
            d.RightColor = color;
            return d;
        }
        public static LargePopTipsData SetBottomColor(this LargePopTipsData d, int color)
        {
            d.BottomColor = color;
            return d;
        }   
        // set show time
        public static LargePopTipsData SetShowTime(this LargePopTipsData d, float time)
        {
            d.ShowTime = time;
            return d;
        }
        
    }
    
    public class LargeTips
    {
        
        public static void Show(LargePopTipsData d)
        {
            Locator.Instance.Get<DebugPlugin>().SendMsg.SendMsg((short)EMsgTy.ShowLargeTips,d);
        }

        public static int White => 0x00ff_ffff;
        public static int Yellow => 9 << 24;
        public static int Cyan => 10 << 24;

        public static int GetGroupColor(int g)
        {
            return (0xff & (g + 1)) << 24;
        }
        
    }
}