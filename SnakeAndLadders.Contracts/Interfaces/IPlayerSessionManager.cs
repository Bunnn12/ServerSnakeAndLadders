using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IPlayerSessionManager
    {
        /// <summary>
        /// Saca al usuario de cualquier lobby/partida/chat activo.
        /// </summary>
        /// <param name="userId">Id del usuario sancionado.</param>
        /// <param name="reason">Motivo legible para logs/mensajes.</param>
        void KickUserFromAllSessions(int userId, string reason);
    }
}
