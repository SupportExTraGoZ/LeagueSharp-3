﻿#region

using System;
using System.IO;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace Evade
{
    internal static class SkillshotDetector
    {
        public delegate void OnDetectSkillshotH(Skillshot skillshot);

        static SkillshotDetector()
        {
            //Detect when the skillshots are created.
            Game.OnGameProcessPacket += GameOnOnGameProcessPacket; 
            Obj_AI_Base.OnProcessSpellCast += ObjAiHeroOnOnProcessSpellCast;

            //Detect when projectiles collide.
            Obj_SpellMissile.OnDelete += ObjSpellMissileOnOnDelete;
            //Obj_SpellMissile.OnCreate += ObjSpellMissileOnOnCreate; TODO: uncomment when missile.StartPosition works
            //GameObject.OnCreate += GameObject_OnCreate; TODO: Detect lux R and other large skillshots.
            GameObject.OnDelete += GameObject_OnDelete;
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {

        }


        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid || sender.Team == ObjectManager.Player.Team && !Config.TestOnAllies) return;
               
            for (int i = Program.DetectedSkillshots.Count - 1; i >= 0; i--)
            {
                var skillshot = Program.DetectedSkillshots[i];
                if (skillshot.SpellData.ToggleParticleName != "" && sender.Name.Contains(skillshot.SpellData.ToggleParticleName))
                    Program.DetectedSkillshots.RemoveAt(i);
            }
        }

        private static void ObjSpellMissileOnOnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_SpellMissile)) return;//not sure if needed
       
                var missile = (Obj_SpellMissile) sender;

                if (Config.PrintSpellData)
                {
                    Game.PrintChat("Projectile Created: " + missile.SData.Name);
                    Console.WriteLine(missile.SData.Name);
                }

                var unit = (Obj_AI_Base) missile.SpellCaster;
                if (!unit.IsValid || (unit.Team == ObjectManager.Player.Team && !Config.TestOnAllies)) return;
                
                var spellData = SpellDatabase.GetByMissileName(missile.SData.Name);
                if (spellData == null) return;
                
                var missilePosition = missile.Position.To2D();
                var unitPosition = missile.StartPosition.To2D();
                var endPos = missile.EndPosition.To2D();

                var castTime = Environment.TickCount - Game.Ping / 2 - spellData.Delay -
                               (int)(1000 * missilePosition.Distance(unitPosition) / spellData.MissileSpeed);

                //Trigger the skillshot detection callbacks.
                TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition, endPos,
                    unit);
            
        }

        /// <summary>
        /// Delete the missiles that collide.
        /// </summary>
        private static void ObjSpellMissileOnOnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_SpellMissile)) return;
            var missile = (Obj_SpellMissile)sender;
            var unit = (Obj_AI_Base) missile.SpellCaster;
            if (!unit.IsValid || (unit.Team == ObjectManager.Player.Team && !Config.TestOnAllies)) return;
                
            var spellName = missile.SData.Name;
            Program.DetectedSkillshots.RemoveAll(skillshot => skillshot.SpellData.MissileSpellName == spellName && skillshot.End.Distance(missile.EndPosition) < 50 && skillshot.SpellData.CanBeRemoved);
        }

        /// <summary>
        ///     This event is fired after a skillshot is detected.
        /// </summary>
        public static event OnDetectSkillshotH OnDetectSkillshot;


        private static void TriggerOnDetectSkillshot(DetectionType detectionType, SpellData spellData, int startT,
            Vector2 start, Vector2 end, Obj_AI_Base unit)
        {
            var skillshot = new Skillshot(detectionType, spellData, startT, start, end, unit);

            if (OnDetectSkillshot != null)
                OnDetectSkillshot(skillshot);
        }

        /// <summary>
        ///     Gets triggered when a unit casts a spell and the unit is visible.
        /// </summary>
        private static void ObjAiHeroOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

           
            if (Config.PrintSpellData)
            {
                Game.PrintChat(Environment.TickCount + " ProcessSpellCast: " + args.SData.Name);
            }

            if (sender.Team == ObjectManager.Player.Team && !Config.TestOnAllies) return;
            //Get the skillshot data.
            var spellData = SpellDatabase.GetByName(args.SData.Name);

            //Skillshot not added in the database.
            if (spellData == null) return;

            Vector2 startPos = new Vector2();

            if (spellData.FromObject != "")
            {
                foreach (var o in ObjectManager.Get<GameObject>())
                {
                    if (o.Name.Contains(spellData.FromObject))
                        startPos = o.Position.To2D();
                }
            }
            else
            {
                startPos = sender.ServerPosition.To2D();
            }

            if (!startPos.IsValid()) return;

            var endPos = args.End.To2D();

            if (spellData.SpellName == "LucianQ" && endPos.Distance(ObjectManager.Player.ServerPosition.To2D()) < 50)
                return;

            //Calculate the real end Point:
            var Direction = (endPos - startPos).Normalized();
            if (startPos.Distance(endPos) > spellData.Range || spellData.FixedRange)
                endPos = startPos + Direction*spellData.Range;

            if (spellData.ExtraRange != -1)
            {
                endPos = endPos + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(startPos)) * Direction;
            }

            
            //Trigger the skillshot detection callbacks.
            TriggerOnDetectSkillshot(DetectionType.ProcessSpell, spellData, Environment.TickCount - Game.Ping / 2, startPos, endPos,
                sender);
        }

        /// <summary>
        /// Detects the spells that have missile and are casted from fow.
        /// </summary>
        public static void GameOnOnGameProcessPacket(GamePacketEventArgs args)
        {
            //Gets received when a projectile is created.
            if (args.PacketData[0] == 0x3B)
            {
                var packet = new GamePacket(args.PacketData);
                
                packet.Position = 1;

                packet.ReadFloat(); //Missile network ID
                
                var missilePosition = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
                var unitPosition = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
                
                packet.Position = packet.Size() - 119;
                var missileSpeed = packet.ReadFloat();

                packet.Position = 65;
                var endPos = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());

                packet.Position = 112;
                var id = packet.ReadByte();

                
                packet.Position = packet.Size() - 83;
               
                var unit = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(packet.ReadInteger());
                if ((!unit.IsValid || unit.Team == ObjectManager.Player.Team) && !Config.TestOnAllies) return;
               
                var spellData = SpellDatabase.GetBySpeed(unit.BaseSkinName, (int) missileSpeed, id);

                if (spellData == null) return;
                
                var castTime = Environment.TickCount - Game.Ping / 2 - spellData.Delay -
                               (int)(1000 * missilePosition.SwitchYZ().To2D().Distance(unitPosition.SwitchYZ()) / spellData.MissileSpeed);
                
                //Trigger the skillshot detection callbacks.
                TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition.SwitchYZ().To2D(), endPos.SwitchYZ().To2D(),
                    unit);

            }
        }
    }
}