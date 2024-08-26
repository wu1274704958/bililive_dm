namespace BililiveDebugPlugin.InteractionGame.mode
{
    public interface IGameMode
    {
        void Init();
        void Start();
        void OnResume(object args);
        void OnPause();
        void Stop();
        void Dispose();
        int GetSeatCountOfPlayer(string id,int g);
        bool NextBackToDefault();
        int GetPlayerGroup(string id);
        int OverrideGetPlayerCount(int g, int count);
        int StartGroupLevel(int g);
        float GetSettlementHonorMultiplier(string id, bool win);
    }

    public class BaseGameMode : IGameMode
    {
        public virtual void Init()
        {
            
        }

        public virtual void Start()
        {
            
        }

        public virtual void OnResume(object args)
        {
            
        }

        public virtual void OnPause()
        {
            
        }

        public virtual void Stop()
        {
            
        }

        public virtual void Dispose()
        {
            
        }

        public virtual int GetSeatCountOfPlayer(string id, int g)
        {
            return 1;
        }

        public virtual bool NextBackToDefault()
        {
            return false;
        }

        public virtual int GetPlayerGroup(string id)
        {
            return -1;
        }

        public virtual int OverrideGetPlayerCount(int g, int count)
        {
            return count;
        }

        public virtual int StartGroupLevel(int g)
        {
            return 0;
        }

        public virtual float GetSettlementHonorMultiplier(string id, bool win)
        {
            return 1.0f;
        }
    }
}