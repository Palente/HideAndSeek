using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace HideAndSeek.Server
{
    public class ServerMain : BaseScript
    {
        public Dictionary<Player, PlayerGameStatus> Joueurs = new Dictionary<Player, PlayerGameStatus>();
        private readonly int MaxJoueurs = 21; // 20joueur et 1 chercheur
        private Vector3 hider_pos = new Vector3(3536.195f, 3665.674f, 28.12189f);
        private Vector3 seeker_pos = new Vector3(3624.037f, 3741.623f, 28.69011f);
        public ServerMain()
        {
            var x = 3536.195f;
            var y = 3665.674;
            var z = 28.12189;
        }

        [EventHandler("hideandseek:new_player")]
        public void New_Player([FromSource]Player player, bool state = true)
        {
            if (state)
            {
                //Rejoins le jeux
                Joueurs.Add(player, PlayerGameStatus.NOT_STARTED);
                TriggerClientEvent("hideandseek:send_notif", $"{player.Name} a ~g~rejoint~s~ le cache cache");
                Random rnd = new Random();
                var xRnd = rnd.NextDouble() * 4.83f + 1;
                var yRnd = rnd.NextDouble() * 7.426f + 1;
                float xBase = 3522.166f;
                float yBase = 3711.961f;

                //TriggerClientEvent(player, "hideandseek:waiting_room", xBase + xRnd, yBase+ yRnd, 20.99177f);
                TriggerClientEvent(player, "hideandseek:waiting_room", 3524.476f, 3708.644f, 20.99177f);
            }
            else
            {
                //quitte le jeux
                TriggerClientEvent("hideandseek:send_notif", $"{player.Name} � ~r~quitter~s~ le cache cache");
            }
        }
        [EventHandler("hideandseek:ask_start")]
        public void AskStart([FromSource] Player player)
        {
            Joueurs[player] = PlayerGameStatus.SEEKER;
            TriggerClientEvent("hideandseek:send_notif", $"~p~Le jeu va commencer!!!");
            foreach(Player p in Players)
            {
                if (Joueurs.ContainsKey(p))
                {
                    //joueur existe
                    if (Joueurs[p] == PlayerGameStatus.SEEKER)
                    {
                        TriggerClientEvent(p,"hideandseek:game_started", seeker_pos.X, seeker_pos.Y, seeker_pos.Z, true);
                    }else
                        TriggerClientEvent(p,"hideandseek:game_started", hider_pos.X, hider_pos.Y, hider_pos.Z, false);
                }
            }
        }
        [EventHandler("hideandseek:player_eliminated")]
        public void OnPlayerEliminated([FromSource]Player victim)
        {
            TriggerClientEvent("hideandseek:send_notif", $"~r~{victim.Name}~s~ a �t� �limin�!");
            TriggerClientEvent(victim, "hideandseek:player_spectator", false, Joueurs.Where(j=>j.Value == PlayerGameStatus.SEEKER).Count());
        }
        public Player GetSeeker()
        {
            return Joueurs.First(x => x.Value == PlayerGameStatus.SEEKER).Key;
        }
    }
    //Server Side
    public enum PlayerGameStatus
    {
        NOT_STARTED = -1,
        HIDER = 0,
        SEEKER,
        SPECTATOR
    }
}