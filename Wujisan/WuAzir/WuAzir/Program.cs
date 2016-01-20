﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;
using Version = System.Version;
using System.Net;
using System.Text.RegularExpressions;

namespace WuAzir
{
    static class Program
    {
        static Version AssVersion;//Kappa
        const string CN = "Azir";
        static Spell.Active Heal;
        static Spell.Targeted Smite, Ignite, Exhaust;
        static Spell.Skillshot Flash;
        static bool WhyIDidThatAddonInsec;
        static float LastQTime;
        const int GetFuckingInsecMana = 270;
        static Obj_AI_Minion InsecSoldier;
        static Item Mikael, Zhonya, Talisma, Hextech, Randuin, Scimitar, QSS;

        static AIHeroClient Target;
        static List<Obj_AI_Minion> AzirSoldiers = new List<Obj_AI_Minion>();
        static Menu Menu;
        readonly static Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 825, SkillShotType.Linear, 250, 1000, 70);
        readonly static Spell.Skillshot W = new Spell.Skillshot(SpellSlot.W, 450, SkillShotType.Circular);
        readonly static Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 1200, SkillShotType.Linear, 250, 1600, 100);
        readonly static Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 250, SkillShotType.Linear, 500, 1000, 532);
        readonly static AIHeroClient Player = EloBuddy.Player.Instance;

        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoadingComplete; }

        //---------------------------------------OnLoadingComplete--------------------------------------------

        static void OnLoadingComplete(EventArgs args)
        {
            if (Player.BaseSkinName != CN) { Chat.Print("抱歉，您没有使用 " + CN + ", 关闭中..."); return; }

            AssVersion = Assembly.GetExecutingAssembly().GetName().Version;
            SearchVersion();

            //-------------------------------------------------Itens--------------------------------------------------

            Mikael = new Item(3222, 600);
            Zhonya = new Item(ItemId.Zhonyas_Hourglass);
            Talisma = new Item(ItemId.Talisman_of_Ascension, 600);

            Hextech = new Item(3146, 700);
            Randuin = new Item(3143, 500);
            Scimitar = new Item(3139);
            QSS = new Item(3140);

            Q.MinimumHitChance = HitChance.Medium;
            E.MinimumHitChance = HitChance.High;
            R.MinimumHitChance = HitChance.Medium;

            Q.AllowedCollisionCount = int.MaxValue;
            R.AllowedCollisionCount = int.MaxValue;

            //-------------------------------------------------Smite--------------------------------------------------

            SpellDataInst smite = Player.Spellbook.Spells.Where(spell => spell.Name.Contains("smite")).Any() ? Player.Spellbook.Spells.Where(spell => spell.Name.Contains("smite")).First() : null;
            if (smite != null)
            {
                Smite = new Spell.Targeted(smite.Slot, 500);
            }

            //-------------------------------------------------Ignite--------------------------------------------------

            SpellDataInst dot = Player.Spellbook.Spells.Where(spell => spell.Name.Contains("dot")).Any() ? Player.Spellbook.Spells.Where(spell => spell.Name.Contains("dot")).First() : null;
            if (dot != null)
            {
                Ignite = new Spell.Targeted(dot.Slot, 600);
            }

            //-------------------------------------------------Heal--------------------------------------------------

            SpellDataInst heal = Player.Spellbook.Spells.Where(spell => spell.Name.Contains("heal")).Any() ? Player.Spellbook.Spells.Where(spell => spell.Name.Contains("heal")).First() : null;
            if (heal != null)
            {
                Heal = new Spell.Active(heal.Slot, 850);
            }

            //-------------------------------------------------Exhaust--------------------------------------------------

            SpellDataInst exhaust = Player.Spellbook.Spells.Where(spell => spell.Name.Contains("exhaust")).Any() ? Player.Spellbook.Spells.Where(spell => spell.Name.Contains("exhaust")).First() : null;
            if (exhaust != null)
            {
                Exhaust = new Spell.Targeted(exhaust.Slot, 650);
            }

            //--------------------------------------------------Flash---------------------------------------------------

            SpellDataInst flash = Player.Spellbook.Spells.Where(spell => spell.Name.Contains("flash")).Any() ? Player.Spellbook.Spells.Where(spell => spell.Name.Contains("flash")).First() : null;
            if (flash != null)
            {
                Flash = new Spell.Skillshot(flash.Slot, 425, SkillShotType.Linear);
            }

            //---------------------------||   Menus   ||----------------------------

            Menu = MainMenu.AddMenu("Wu" + CN, "Wu" + CN);

            //------------------------------Combo-------------------------------

            Menu.AddGroupLabel("连招");
            {
                Menu.Add("连招Q", new CheckBox("使用Q"));
                Menu.Add("连招Q命中率", new Slider("Q命中率%:", 65, 1, 100));
                Menu.Add("连招W", new CheckBox("使用W"));
                Menu.Add("连招E", new CheckBox("使用E"));
                Menu.Add("连招R", new CheckBox("使用R"));
                Menu.Add("使用虚弱?", new CheckBox("使用虚弱?"));
            }
            Menu.AddSeparator();

            //------------------------------Harass-------------------------------

            Menu.AddGroupLabel("骚扰");
            {
                Menu.Add("Q骚扰", new CheckBox("使用Q"));
                Menu.Add("骚扰Q命中率", new Slider("Q命中率%:", 65, 1, 100));
                Menu.Add("W骚扰", new CheckBox("使用W"));
                Menu.Add("E骚扰", new CheckBox("使用E"));
                Menu.Add("骚扰, 蓝量 %", new Slider("骚扰, 蓝量 %", 30, 1, 100));
            }
            Menu.AddSeparator();

            //------------------------------JungleClear-------------------------------

            Menu.AddGroupLabel("清野");
            {
                Menu.Add("使用Q清野", new CheckBox("使用Q"));
                Menu.Add("使用W清野", new CheckBox("使用W"));
                Menu.Add("使用E清野", new CheckBox("使用E"));
                Menu.Add("清野, 蓝量 %", new Slider("清野, 蓝量 %", 30, 1, 100));
            }
            Menu.AddSeparator();

            //------------------------------LaneClear-------------------------------

            Menu.AddGroupLabel("清线");
            {
                Menu.Add("使用Q清线", new CheckBox("使用Q"));
                Menu.Add("使用W清线", new CheckBox("使用W"));
                Menu.Add("清线, 蓝量 %", new Slider("清线, 蓝量 %", 30, 1, 100));
            }
            Menu.AddSeparator();

            //------------------------------LastHit-------------------------------

            Menu.AddGroupLabel("尾兵");
            {
                Menu.Add("使用Q尾兵", new CheckBox("使用Q"));
                Menu.Add("尾兵, 蓝量 %", new Slider("尾兵, 蓝量 %", 30, 1, 100));
            }
            Menu.AddSeparator();

            //------------------------------Flee-------------------------------

            Menu.AddGroupLabel("逃跑");
            {
                Menu.Add("使用Q逃跑", new CheckBox("使用Q"));
                Menu.Add("使用W逃跑", new CheckBox("使用W"));
                Menu.Add("使用E逃跑", new CheckBox("使用E"));
            }
            Menu.AddSeparator();

            //------------------------------Smite Usage-------------------------------

            if (Smite != null)
            {
                Menu.AddGroupLabel("惩戒");
                {
                		Menu.Add("使用惩戒?", new CheckBox("使用惩戒?"));
                		Menu.AddSeparator();
                		Menu.Add("红?", new CheckBox("红?"));
                		Menu.Add("蓝?", new CheckBox("蓝?"));
                		Menu.Add("狼?", new CheckBox("狼?"));
                		Menu.Add("蛤蟆?", new CheckBox("蛤蟆?"));
                		Menu.Add("鸟?", new CheckBox("鸟?"));
                		Menu.Add("石头人?", new CheckBox("石头人?"));
                }
                Menu.AddSeparator();
            }

            //------------------------------Drawings-------------------------------

            Menu.AddGroupLabel("线圈显示");
            {
                Menu.Add("显示Q", new CheckBox("显示Q"));
                Menu.Add("显示W", new CheckBox("显示W"));
                Menu.Add("显示R", new CheckBox("显示R"));
                if (Smite != null) Menu.Add("显示惩戒范围", new CheckBox("显示惩戒范围"));
                Menu.Add("血条显示连招总伤害", new CheckBox("血条显示连招总伤害"));
            }
            Menu.AddSeparator();

            //------------------------------Others-------------------------------

            Menu.Add("漂移推人到鼠标位置", new KeyBind("漂移推人到鼠标位置", false, KeyBind.BindTypes.HoldActive, 'J'));
            Menu.Add("神级漂移到鼠标位置", new KeyBind("神级漂移推人到鼠标位置", false, KeyBind.BindTypes.HoldActive, 'G'));
            Menu.Add("抢人头", new CheckBox("抢人头"));
            Menu.Add("R 抢人头", new CheckBox("使用 R 抢人头?"));
            Menu.Add("技能打断", new CheckBox("技能打断"));
            Menu.Add("自动点燃", new CheckBox("自动点燃"));
            Menu.AddSeparator();
            Menu.Add("使用治疗?", new CheckBox("使用治疗?"));
            Menu.Add("治疗血量设置", new Slider("自动治疗当血量为% 时:", 20, 1, 100));
            Menu.AddSeparator();
            Menu.Add("自动中亚?", new CheckBox("自动中亚?"));
            Menu.Add("中亚设置", new Slider("自动使用中亚当血量为% 时:", 15, 1, 100));
            Menu.AddSeparator();
            Menu.Add("HU3开始嘲讽", new KeyBind("HU3HU3HU3HU3", false, KeyBind.BindTypes.HoldActive, 'U'));
            Menu.Add("HU3嘲讽模式", new Slider("HU3HU3HU3HU3 Mode", 3, 1, 4));

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;

            Chat.Print("Wu" + CN + " Loaded, [By WujuSan]［chienhao 汉化］ , Version: " + AssVersion);
        }

        //-----------------------------------Orbwalker_OnUnkillableMinion-------------------------------------

        static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (!Player.IsDead && Orbwalker.ValidAzirSoldiers.Any() && Q.IsReady() && target.IsValidTarget(Q.Range) && SpellDamage(target, SpellSlot.Q) >= target.Health)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Player.ManaPercent >= Menu["清线, 蓝量 %"].Cast<Slider>().CurrentValue && Menu["使用Q清线"].Cast<CheckBox>().CurrentValue) { Q.Cast(target.ServerPosition); return; }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && Player.ManaPercent >= Menu["尾兵, 蓝量 %"].Cast<Slider>().CurrentValue && Menu["使用Q尾兵"].Cast<CheckBox>().CurrentValue) { Q.Cast(target.ServerPosition); return; }
            }

            return;
        }

        //-----------------------------------Interrupter_OnInterruptableSpell---------------------------------

        static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!Player.IsDead && e.DangerLevel == DangerLevel.High && sender.IsEnemy && sender.IsValidTarget(500))
            {
                if (sender.IsValidTarget(R.Range) && R.IsReady())
                {
                    var RPred = R.GetPrediction(sender);

                    if (RPred.HitChance >= HitChance.Medium) { R.Cast(RPred.CastPosition); return; }
                }
            }

            return;
        }

        //-----------------------------------------Drawing_OnEndScene-----------------------------------------

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (!Player.IsDead)
            {
                if (Target != null)
                {
                    if (Menu["血条显示连招总伤害"].Cast<CheckBox>().CurrentValue)
                    {
                        float FutureDamage = GetComboDamage(Target) > Target.Health ? -1 : GetComboDamage(Target) / Target.MaxHealth;

                        if (FutureDamage == -1)
                        {
                            Drawing.DrawText(Target.Position.WorldToScreen().X - 30, Target.Position.WorldToScreen().Y - 150, Color.Yellow, "可击杀");
                            return;
                        }

                        Line.DrawLine
                        (
                            Color.LightSkyBlue, 9f,
                            new Vector2(Target.HPBarPosition.X + 1, Target.HPBarPosition.Y + 9),
                            new Vector2(Target.HPBarPosition.X + 1 + FutureDamage * 104, Target.HPBarPosition.Y + 9)
                        );
                    }
                }

            }

            return;
        }

        //----------------------------------------------Drawing_OnDraw----------------------------------------

        static void Drawing_OnDraw(EventArgs args)
        {
            if (!Player.IsDead)
            {
                if (Menu["显示Q"].Cast<CheckBox>().CurrentValue)
                    Circle.Draw(Q.IsReady() ? SharpDX.Color.Green : SharpDX.Color.Red, Q.Range, Player.Position);

                if (Menu["显示W"].Cast<CheckBox>().CurrentValue)
                    Circle.Draw(W.IsReady() ? SharpDX.Color.Green : SharpDX.Color.Red, W.Range, Player.Position);

                if (Menu["显示R"].Cast<CheckBox>().CurrentValue)
                    Circle.Draw(R.IsReady() ? SharpDX.Color.Green : SharpDX.Color.Red, R.Range, Player.Position);

                if (Smite != null)
                    if (Menu["显示惩戒范围"].Cast<CheckBox>().CurrentValue)
                        Circle.Draw(Smite.IsReady() ? SharpDX.Color.Green : SharpDX.Color.Red, Smite.Range, Player.Position);

            }

            return;
        }

        //-------------------------------------------Game_OnTick----------------------------------------------

        static void Game_OnTick(EventArgs args)
        {
            if (Player.IsDead) return;

            Orbwalker.ValidAzirSoldiers.RemoveAll(it => it.Health > 0 || it.Distance(Player) >= 1300);

            if (!R.IsReady() || Player.CountEnemiesInRange(1100) == 0) { WhyIDidThatAddonInsec = false; }

            R.Width = 133 * (3 + R.Level);
            
            if (Menu["HU3开始嘲讽"].Cast<KeyBind>().CurrentValue)
            {
                switch (Menu["HU3嘲讽模式"].Cast<Slider>().CurrentValue)
                {
                    case 1:
                        EloBuddy.Player.DoEmote(Emote.Joke);
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                        break;

                    case 2:
                        EloBuddy.Player.DoEmote(Emote.Taunt);
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                        break;

                    case 3:
                        EloBuddy.Player.DoEmote(Emote.Dance);
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                        break;

                    case 4:
                        EloBuddy.Player.DoEmote(Emote.Laugh);
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                        break;

                    default:
                        break;
                }
                
            }

            if (R.IsReady() && Game.Time - LastQTime > 0.1f && Game.Time - LastQTime < 1) Player.Spellbook.CastSpell(SpellSlot.R, CursorCorrectRange(R.Range));

            if (WhyIDidThatAddonInsec) { Orbwalker.DisableAttacking = true; Orbwalker.DisableMovement = true; }
            else { Orbwalker.DisableAttacking = false; Orbwalker.DisableMovement = false; }

            if (Zhonya.IsReady() && Menu["自动中亚?"].Cast<CheckBox>().CurrentValue && Player.HealthPercent <= Menu["中亚设置"].Cast<Slider>().CurrentValue && EntityManager.Heroes.Enemies.Any(it => it.Distance(Player) <= it.GetAutoAttackRange() && it.IsValidTarget())) Zhonya.Cast();

            if (Player.CountEnemiesInRange(1000) > 0) Modes.SaveAlly();

            Target = TargetSelector.GetTarget(W.Range + Q.Range, DamageType.Magical);

            //---------------------------------------------Smite Usage---------------------------------------------

            if (Smite != null)
            {
                if (Smite.IsReady() && Menu["使用惩戒?"].Cast<CheckBox>().CurrentValue)
                {
                    Obj_AI_Minion Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, Smite.Range).FirstOrDefault();

                    if (Mob != default(Obj_AI_Minion))
                    {
                        bool kill = GetSmiteDamage() >= Mob.Health;

                        if (kill)
                        {
                            if ((Mob.Name.Contains("SRU_Dragon") || Mob.Name.Contains("SRU_Baron"))) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Red") && Menu["红?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Blue") && Menu["蓝?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Murkwolf") && Menu["浪狼?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Krug") && Menu["石头人?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Gromp") && Menu["蛤蟆?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Razorbeak") && Menu["鸟?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                        }
                    }
                }
            }

            //-----------------------------------------------Auto Ignite----------------------------------------

            if (Menu["自动点燃"].Cast<CheckBox>().CurrentValue && Ignite != null)
            {
                if (Ignite.IsReady())
                {
                    var IgniteEnemy = EntityManager.Heroes.Enemies.FirstOrDefault(it => DamageLibrary.GetSummonerSpellDamage(Player, it, DamageLibrary.SummonerSpells.Ignite) >= it.Health - 30);

                    if (IgniteEnemy != null)
                    {
                        if ((IgniteEnemy.Distance(Player) >= 300 || Player.HealthPercent <= 40))
                        {
                            Ignite.Cast(IgniteEnemy);
                        }
                    }
                }
            }

            //----------------------------------------------------KS----------------------------------------------

            if (Menu["抢人头"].Cast<CheckBox>().CurrentValue && Player.CountEnemiesInRange(W.Range + Q.Range) > 0) Modes.KS();

            //---------------------------------------------Flee Key-----------------------------------------------

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee) && E.IsReady()) Modes.Flee();

            //--------------------------------------------Orbwalker Modes-------------------------------------------

            if (Target != null)
            {
                if (Target.IsValidTarget())
                {
                    Modes.UpdateVariables();

                    if (R.IsReady() && Player.Mana >= GetFuckingInsecMana)
                    {
                        //--------------------------------------------------Insec--------------------------------------------

                        if (Menu["漂移推人到鼠标位置"].Cast<KeyBind>().CurrentValue) Insec(Target, Game.CursorPos);

                        //--------------------------------------------------WhyInsec------------------------------------------

                        else if (Menu["神级漂移到鼠标位置"].Cast<KeyBind>().CurrentValue) WhyInsec(Target, Game.CursorPos);
                    }

                    //---------------------------------------------------Combo--------------------------------------------

                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Modes.Combo();

                    //---------------------------------------------------Mixed--------------------------------------------

                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && Player.ManaPercent >= Menu["骚扰, 蓝量 %"].Cast<Slider>().CurrentValue) Modes.Harass();
                }

                else Target = null;
            }

            //---------------------------------------------------JungleClear--------------------------------------------

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && Player.ManaPercent >= Menu["清野, 蓝量 %"].Cast<Slider>().CurrentValue) Modes.JungleClear();

            //---------------------------------------------------LaneClear--------------------------------------------

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Player.ManaPercent >= Menu["清线, 蓝量 %"].Cast<Slider>().CurrentValue) Modes.LaneClear();

            return;
        }

        //------------------------------------------class Modes------------------------------------------------

        class Modes
        {
            static bool QIsReady;
            static bool WIsReady;
            static bool EIsReady;

            static bool QRange;
            static bool WRange;

            //----------------------------------------UpdateVariables()-------------------------------------------

            public static void UpdateVariables()
            {
                QIsReady = Q.IsReady();
                WIsReady = W.IsReady();
                EIsReady = E.IsReady();

                QRange = Player.Distance(Target) <= W.Range + Q.Range - 100;
                WRange = Player.Distance(Target) <= W.Range + Orbwalker.AzirSoldierAutoAttackRange - 100;

                return;
            }

            //---------------------------------------------Combo()------------------------------------------------

            public static void Combo()
            {
                if (R.IsReady() && Menu["连招R"].Cast<CheckBox>().CurrentValue && R.IsInRange(Target) && SpellDamage(Target, SpellSlot.R) >= Target.Health) R.HitChanceCast(Target, 70);

                if (W.IsReady() && (WRange || (Q.IsReady() && QRange)) && Menu["连招W"].Cast<CheckBox>().CurrentValue) { var WPos = Prediction.Position.PredictUnitPosition(Target, 1000).To3D(); W.Cast(CorrectRange(WPos, W.Range)); }

                else if (Orbwalker.ValidAzirSoldiers.Any())
                {
                    if (Q.IsReady() && Menu["连招Q"].Cast<CheckBox>().CurrentValue && QRange) Q.HitChanceCast(Target, Menu["连招Q命中率"].Cast<Slider>().CurrentValue);
                    if (E.IsReady() && Menu["连招E"].Cast<CheckBox>().CurrentValue) CastE(Target);
                }

                if (Smite != null)
                {
                    if (Smite.IsInRange(Target) && Smite.IsReady())
                    {
                        if (Smite.Name.Contains("gank")) Smite.Cast(Target);
                        else if (Smite.Name.Contains("duel") && Player.IsInAutoAttackRange(Target)) Smite.Cast(Target);
                    }
                }

                if (Talisma.IsReady() && Player.CountAlliesInRange(600) > 0) Talisma.Cast();

                if (Exhaust != null && Menu["使用虚弱?"].Cast<CheckBox>().CurrentValue && TargetSelector.GetPriority(Target) > 3 && Target.IsValidTarget(Exhaust.Range)) Exhaust.Cast(Target);

                if (Target.IsValidTarget(500) && Randuin.IsReady()) Randuin.Cast();

                if (Target.IsValidTarget(700) && Hextech.IsReady()) Hextech.Cast(Target);

                return;
            }

            //---------------------------------------------Harass()-----------------------------------------------

            public static void Harass()
            {
                if (W.IsReady() && (WRange || (Q.IsReady() && QRange)) && Menu["W骚扰"].Cast<CheckBox>().CurrentValue) { var WPos = Prediction.Position.PredictUnitPosition(Target, 500).To3D(); W.Cast(CorrectRange(WPos, W.Range)); }

                else if (Orbwalker.ValidAzirSoldiers.Any())
                {
                    if (Q.IsReady() && Menu["连招Q"].Cast<CheckBox>().CurrentValue && QRange) Q.HitChanceCast(Target, Menu["Q命中率%"].Cast<Slider>().CurrentValue);
                    if (E.IsReady() && Menu["连招E"].Cast<CheckBox>().CurrentValue) CastE(Target);
                }

                return;
            }

            //-------------------------------------------JungleClear()-----------------------------------------------

            public static void JungleClear()
            {
                if (Menu["使用W清野"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    var WMinion = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, W.Range).FirstOrDefault();
                    if (WMinion != null) { var WPos = Player.Position.Extend(WMinion, Player.Distance(WMinion) / 2).To3D(); W.Cast(WPos); }
                }

                if (Menu["使用E清野"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    var EMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, E.Range);
                    if (EMinions.Any()) CastE(EMinions);
                }

                if (Menu["使用Q清野"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    var QMinion = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, Q.Range).FirstOrDefault();

                    if (QMinion != null) Q.HitChanceCast(QMinion, 40);
                }

                return;
            }

            //-------------------------------------------LaneClear()-----------------------------------------------

            public static void LaneClear()
            {
                if (Orbwalker.ValidAzirSoldiers.Any(it => it.Distance(Player) <= 900))
                {
                    if (Q.IsReady() && Menu["使用Q清线"].Cast<CheckBox>().CurrentValue)
                    {
                        var WPos = BestCircularFarmLocation(250, (int)Q.Range);
                        if (WPos != default(Vector3)) Q.Cast(WPos);
                    }
                    
                }

                else if (W.IsReady() && Menu["使用W清线"].Cast<CheckBox>().CurrentValue)
                {
                    var WPos = BestCircularFarmLocation(250, (int)W.Range);
                    if (WPos != default(Vector3)) W.Cast(WPos);
                }

                return;
            }

            //-------------------------------------------------KS--------------------------------------------------

            public static void KS()
            {
                if (Orbwalker.ValidAzirSoldiers.Any())
                {
                    if (Q.IsReady())
                    {
                        var bye = EntityManager.Heroes.Enemies.FirstOrDefault(it => it.IsValidTarget(Q.Range) && SpellDamage(it, SpellSlot.Q) >= it.Health);
                        if (bye != null) Q.HitChanceCast(bye, 70);
                    }

                    if (E.IsReady())
                    {
                        var bye = EntityManager.Heroes.Enemies.FirstOrDefault(it => it.IsValidTarget(E.Range) && SpellDamage(it, SpellSlot.E) >= it.Health);
                        if (bye != null) { CastE(bye); return; }
                    }
                }

                else if (W.IsReady())
                {
                    if (Q.IsReady())
                    {
                        var bye = EntityManager.Heroes.Enemies.FirstOrDefault(it => it.IsValidTarget(Q.Range + W.Range - 150) && SpellDamage(it, SpellSlot.Q) + SpellDamage(it, SpellSlot.W) >= it.Health);
                        if (bye != null)
                        {
                            if (W.Cast(CorrectRange(bye.ServerPosition, W.Range))) Core.DelayAction(() => Q.HitChanceCast(bye, 70), 50);
                        }
                    }
                }

                if (Menu["R 抢人头"].Cast<CheckBox>().CurrentValue && R.IsReady())
                {
                    var bye = EntityManager.Heroes.Enemies.FirstOrDefault(it => it.IsValidTarget(R.Range) && SpellDamage(it, SpellSlot.R) >= it.Health);
                    if (bye != null) { R.HitChanceCast(bye, 70); return; }
                }

                if (Smite != null)
                {
                    if (Smite.Name.Contains("gank") && Smite.IsReady())
                    {
                        var bye = EntityManager.Heroes.Enemies.FirstOrDefault(enemy => enemy.IsValidTarget(Smite.Range) && DamageLibrary.GetSummonerSpellDamage(Player, enemy, DamageLibrary.SummonerSpells.Smite) >= enemy.Health);
                        if (bye != null) { Smite.Cast(bye); return; }
                    }
                }

                var WEnemy = EntityManager.Heroes.Enemies.FirstOrDefault(it => Orbwalker.ValidAzirSoldiers.Any(enemy => enemy.Distance(it) <= 275));

                if (WEnemy != null && SpellDamage(WEnemy, SpellSlot.W) >= WEnemy.Health) EloBuddy.Player.IssueOrder(GameObjectOrder.AttackTo, WEnemy);

                return;
            }

            //----------------------------------------------Flee------------------------------------------------

            public static void Flee()
            {
                if (W.IsReady() && Q.IsReady())
                {
                    var WPos = CursorCorrectRange(W.Range);
                    if ( W.Cast( WPos ) )
                    {
                        int EDelay = (int)((Player.Distance(WPos) - 150) / 8 * 5);

                        if ( E.Cast(CursorCorrectRange(W.Range)))
                        {
                            Core.DelayAction(delegate
                            {
                                Q.Cast(CursorCorrectRange(Q.Range));
                            }, EDelay);
                        }
                    }
                }

                if (W.IsReady())
                {
                    if ( W.Cast( CursorCorrectRange(W.Range) ) ) Core.DelayAction(() => E.Cast( CursorCorrectRange(E.Range) ), Game.Ping + 20);
                }

                if (Orbwalker.ValidAzirSoldiers.Any(it => it.Distance(Game.CursorPos) <= 150)) E.Cast(CursorCorrectRange(E.Range));

                if (Orbwalker.ValidAzirSoldiers.Any() && Q.IsReady())
                {
                    if (Q.Cast(CursorCorrectRange(Q.Range))) Core.DelayAction(() => E.Cast(CursorCorrectRange((E.Range))), Game.Ping + 20);
                }

                return;
            }

            //----------------------------------------------SaveAlly-----------------------------------------------

            public static void SaveAlly()
            {
                if (Mikael.IsReady())
                {
                    var Ally = EntityManager.Heroes.Allies.FirstOrDefault(ally => EntityManager.Heroes.Enemies.Any(enemy => ally.IsFacing(enemy)) && ally.HealthPercent <= 30 && Player.Distance(ally) <= 750);

                    if (Ally != null)
                    {
                        if ((Ally.HasBuffOfType(BuffType.Charm) || Ally.HasBuffOfType(BuffType.Fear) || Ally.HasBuffOfType(BuffType.Poison) || Ally.HasBuffOfType(BuffType.Polymorph) || Ally.HasBuffOfType(BuffType.Silence) || Ally.HasBuffOfType(BuffType.Sleep) || Ally.HasBuffOfType(BuffType.Slow) || Ally.HasBuffOfType(BuffType.Snare) || Ally.HasBuffOfType(BuffType.Stun) || Ally.HasBuffOfType(BuffType.Taunt))) Mikael.Cast(Ally);
                    }
                }

                if (Heal != null && Heal.IsReady() && Menu["使用治疗?"].Cast<CheckBox>().CurrentValue)
                {
                    var healtarget = EntityManager.Heroes.Allies.FirstOrDefault(it => it.IsValidTarget(Heal.Range) && it.HealthPercent <= Menu["治疗血量设置"].Cast<Slider>().CurrentValue);

                    if (healtarget != null)
                    {
                        if (EntityManager.Heroes.Enemies.Any(it => it.IsValidTarget() && it.Distance(healtarget) <= it.GetAutoAttackRange())) Heal.Cast();
                    }
                }

                return;
            }

        }

        //----------------------------------BestCircularFarmLocation()-----------------------------------------

        static Vector3 BestCircularFarmLocation(int radius, int range)
        {
            var minions = EntityManager.MinionsAndMonsters.CombinedAttackable.Where(it => it.IsValidTarget(range));
            
            if (minions.Any() && minions.Count() == 1) return default(Vector3);

            var hitsperminion = new List<int>();
            int hits = new int();

            for (int i = 0; i < minions.Count(); i++)
            {
                hits = 1;

                for (int j = 0; j < minions.Count(); j++)
                {
                    if (j == i) continue;

                    if (minions.ElementAt(i).Distance(minions.ElementAt(j)) <= radius) hits++;
                }

                hitsperminion.Add(hits);
            }

            if (hitsperminion.Any() && hitsperminion.Max() > 1)
            {
                var pos = minions.ElementAt(hitsperminion.IndexOf(hitsperminion.Max())).ServerPosition;

                return pos;
            }

            return default(Vector3);
        }

        //----------------------------------------CorrectRange()-----------------------------------------------

        static Vector3 CursorCorrectRange(uint range)
        {
            if (Player.Distance(Game.CursorPos) <= range) return Game.CursorPos;
            return Player.Position.Extend(Game.CursorPos, range-25).To3D();
        }

        //----------------------------------------CorrectRange()-----------------------------------------------

        static Vector3 CorrectRange(Vector3 pos, uint range)
        {
            if (Player.Distance(pos) <= range) return pos;
            return Player.Position.Extend(pos, range-25).To3D();
        }

        //---------------------------------------Insec()-------------------------------------------------------

        static void Insec(Obj_AI_Base target, Vector3 cursorpos)
        {
            //Back distance = 300
            
            //Normal Insec

            if (!WhyIDidThatAddonInsec && Player.Distance(target) <= W.Range + Q.Range - 300)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, target);

                if (Player.Distance(target) < 180)
                {
                    var tower = EntityManager.Turrets.Allies.FirstOrDefault(it => it.IsValidTarget(1000));

                    if (tower != null)
                    {
                        if (EloBuddy.Player.CastSpell(SpellSlot.R, CorrectRange(tower.Position, R.Range))) return;
                    }

                    if (EloBuddy.Player.CastSpell(SpellSlot.R, CursorCorrectRange(R.Range))) return;
                }

                else if (E.IsReady())
                {
                    if (Orbwalker.ValidAzirSoldiers.Any(it => it.Distance(Player) <= 900))
                    {
                        var ESoldier = Orbwalker.ValidAzirSoldiers.FirstOrDefault(it => it.Distance(target) <= 200);

                        if (ESoldier != null) E.Cast(CorrectRange(ESoldier.Position, E.Range));

                        else if (Q.IsReady())
                        {
                            Q.HitChanceCast(target, 60);
                        }
                    }

                    else if (W.IsReady())
                    {
                        var WPos = Prediction.Position.PredictUnitPosition(target, 1000).To3D();
                        W.Cast(CorrectRange(WPos, W.Range));
                        return;
                    }
                }
            }

            return;
        }

        //---------------------------------------WhyInsec()-------------------------------------------------------

        static void WhyInsec(Obj_AI_Base target, Vector3 cursorpos)
        {
            //Back distance = 300

            //Why I did that

            if (!WhyIDidThatAddonInsec && Orbwalker.ValidAzirSoldiers.Any(it => it.Distance(target) >= E.Width + target.BoundingRadius && it.Distance(target) <= (R.Width/2) - 50))
            {
                if (!WhyIDidThatAddonInsec)

                WhyIDidThatAddonInsec = true;

                Core.DelayAction(() => WhyIDidThatAddonInsec = false, 3000);

                if (E.IsReady())
                {
                    InsecSoldier = Orbwalker.ValidAzirSoldiers.Where(it => it.Distance(target) <= (R.Width / 2) - 50).OrderByDescending(it => it.Distance(target)).First();

                    var targetpos = Prediction.Position.PredictUnitPosition(target, 500).To3D();

                    var rectangle = new Geometry.Polygon.Rectangle(Player.Position, InsecSoldier.Position, E.Width + target.BoundingRadius);

                    if (!rectangle.IsInside(targetpos) && InsecSoldier.Distance(target) <= (R.Width / 2) - 50)
                    {
                        var EDelay = (int)((((Player.Distance(InsecSoldier) - 100) / 8) * 5));

                        if (E.Cast(CorrectRange(InsecSoldier.Position, E.Range)))
                        {
                            //Delayed insec

                            Core.DelayAction(delegate
                            {
                                if (Player.Spellbook.CastSpell(SpellSlot.Q, CursorCorrectRange(Q.Range)))
                                {
                                    LastQTime = Game.Time;
                                }

                                else WhyIDidThatAddonInsec = false;
                            }, EDelay);
                        }
                        else WhyIDidThatAddonInsec = false;
                    }
                    else { WhyIDidThatAddonInsec = false; }
                }

                else WhyIDidThatAddonInsec = false;
            }

            return;
        }

        //------------------------------------CastE(Obj_AI_Base target)----------------------------------------

        static void CastE(Obj_AI_Base target)
        {
            foreach (var soldier in Orbwalker.ValidAzirSoldiers)
            {
                var rectangle = new Geometry.Polygon.Rectangle(Player.Position, soldier.Position, 90);

                if (rectangle.IsInside(target))
                {
                    if (E.Cast(target)) return;
                }
            }
            
            return;
        }

        //----------------------------------CastE(List<Obj_AI_Base> targets)----------------------------------

        static void CastE(IEnumerable<Obj_AI_Base> targets)
        {
            var rectangles = new List<Geometry.Polygon.Rectangle>();

            foreach (var soldier in Orbwalker.ValidAzirSoldiers)
            {
                rectangles.Add(new Geometry.Polygon.Rectangle(Player.Position, soldier.Position, 90));
            }

            if ( targets.Any( it => rectangles.Any( rectangle => rectangle.IsInside(it) ) ) ) E.Cast(Player.Position);

            return;
        }

        //------------------------------------------HitChanceCast()--------------------------------------------

        static void HitChanceCast(this Spell.Skillshot spell, Obj_AI_Base target, float chance)
        {
            var Pred = spell.GetPrediction(target);

            if (Pred.HitChancePercent >= chance)
            {
                var pos = Player.Position.Extend(Pred.CastPosition, Player.Distance(Pred.CastPosition) + 300);
                spell.Cast(Pred.CastPosition);
            }
        }

        //------------------------SpellDamage(Obj_AI_Base target, SpellSlot slot)------------------------------

        static float SpellDamage(Obj_AI_Base target, SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                    return Player.CalculateDamageOnUnit(target, DamageType.Magical, new int[] { 65, 85, 105, 125, 145 }[Q.Level - 1] + Player.TotalMagicalDamage/2);
                case SpellSlot.W:
                    if (Orbwalker.ValidAzirSoldiers.Any() || W.IsReady()) return Player.CalculateDamageOnUnit(target, DamageType.Magical, new int[] { 50, 55, 60, 65, 70, 75, 80, 85, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180}[Player.Level-1] + 0.6f * Player.TotalMagicalDamage, true, true);
                    return 0;
                case SpellSlot.E:
                    return Player.CalculateDamageOnUnit(target, DamageType.Magical, 30 * (E.Level + 1) + 0.4f * Player.TotalMagicalDamage);
                case SpellSlot.R:
                    return Player.CalculateDamageOnUnit(target, DamageType.Magical, new int[] { 150, 225, 300 }[R.Level - 1] + 0.6f * Player.TotalMagicalDamage);
                default:
                    return 0;
            }
        }

        //---------------------------------GetComboDamage(Obj_AI_Hero target)----------------------------------
        
        static float GetComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float ComboDamage = new float();

                ComboDamage = Q.IsReady() ? SpellDamage(target, SpellSlot.Q) : 0;
                ComboDamage += SpellDamage(target, SpellSlot.W) * 2;
                ComboDamage += E.IsReady() ? SpellDamage(target, SpellSlot.E) : 0;
                ComboDamage += R.IsReady() ? SpellDamage(target, SpellSlot.R) : 0;
                ComboDamage += Player.TotalAttackDamage;
                if (Ignite != null) ComboDamage += Convert.ToSingle(Ignite.IsReady() && Ignite.IsInRange(target) ? DamageLibrary.GetSummonerSpellDamage(Player, target, DamageLibrary.SummonerSpells.Ignite) : 0);
                if (Smite != null) ComboDamage += Convert.ToSingle(Ignite.IsReady() && Smite.Name.Contains("gank") && Ignite.IsInRange(target) ? DamageLibrary.GetSummonerSpellDamage(Player, target, DamageLibrary.SummonerSpells.Smite) : 0);

                return ComboDamage;
            }
            return 0;
        }

        //------------------------------------------GetSmiteDamage()-------------------------------------------

        static float GetSmiteDamage()
        {
            float damage = new float(); //Arithmetic Progression OP :D

            if (Player.Level < 10) damage = 360 + (Player.Level - 1) * 30;

            else if (Player.Level < 15) damage = 280 + (Player.Level - 1) * 40;

            else if (Player.Level < 19) damage = 150 + (Player.Level - 1) * 50;

            return damage;
        }

        //--------------------------------------------SearchVersion()-------------------------------------------

        static void SearchVersion()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    string Text = new WebClient().DownloadString("https://raw.githubusercontent.com/WujuSan/EloBuddy/master/Wu" + CN + "/Wu" + CN + "/Properties/AssemblyInfo.cs");

                    var Match = new Regex(@"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]").Match(Text);

                    if (Match.Success)
                    {
                        var CorrectVersion = new Version(string.Format("{0}.{1}.{2}.{3}", Match.Groups[1], Match.Groups[2], Match.Groups[3], Match.Groups[4]));

                        if (CorrectVersion > AssVersion)
                        {
                            Chat.Print("<font color='#FFFF00'>Your Wu" + CN + " is </font><font color='#FF0000'>OUTDATED</font><font color='#FFFF00'>, The correct version is: " + CorrectVersion + "</font>");
                            Chat.Print("<font color='#FFFF00'>Your Wu" + CN + " is </font><font color='#FF0000'>OUTDATED</font><font color='#FFFF00'>, The correct version is: " + CorrectVersion + "</font>");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e + "\n [ [RIP] Search ]");
                }
            });
        }

    }//Class End
}
