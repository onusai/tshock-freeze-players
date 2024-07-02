using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Text.Json;
using Terraria.GameContent.Creative;

namespace FreezePlayers
{
    [ApiVersion(2, 1)]
    public class FreezePlayers : TerrariaPlugin
    {

        public override string Author => "Onusai";
        public override string Description => "Prevents all playeres from moving";
        public override string Name => "FreezePlayers";
        public override Version Version => new Version(1, 0, 0, 0);

        public class ConfigData
        {
            public bool FreezeOnStart { get; set; } = false;
        }

        public Thread delayThread;
        bool active = true;

        ConfigData configData;
        bool freezeEnabled;

        public FreezePlayers(Main game) : base(game) { }

        public override void Initialize()
        {
            configData = PluginConfig.Load("FreezePlayers");
            freezeEnabled = configData.FreezeOnStart;

            ServerApi.Hooks.GameInitialize.Register(this, OnGameLoad);
        }

        void OnGameLoad(EventArgs e)
        {
            RegisterCommand("freeze", "tshock.admin", ToggleFreeze, "Enables / Disables freeze players");

            delayThread = new Thread(new ThreadStart(DelayAction));
            delayThread.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                active = false;
                delayThread.Join();

                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name)
            { HelpText = helptext });
        }

        void ToggleFreeze(CommandArgs args)
        {
            freezeEnabled = !freezeEnabled;
            var godPower = CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>();
            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null || player.Dead) continue;
                player.GodMode = freezeEnabled;
                godPower.SetEnabledState(player.Index, player.GodMode);
            }
        }

        void DelayAction()
        {
            while (active)
            {
                Thread.Sleep(1000);

                if (freezeEnabled)
                {
                    var buffSeconds = 3;
                    var godPower = CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>();
                    foreach (TSPlayer player in TShock.Players)
                    {
                        if (player != null && !player.Dead)
                        {
                            var buffTime = buffSeconds * 60;
                            player.SetBuff(14, buffTime); // thorns
                            player.SetBuff(116, buffTime); // inferno

                            player.SetBuff(115, buffTime); // rage
                            player.SetBuff(117, buffTime); // wraith

                            player.SetBuff(47, buffTime / 2); // frozen
                            player.SetBuff(149, buffTime); // webbed
                            //player.SetBuff(156, buffTime); // stoned

                            //player.SetBuff(163, buffTime); // obstructed

                            player.SetBuff(11, buffTime); // shine
                            player.SetBuff(12, buffTime); // night owl
                            player.SetBuff(10, buffTime); // invisiblity
                            player.SetBuff(119, buffTime * buffSeconds); // lovestruck
                                                                         //player.SetBuff(164, buffTime); // distored (gravity)

                            //player.SetBuff(170, buffTime); // solar
                            //player.SetBuff(173, buffTime); // life
                            //player.SetBuff(176, buffTime); // mana

                            //player.SetBuff(22, buffTime); // darkness


                            player.GodMode = freezeEnabled;
                            godPower.SetEnabledState(player.Index, player.GodMode);
                        }
                    }
                }
            }
        }

        public static class PluginConfig
        {
            public static string filePath;
            public static ConfigData Load(string Name)
            {
                filePath = String.Format("{0}/{1}.json", TShock.SavePath, Name);

                if (!File.Exists(filePath))
                {
                    var data = new ConfigData();
                    Save(data);
                    return data;
                }

                var jsonString = File.ReadAllText(filePath);
                var myObject = JsonSerializer.Deserialize<ConfigData>(jsonString);

                return myObject;
            }

            public static void Save(ConfigData myObject)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(myObject, options);

                File.WriteAllText(filePath, jsonString);
            }
        }

    }
}