using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using KoreanAIO.Managers;
using KoreanAIO.Model;
using KoreanAIO.Utilities;

namespace KoreanAIO.Champions
{
    public class Cassiopeia : ChampionBase
    {
        public Dictionary<int, bool> CachedPoisoned = new Dictionary<int, bool>();

        public Cassiopeia()
        {
            Q = new SpellBase(SpellSlot.Q, SpellType.Circular, 850)
            {
                Width = 75,
                CastDelay = 400
            };
            W = new SpellBase(SpellSlot.W, SpellType.Circular, 850)
            {
                Width = 90,
                CastDelay = 250,
                Speed = 2500
            };
            E = new SpellBase(SpellSlot.E, SpellType.Targeted, 700)
            {
                Speed = 1900,
                CastDelay = 125
            };
            R = new SpellBase(SpellSlot.R, SpellType.Cone, 825)
            {
                Width = 80,
                CastDelay = 500
            };

            Spellbook.OnCastSpell += delegate (Spellbook sender, SpellbookCastSpellEventArgs args)
            {
                if (sender.Owner.IsMe)
                {
                    switch (args.Slot)
                    {
                        case SpellSlot.Q:
                            Q.LastSentTime = Core.GameTickCount;
                            Q.LastEndPosition = args.EndPosition;
                            break;
                        case SpellSlot.W:
                            W.LastSentTime = Core.GameTickCount;
                            W.LastEndPosition = args.EndPosition;
                            break;
                    }
                }
            };

            Obj_AI_Base.OnProcessSpellCast += delegate (Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (sender.IsMe)
                {
                    switch (args.Slot)
                    {
                        case SpellSlot.Q:
                            Q.LastCastTime = Core.GameTickCount;
                            Q.LastEndPosition = args.End;
                            break;
                        case SpellSlot.W:
                            W.LastCastTime = Core.GameTickCount;
                            W.LastEndPosition = args.End;
                            break;
                    }
                }
            };

            MenuManager.AddSubMenu("热键");
            {
                KeysMenu.Add("AssistedUltimate",
                    new KeyBind("大招辅助", false, KeyBind.BindTypes.HoldActive, 'T'));
                ToggleManager.RegisterToggle(
                    KeysMenu.Add("HarassToggle", new KeyBind("自动骚扰", false, KeyBind.BindTypes.PressToggle, 'K')),
                    delegate
                    {
                        if (!ModeManager.Combo)
                        {
                            Harass();
                        }
                    });
                ToggleManager.RegisterToggle(
                    KeysMenu.Add("LastHitToggle",
                        new KeyBind("自动尾兵", false, KeyBind.BindTypes.PressToggle, 'L')),
                    delegate
                    {
                        if (!ModeManager.Combo)
                        {
                            LastHit();
                        }
                    });
            }
            Q.AddConfigurableHitChancePercent();
            W.AddConfigurableHitChancePercent();
            R.AddConfigurableHitChancePercent();

            MenuManager.AddSubMenu("连招");
            {
                ComboMenu.Add("Q", new CheckBox("连招 Q"));
                ComboMenu.Add("W", new CheckBox("连招 W"));
                ComboMenu.AddStringList("E", "使用E连招", new[] { "从不", "中毒的", "一直" }, 1);
                ComboMenu.Add("R", new Slider("连招使用R如果敌人数量 >= {0}", 3, 0, 5));
            }

            MenuManager.AddSubMenu("骚扰");
            {
                HarassMenu.Add("Q", new CheckBox("骚扰 Q"));
                HarassMenu.Add("W", new CheckBox("骚扰 W"));
                HarassMenu.AddStringList("E", "使用E骚扰", new[] { "从不", "中毒的", "一直" }, 1);
                HarassMenu.Add("ManaPercent", new Slider("最低蓝量使用", 25));
            }

            MenuManager.AddSubMenu("清线");
            {
                ClearMenu.AddGroupLabel("清线");
                {
                    ClearMenu.Add("LaneClear.Q", new Slider("使用Q清线如果能命中 >= {0}", 2, 0, 10));
                    ClearMenu.Add("LaneClear.W", new Slider("使用W清线如果能命中 >= {0}", 3, 0, 10));
                    ClearMenu.AddStringList("LaneClear.E", "使用E清线", new[] { "从不", "中毒的", "一直" }, 1);
                    ClearMenu.Add("LaneClear.ManaPercent", new Slider("最低蓝量使用", 50));
                }
                ClearMenu.AddGroupLabel("尾兵");
                {
                    ClearMenu.AddStringList("LastHit.E", "使用E尾兵", new[] { "从不", "中毒的", "一直" }, 1);
                    ClearMenu.Add("LastHit.ManaPercent", new Slider("最低蓝量使用", 50));
                }
                ClearMenu.AddGroupLabel("清野");
                {
                    ClearMenu.Add("JungleClear.Q", new CheckBox("清野使用 Q"));
                    ClearMenu.Add("JungleClear.W", new CheckBox("清野使用 W"));
                    ClearMenu.Add("JungleClear.E", new CheckBox("清野使用 E"));
                    ClearMenu.Add("JungleClear.ManaPercent", new Slider("最低蓝量使用", 20));
                }
            }
            MenuManager.AddKillStealMenu();
            {
                KillStealMenu.Add("Q", new CheckBox("抢人头使用 Q"));
                KillStealMenu.Add("W", new CheckBox("抢人头使用 W"));
                KillStealMenu.Add("E", new CheckBox("抢人头使用 E"));
                KillStealMenu.Add("R", new CheckBox("抢人头使用 R", false));
            }
            MenuManager.AddSubMenu("自动");
            {
                AutomaticMenu.Add("R", new CheckBox("自动R打断敌方技能"));
            }
            MenuManager.AddDrawingsMenu();
            {
                Q.AddDrawings();
                W.AddDrawings(false);
                E.AddDrawings();
                R.AddDrawings();
                DrawingsMenu.Add("Toggle", new CheckBox("自动技能面板显示"));
            }
        }


        protected override void PermaActive()
        {
            CachedPoisoned.Clear();
            Range = Q.Range + Q.Width;
            Target = TargetSelector.GetTarget(UnitManager.ValidEnemyHeroesInRange, DamageType.Magical);
            if (ModeManager.Combo && MyHero.InAutoAttackRange(Target) && E.IsLearned && MyHero.Mana >= E.Mana &&
                (E.IsReady || E.Cooldown < 1) &&
                IsPoisoned(Target))
            {
                Orbwalker.DisableAttacking = true;
            }
            else
            {
                Orbwalker.DisableAttacking = false;
            }
            if (KeysMenu.KeyBind("AssistedUltimate"))
            {
                var enemyNear =
                    UnitManager.ValidEnemyHeroesInRange.OrderBy(client => client.Distance(MousePos, true))
                        .FirstOrDefault();
                if (enemyNear != null)
                {
                    R.Cast(enemyNear);
                }
            }
            base.PermaActive();
        }

        protected override void KillSteal()
        {
            foreach (var enemy in UnitManager.ValidEnemyHeroesInRange.Where(h => h.HealthPercent <= 40f))
            {
                var result = GetBestCombo(enemy);
                if (KillStealMenu.CheckBox("Q") && (result.Q || Q.IsKillable(enemy)))
                {
                    Q.Cast(enemy);
                }
                if (KillStealMenu.CheckBox("W") && (result.W || W.IsKillable(enemy)))
                {
                    W.Cast(enemy);
                }
                if (KillStealMenu.CheckBox("E") && (result.E || E.IsKillable(enemy)))
                {
                    E.Cast(enemy);
                }
                if (KillStealMenu.CheckBox("R") && (result.R || R.IsKillable(enemy)))
                {
                    CastR(enemy);
                }
            }
            base.KillSteal();
        }

        protected override void Combo()
        {
            if (Target != null)
            {
                if (ComboMenu.Slider("E") > 0)
                {
                    switch (ComboMenu.Slider("E"))
                    {
                        case 1:
                            CastE(Target);
                            break;
                        case 2:
                            E.Cast(Target);
                            break;
                    }
                }
                if (ComboMenu.CheckBox("Q"))
                {
                    CastQ(Target);
                }
                if (ComboMenu.CheckBox("W"))
                {
                    CastW(Target);
                }
                if (ComboMenu.Slider("R") > 0)
                {
                    if (MyHero.CountEnemiesInRange(R.Range) >= ComboMenu.Slider("R"))
                    {
                        R.Cast(Target);
                    }
                }
            }
            base.Combo();
        }

        protected override void Harass()
        {
            if (MyHero.ManaPercent >= HarassMenu.Slider("ManaPercent"))
            {
                if (Target != null)
                {
                    if (HarassMenu.CheckBox("Q"))
                    {
                        CastQ(Target);
                    }
                    if (HarassMenu.CheckBox("W"))
                    {
                        CastW(Target);
                    }
                    if (HarassMenu.Slider("E") > 0)
                    {
                        switch (HarassMenu.Slider("E"))
                        {
                            case 1:
                                CastE(Target);
                                break;
                            case 2:
                                E.Cast(Target);
                                break;
                        }
                    }
                }
            }
            base.Harass();
        }

        protected override void LaneClear()
        {
            if (MyHero.ManaPercent >= ClearMenu.Slider("LaneClear.ManaPercent"))
            {
                Q.LaneClear(true, ClearMenu.Slider("LaneClear.Q"));
                W.LaneClear(true, ClearMenu.Slider("LaneClear.W"));
                var minion = E.LaneClear(false);
                if (minion != null)
                {
                    switch (ClearMenu.Slider("LaneClear.E"))
                    {
                        case 1:
                            CastE(minion);
                            break;
                        case 2:
                            E.Cast(minion);
                            break;
                    }
                }
            }
            base.LaneClear();
        }

        protected override void JungleClear()
        {
            if (MyHero.ManaPercent >= ClearMenu.Slider("JungleClear.ManaPercent"))
            {
                if (ClearMenu.CheckBox("JungleClear.Q"))
                {
                    CastQ(Q.JungleClear(false));
                }
                if (ClearMenu.CheckBox("JungleClear.W"))
                {
                    CastW(W.JungleClear(false));
                }
                if (ClearMenu.CheckBox("JungleClear.E"))
                {
                    var minion = E.JungleClear(false);
                    if (minion != null)
                    {
                        CastE(minion);
                    }
                }
            }
            base.JungleClear();
        }

        protected override void LastHit()
        {
            if (ClearMenu.Slider("LastHit.E") > 0)
            {
                var minion = E.LastHit(LastHitType.Always, false).FirstOrDefault();
                if (minion != null)
                {
                    switch (ClearMenu.Slider("LastHit.E"))
                    {
                        case 1:
                            CastE(minion);
                            break;
                        case 2:
                            E.Cast(minion);
                            break;
                    }
                }
            }
            base.LastHit();
        }

        public void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady && target != null)
            {
                if (IsPoisoned(target))
                {
                    return;
                }
                if (W.LastSentTime > 0)
                {
                    var arrivalTime = W.GetArrivalTime(W.LastEndPosition) + 100;
                    if (Core.GameTickCount - W.LastSentTime <= arrivalTime)
                    {
                        return;
                    }
                    if (W.LastCastTime > 0 && Core.GameTickCount - W.LastCastTime <= arrivalTime)
                    {
                        return;
                    }
                }
                Q.Cast(target);
            }
        }

        public void CastW(Obj_AI_Base target)
        {
            if (W.IsReady && target != null)
            {
                if (IsPoisoned(target))
                {
                    return;
                }
                if (Q.LastSentTime > 0)
                {
                    var arrivalTime = Q.GetArrivalTime(Q.LastEndPosition) + 100;
                    if (Core.GameTickCount - Q.LastSentTime <= arrivalTime)
                    {
                        return;
                    }
                    if (Q.LastCastTime > 0 && Core.GameTickCount - Q.LastCastTime <= arrivalTime)
                    {
                        return;
                    }
                }
                W.Cast(target);
            }
        }

        public void CastE(Obj_AI_Base target)
        {
            if (E.IsReady && target != null)
            {
                var canCast = true;
                if (!IsPoisoned(target))
                {
                    canCast = false;
                    var timeToArriveE = E.GetArrivalTime(target);
                    var timeToArriveW = W.GetArrivalTime(target) - (Core.GameTickCount - W.LastCastTime);
                    var timeToArriveQ = Q.GetArrivalTime(target) - (Core.GameTickCount - Q.LastCastTime);
                    if (timeToArriveW >= 0)
                    {
                        if (timeToArriveE >= timeToArriveW)
                        {
                            var pred = W.GetPrediction(target);
                            if (pred.HitChancePercent >= W.HitChancePercent &&
                                W.LastEndPosition.Distance(pred.CastPosition, true) <=
                                (target.BoundingRadius + W.Radius).Pow())
                            {
                                canCast = true;
                            }
                        }
                    }
                    if (timeToArriveQ >= 0)
                    {
                        if (timeToArriveE >= timeToArriveQ)
                        {
                            var pred = Q.GetPrediction(target);
                            if (pred.HitChancePercent >= Q.HitChancePercent &&
                                Q.LastEndPosition.Distance(pred.CastPosition, true) <=
                                (target.BoundingRadius + Q.Radius).Pow())
                            {
                                canCast = true;
                            }
                        }
                    }
                }
                if (canCast)
                {
                    E.Cast(target);
                }
            }
        }

        public void CastR(AIHeroClient target)
        {
            if (R.IsReady && target != null && IsFacing(target))
            {
                R.Cast(target);
            }
        }

        public bool IsFacing(AIHeroClient target)
        {
            return R.GetPrediction(target).CastPosition.Distance(MyHero, true) <= target.GetDistanceSqr(MyHero);
        }

        public bool IsPoisoned(Obj_AI_Base target)
        {
            if (target == null)
            {
                return false;
            }
            if (!CachedPoisoned.ContainsKey(target.NetworkId))
            {
                CachedPoisoned[target.NetworkId] = false;
                if (target.HasBuffOfType(BuffType.Poison))
                {
                    var bestEndTime =
                        target.Buffs.Where(b => b.IsActive && b.Type == BuffType.Poison && b.EndTime > 0)
                            .OrderByDescending(b => b.EndTime)
                            .FirstOrDefault();
                    if (bestEndTime != null && bestEndTime.EndTime - Game.Time >= E.GetArrivalTime(target) / 1000f)
                    {
                        CachedPoisoned[target.NetworkId] = true;
                    }
                }
            }
            return CachedPoisoned[target.NetworkId];
        }

        public override float GetSpellDamage(SpellSlot slot, Obj_AI_Base target)
        {
            if (target != null)
            {
                var level = slot.GetSpellDataInst().Level;
                switch (slot)
                {
                    case SpellSlot.Q:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Magical,
                            40f * level + 35f + 0.45f * MyHero.FlatMagicDamageMod) / 2f;
                    case SpellSlot.W:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Magical,
                            45f * level + 45f + 0.9f * MyHero.FlatMagicDamageMod) / 2;
                    case SpellSlot.E:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Magical,
                            25f * level + 30f + 0.55f * MyHero.FlatMagicDamageMod);
                    case SpellSlot.R:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Magical,
                            100f * level + 50f + 0.5f * MyHero.FlatMagicDamageMod);
                }
            }
            return base.GetSpellDamage(slot, target);
        }
    }
}