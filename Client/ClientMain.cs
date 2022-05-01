using System;
using System.Threading.Tasks;
using CitizenFX.Core;

using HideAndSeek.Client.Utils;

using static CitizenFX.Core.Native.API;

namespace HideAndSeek.Client
{
    public class ClientMain : BaseScript
    {
        private bool _IsPlaying = false;
        private bool _GameStarted = false;
        private long _startTime;
        private long _endTime;
        private Model CurrentModel;
        private Vector3 _OldPos;
        private HideGame _CurrentGame;
        public long CurrentTime
        {
            get => _endTime - TimeUtils.ToUnixTimeSeconds(); 
        }
        public PlayerGameStatus PlayerRole;
        public ClientMain()
        {
            Debug.WriteLine("Hi from HideAndSeek.Client!");
        }
        [Command("hideandseek")]
        public void HideAndSeekCommand()
        {
            _IsPlaying = !_IsPlaying;
            if (_IsPlaying && !_GameStarted)
            {
                _OldPos = LocalPlayer.Character.Position;
                SendNotification($"Le jeux ne vas pas tarder à commencer merci de patienter...");
                TriggerServerEvent("hideandseek:new_player", _IsPlaying);
            }

            SendNotification((_IsPlaying ? "Vous rejoignez le jeux!" : "Vous avez bien quitter le jeux"));
            if(!_IsPlaying && _GameStarted)
            {
                TriggerServerEvent("hideandseek:player_eliminated");
                SendNotification("Vous avez perdu vous ferez mieux la prochaine fois!");
            }
        }
        [Command("gohide")]
        public void LaunchGameCommand()
        {
            //lance le jeux
            TriggerServerEvent("hideandseek:ask_start");
        }
        [Tick]
        public Task OnTick()
        {
            if (_IsPlaying && _GameStarted)
            {
                DisplayDuration();
                DisplayPlayerAlive();
                //Cacher L'inventaire d'arme
                HideHudComponentThisFrame(19);
                DisableControlAction(2, 37, true);
                SetPedCanSwitchWeapon(GetPlayerPed(-1), false);
                if (PlayerRole == PlayerGameStatus.HIDER)
                {
                    SetCurrentPedWeapon(GetPlayerPed(-1), (uint)WeaponHash.Unarmed, true);
                    DisableControlAction(0, 106, true);
                    SetCanPedEquipAllWeapons(GetPlayerPed(-1), false);
                    if (IsPedBeingStunned(GetPlayerPed(-1),0))
                    {
                        // 4 secondes au sol
                        SetPedMinGroundTimeForStungun(GetPlayerPed(-1), 4 * 1000);
                        TriggerServerEvent("hideandseek:player_eliminated");
                    }
                }
                else
                {
                    //Seeker
                }
                int min, seconds;
                min = (int)(CurrentTime / 60);
                seconds = (int)(CurrentTime % 60);

                if(min <= 0 && seconds == 0)
                {
                    //Fin de la game
                    TriggerServerEvent("hideandseek:player_eliminated");
                }
            }
            return Task.FromResult(0);
        }

        private void DisplayDuration()
        {
            //TODO: changez la couleur lorsqu'il reste moins d'une minute
            SetTextFont(4);
            SetTextScale(0.3f, 0.3f);
            SetTextProportional(false);
            if (PlayerRole == PlayerGameStatus.SEEKER)
                SetTextColour(211, 0, 0, 255);
            else
                SetTextColour(0, 220, 110,255);
            SetTextDropshadow(0, 0, 0, 0, 255);
            SetTextEdge(1, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextOutline();
            SetTextCentre(false);
            SetTextJustification(0);
            SetTextEntry("STRING");

            int min, seconds;
            min = (int)(CurrentTime / 60);
            seconds = (int)(CurrentTime % 60);
            string txt;

            if (PlayerRole == PlayerGameStatus.SEEKER)
                txt = $"Il ne vous reste plus que : {min}:{seconds}";
            else
                txt = $"Restez cachez pendant encore : {min}:{seconds}";
            AddTextComponentString(txt+$"{(min !=0?"minutes":"secondes")}");
            int x = 0, y = 0;
            GetScreenActiveResolution(ref x, ref y);
            DrawText(0.5f, 0.98f);
        }
        private void DisplayPlayerAlive()
        {
            SetTextFont(4);
            SetTextScale(0.3f, 0.3f);
            SetTextProportional(false);
            if (PlayerRole == PlayerGameStatus.SEEKER)
                SetTextColour(211, 0, 0, 255);
            else
                SetTextColour(0, 220, 110, 255);
            SetTextDropshadow(0, 0, 0, 0, 255);
            SetTextEdge(1, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextOutline();
            SetTextCentre(false);
            SetTextJustification(0);
            SetTextEntry("STRING");
            AddTextComponentString($"{_CurrentGame.PlayerAlive} / {_CurrentGame.PlayerCount}");
            int x = 0, y = 0;
            GetScreenActiveResolution(ref x, ref y);
            DrawText(0.5f, 0.98f);
        }



        public void SendNotification(string text)
        {
            BeginTextCommandThefeedPost("STRING");
            AddTextComponentString(text);
            EndTextCommandThefeedPostTicker(true, true);
        }
        [EventHandler("hideandseek:send_notif")]
        public void SendNotif_Server(string text)
        {
            //Pas spaam les victimes
            if(_IsPlaying)
                SendNotification(text);
        }
        [EventHandler("hideandseek:game_started")]
        public void GameStarted(float x, float y, float z, bool seeker)
        {
            LocalPlayer.Character.Position = new Vector3(x, y, z);
            _GameStarted = true;
            _startTime = TimeUtils.ToUnixTimeSeconds();
            //11 minutes de jeux: 1minutes pour se cacher
            _endTime = TimeUtils.ToUnixTimeSeconds() + 11*60;
            if (seeker)
            {
                SendNotification("Ton but est de trouver et tazzer les gens bg");
                PlayerRole = PlayerGameStatus.SEEKER;
                GiveWeaponToPed(GetPlayerPed(-1), (uint)WeaponHash.StunGun, 99, true, true);
                NetworkSetFriendlyFireOption(true);
                SetCurrentPedWeapon(GetPlayerPed(-1), (uint)WeaponHash.StunGun, true);
                SetCanAttackFriendly(GetPlayerPed(-1), true, false);
                PlayPedAmbientSpeechNative(GetPlayerPed(-1), "Generic_Insult_High", "Speech_Params_Force");

                //CRINGE
                SetPedDiesWhenInjured(GetPlayerPed(-1), false);
                SetEntityInvincible(GetPlayerPed(-1), true);
                SetPedCanRagdollFromPlayerImpact(GetPlayerPed(-1), false);
                SetEntityCanBeDamaged(GetPlayerPed(-1), false);
                //When respawning player do not have collision
                //SetEntityCompletelyDisableCollision(GetPlayerPed(-1), true, false);
                //Change Model
                CurrentModel = LocalPlayer.Character.Model;
                Game.Player.ChangeModel(PedHash.FibOffice01SMM);
            }
            else
            {
                SendNotification("Cache toi hein force");
                PlayerRole = PlayerGameStatus.HIDER;
                //NetworkSetFriendlyFireOption(false);
                SetCurrentPedWeapon(GetPlayerPed(-1), (uint)WeaponHash.Unarmed, true);
            }
        }
        [EventHandler("hideandseek:player_spectator")]
        public void PlayerSpectator(bool finished = false, int count = -1)
        {
            _CurrentGame.PlayerCount = count;
            //TODO: make a real spectator
            if (PlayerRole == PlayerGameStatus.SEEKER)
            {
                //Fin de la game?
                RemoveWeaponFromPed(GetPlayerPed(-1), (uint)WeaponHash.StunGun);
                SetPedDiesWhenInjured(GetPlayerPed(-1), true);
                SetEntityInvincible(GetPlayerPed(-1), false);
                SetPedCanRagdollFromPlayerImpact(GetPlayerPed(-1), true);
                SetEntityCanBeDamaged(GetPlayerPed(-1), true);
                //SetEntityCompletelyDisableCollision(GetPlayerPed(-1), false, true);
                Game.Player.ChangeModel(CurrentModel);
            }
            PlayerRole = PlayerGameStatus.SPECTATOR;
            SendNotification("~p~Vous êtes maintenant un spectateur");
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(GetPlayerPed(-1), true, false);
            DisableControlAction(2, 37, false);
            SetPedCanSwitchWeapon(GetPlayerPed(-1), true);
            DisableControlAction(0, 106, false);
            SetCanPedEquipAllWeapons(GetPlayerPed(-1), true);
            _OldPos = new Vector3(_OldPos.X, _OldPos.Y, _OldPos.Z + 2f);
            LocalPlayer.Character.Position = _OldPos;
            _IsPlaying = false;
        }
        [EventHandler("hideandseek:waiting_room")]
        public void WaitingRoom(float x, float y, float z)
        {
            LocalPlayer.Character.Position = new Vector3(x, y, z);
            PlayerRole = PlayerGameStatus.NOT_STARTED;
        }
    }
    //Client Side
    public enum PlayerGameStatus
    {
        NOT_STARTED = -1,
        HIDER = 0,
        SEEKER,
        SPECTATOR
    }
    public class HideGame
    {
        public int PlayerCount = 0; //Player in the game
        public int PlayerAlive = 0; //Player alive

        public bool Started = false;
    }
}