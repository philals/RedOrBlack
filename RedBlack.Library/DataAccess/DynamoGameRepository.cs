using System;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library.DataAccess
{
    public class DynamoGameRepository : IGameRepository
    {
        public bool SaveGame(Game gameData)
        {
            throw new NotImplementedException();
        }

        public Game FindGame(string playerId)
        {
            throw new NotImplementedException();
        }
    }
}