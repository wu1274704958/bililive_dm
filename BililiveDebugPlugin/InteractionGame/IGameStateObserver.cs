using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BililiveDebugPlugin.InteractionGame
{
    public interface IGameStateObserver<E,T>
    {
        void Init();
        T CheckState(E state);
        T CheckState(EAoe4State state, IntPtr hwnd);
        T GetData(int x,int y, IntPtr hwnd);
        int GetSquadCount(int group);
        bool OnSpawnSquad(int group,int count);
        void Stop();
        void OnClear();
        void OnTick();
    }
}
