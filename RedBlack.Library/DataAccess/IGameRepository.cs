using RedBlack.Library.DataContracts;

namespace RedBlack.Library.DataAccess
{
    public interface IGameRepository
    {
        bool SaveGame(Game gameData);
        Game FindGame(string playerId);
    }
}