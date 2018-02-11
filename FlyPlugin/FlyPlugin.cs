
using System;
using System.IO;
using System.Reflection;
using Fougerite;
using UnityEngine;

namespace FlyPlugin
{
    public class FlyPlugin : Fougerite.Module
    {
        public static Vector3 h = new Vector3(0f, -0.75f, 0f);
        public bool Mods = false;

        public override string Name
        {
            get { return "FlyPlugin"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "Fly on a nice box"; }
        }

        public override Version Version
        {
            get { return new Version("1.1"); }
        }

        public override void Initialize()
        {
            Fougerite.Hooks.OnCommand += On_Command;
            IniParser ini = IniParser();
            Mods = ini.GetBoolSetting("Settings", "EnableForMods");
        }

        public override void DeInitialize() 
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(FlyingPlayer));
            if (objects != null)
            {
                foreach (var gameObj in objects) 
                {
                    UnityEngine.Object.Destroy(gameObj);
                }
            }
            Fougerite.Hooks.OnCommand -= On_Command;
        }

        private IniParser IniParser()
        {
            IniParser ini;
            if (!File.Exists(ModuleFolder + "\\Config.ini"))
            {
                File.Create(ModuleFolder + "\\Config.ini").Dispose();
                ini = new IniParser(ModuleFolder + "\\Config.ini");
                ini.AddSetting("Settings", "EnableForMods", "true");
                ini.Save();
                return ini;
            }
            ini = new IniParser(ModuleFolder + "\\Config.ini");
            return ini;
        }

        public void On_Command(Fougerite.Player player, string cmd, string[] args)
        {
            if (cmd == "fly")
            {
                if (player.Admin || (player.Moderator && Mods))
                {
                    if (args.Length == 0)
                    {
                        if (player.PlayerClient.netUser.playerClient.GetComponent<FlyingPlayer>() != null)
                        {
                            UnityEngine.Object.Destroy(player.PlayerClient.netUser.playerClient
                                .GetComponent<FlyingPlayer>());
                            player.MessageFrom("Fly", "Flying Disabled.");
                            return;
                        }

                        player.MessageFrom("Fly", "Usage: /fly number");
                    }
                    else
                    {
                        float speed = 1f;
                        if (args.Length > 0)
                        {
                            float.TryParse(string.Join("", args), out speed);
                        }

                        FlyingPlayer flyingPlayer =
                            player.PlayerClient.netUser.playerClient.GetComponent<FlyingPlayer>();
                        if (flyingPlayer == null)
                        {
                            flyingPlayer = player.PlayerClient.netUser.playerClient.gameObject
                                .AddComponent<FlyingPlayer>();
                        }

                        flyingPlayer.Refresh();
                        flyingPlayer.speed = speed;
                        player.MessageFrom("Fly", "Flying Speed: " + speed);
                    }
                }
            }
            else if (cmd == "flyreload")
            {
                if (player.Admin)
                {
                    IniParser ini = IniParser();
                    Mods = ini.GetBoolSetting("Settings", "EnableForMods");
                    player.MessageFrom("Fly", "Done");
                }
            }
        }

        public class FlyingPlayer : MonoBehaviour
        {
            public PlayerClient playerClient;
            public DeployableObject DeployableObject;
            public Vector3 origin;
            public Character character;
            public float speed;
            public RustServerManagement ServerManagement;

            public void Awake()
            {
                ServerManagement = RustServerManagement.Get();
                playerClient = GetComponent<PlayerClient>();
                character = playerClient.controllable.GetComponent<Character>();
                origin = character.origin;
                character.takeDamage.SetGodMode(true);
                NewObject();
            }

            public void NewObject()
            {
                DeployableObject = NetCull.InstantiateStatic(";deploy_wood_box", origin + h, character.rotation).GetComponent<DeployableObject>();
                DeployableObject.SetupCreator(character.controllable);
            }

            public void FixedUpdate()
            {
                if (character == null)
                {
                    Destroy(this);
                    return;
                }
                if (speed == 0f)  { return; }
                origin = origin + (character.eyesRotation * Vector3.forward) * speed;
                if (DeployableObject != null) { NetCull.Destroy(DeployableObject.gameObject); }
                NewObject();
                ServerManagement.TeleportPlayerToWorld(playerClient.netPlayer, origin);
            } 

            public void Refresh()
            {
                origin = character.origin;
            }

            public void OnDestroy()
            {
                NetCull.Destroy(DeployableObject.gameObject);
                if (character != null) { character.takeDamage.SetGodMode(false); }
            }
        }
    }
}
