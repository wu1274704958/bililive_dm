using InteractionGame;

namespace BililiveDebugPlugin.InteractionGame.Settlement
{
    public interface ISettlement<IT> where IT : class,IContext
    {
        void ShowSettlement(IT it,int winGroup);

        void Restart();
    }
}