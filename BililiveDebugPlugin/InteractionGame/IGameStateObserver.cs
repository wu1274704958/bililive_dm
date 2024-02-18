using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BililiveDebugPlugin.InteractionGame
{
    
    public interface ISquadCountObserver
    {
        void SquadCountChanged(int g,int old, int n);
    }
    public interface IGameStateObserver<E,T>
    {
        void Init();
        T CheckState(E state);
        T CheckState(EAoe4State state, IntPtr hwnd);
        T GetData(int x,int y, IntPtr hwnd);
        int GetSquadCount(int group);
        bool OnSpawnSquad(int group,int count,int lockTime=5);
        void Stop();
        void OnClear();
        void OnTick();
        void AddObserver(ISquadCountObserver observer);

        void RemoveObserver(ISquadCountObserver observer);
    }
}
