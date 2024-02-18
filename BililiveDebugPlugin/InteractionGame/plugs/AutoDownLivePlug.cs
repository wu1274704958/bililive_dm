using System;
using System.Diagnostics;
using BililiveDebugPlugin.InteractionGameUtils;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class AutoDownLivePlug: IPlug<EGameAction>,ISquadCountObserver
    {
        private int DownHour = 5;
        private bool NeedDown = false;
        public override void Tick()
        {
            if (DateTime.Now.Hour == DownHour)
            {
                NeedDown = true;
                LargeTips.Show(LargePopTipsDataBuilder.Create("本局结束","将会自动下播")
                    .SetBottom("感谢支持~").SetLeftColor(LargeTips.Yellow).SetRightColor(LargeTips.Yellow).SetBottomColor(LargeTips.Cyan));
            }
        }

        public override void Notify(EGameAction m)
        {
            if (NeedDown && m == EGameAction.GameStop)
            {
                LargeTips.Show(LargePopTipsDataBuilder.Create("即将自动下播", "感谢支持~")
                    .SetBottom("不出意外10点左右会开播哦~").SetLeftColor(LargeTips.Yellow).SetRightColor(LargeTips.Yellow).SetBottomColor(LargeTips.Cyan).SetShowTime(7.0f));
                Process.Start("shutdown","/s /t 17"); 
            }
        }

        public void SquadCountChanged(int g, int old, int n)
        {
            
        }
    }
}