﻿#region

using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace Evade
{
    internal static class Config
    {
        public const bool PrintSpellData = false;

        public const bool TestOnAllies = false;
        public const int SkillShotsExtraRadius = 10;
        public const int SkillShotsExtraRange = 20;
        public const int GridSize = 10;
        public const int ExtraEvadeDistance = 15;
        public const int DiagonalEvadePointsCount = 7;
        public const int DiagonalEvadePointsStep = 20;

        public const int CrossingTimeOffset = 250;

        public const int EvadingFirstTimeOffset = 250;
        public const int EvadingSecondTimeOffset = 0;

        public const int EvadingRouteChangeTimeOffset = 250;

        public const int EvadePointChangeInterval = 300;
        public static int LastEvadePointChangeT = 0;

        public static Menu Menu;

        public static void CreateMenu()
        {
            
            Menu = new Menu("Evade", "Evade", true);
            
            //Create the evade spells submenus.
            var evadeSpells = new Menu("Evade spells", "evadeSpells");
            foreach (var spell in EvadeSpellDatabase.Spells)
            {
                var subMenu = new Menu(spell.Name, spell.Name);

                subMenu.AddItem(new MenuItem("DangerLevel" + spell.Name, "Danger level").SetValue(new Slider(spell.DangerLevel,
                                    5, 1)));

                subMenu.AddItem(new MenuItem("Enabled" + spell.Name, "Enabled").SetValue(true));

                evadeSpells.AddSubMenu(subMenu);
            }
            Menu.AddSubMenu(evadeSpells);

            //Create the skillshots submenus.
            var skillShots = new Menu("Skillshots", "Skillshots");

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.Team != ObjectManager.Player.Team || Config.TestOnAllies)
                {
                    foreach (var spell in SpellDatabase.Spells)
                    {
                        if (spell.BaseSkinName == hero.BaseSkinName)
                        {
                            var subMenu = new Menu(spell.MenuItemName, spell.MenuItemName);

                            subMenu.AddItem(
                                new MenuItem("DangerLevel" + spell.MenuItemName, "Danger level").SetValue(new Slider(spell.DangerValue,
                                    5, 1)));

                            subMenu.AddItem(new MenuItem("IsDangerous" + spell.MenuItemName, "Is Dangerous").SetValue(spell.IsDangerous));

                            subMenu.AddItem(new MenuItem("Draw" + spell.MenuItemName, "Draw").SetValue(true));
                            subMenu.AddItem(new MenuItem("Enabled" + spell.MenuItemName, "Enabled").SetValue(true));

                            skillShots.AddSubMenu(subMenu);
                        }
                    }
                }
            }

            Menu.AddSubMenu(skillShots);

            var shielding = new Menu("Ally shielding", "Shielding");

            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (ally.IsAlly && !ally.IsMe)
                    shielding.AddItem(new MenuItem("shield" + ally.BaseSkinName, "Shield " + ally.BaseSkinName).SetValue(true));

            }
            Menu.AddSubMenu(shielding);

            var drawings = new Menu("Drawings", "Drawings");
            drawings.AddItem(new MenuItem("EnabledColor", "Enabled spell color").SetValue(Color.White));
            drawings.AddItem(new MenuItem("DisabledColor", "Disabled spell color").SetValue(Color.Red));
            drawings.AddItem(new MenuItem("Border", "Border Width").SetValue(new Slider(1, 5, 1)));

            drawings.AddItem(new MenuItem("EnableDrawings", "Enabled").SetValue(true));
            Menu.AddSubMenu(drawings);

            Menu.AddItem(
                new MenuItem("Enabled", "Enabled").SetValue(new KeyBind("K".ToCharArray()[0], KeyBindType.Toggle,
                    true)));

            Menu.AddItem(
                new MenuItem("OnlyDangerous", "Dodge only dangerous").SetValue(new KeyBind(32, KeyBindType.Press)));

            Menu.AddToMainMenu();
        }
    }
}