﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using KoreanAIO.Managers;
using KoreanAIO.Model;
using KoreanAIO.Utilities;
using SharpDX;
using Circle = KoreanAIO.Managers.Circle;

namespace KoreanAIO.Champions
{
    public class Zed : ChampionBase
    {
        public const string ShadowSkinName = "ZedShadow";
        public const string IsDeadName = "Zed_Base_R_buf_tell.troy";

        public const int WRange = 700;

        public const int QReducedSqr = 693056; // (0.9 * Q.Range).Pow()
        public GameObject IsDeadObject;
        public Text IsDeadText;

        public Obj_AI_Minion RShadow;

        public Obj_AI_Minion WShadow;

        public Zed()
        {
            Q = new SpellBase(SpellSlot.Q, SpellType.Linear, 925)
            {
                CastDelay = 250,
                Speed = 1700,
                Width = 50
            };
            Q.SetSourceFunction(() => MyHero);
            Q.SetRangeCheckSourceFunction(() => MyHero);
            W = new SpellBase(SpellSlot.W, SpellType.Linear, WRange)
            {
                Speed = 1750,
                Width = 60
            };
            E = new SpellBase(SpellSlot.E, SpellType.Self, 280);
            E.SetSourceFunction(() => MyHero);
            E.SetRangeCheckSourceFunction(() => MyHero);
            R = new SpellBase(SpellSlot.R, SpellType.Targeted, 625);
            IsDeadText = new Text("接受死亡吧", new Font("Arial", 30F, FontStyle.Bold))
            {
                Color = System.Drawing.Color.Red,
                Position = new Vector2(100, 50)
            };
            Obj_AI_Base.OnBuffGain += delegate (Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
            {
                var minion = sender as Obj_AI_Minion;
                if (minion != null && minion.IsAlly && minion.BaseSkinName == ShadowSkinName && args.Buff.Caster.IsMe)
                {
                    switch (args.Buff.Name)
                    {
                        case "zedwshadowbuff":
                            WShadow = minion;
                            break;
                        case "zedrshadowbuff":
                            RShadow = minion;
                            break;
                    }
                }
            };
            Obj_AI_Base.OnPlayAnimation += delegate (Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
            {
                var minion = sender as Obj_AI_Minion;
                if (minion != null && minion.IsAlly && minion.BaseSkinName == ShadowSkinName)
                {
                    if (args.Animation == "Death")
                    {
                        if (WShadow.IdEquals(minion))
                        {
                            WShadow = null;
                        }
                        else if (RShadow.IdEquals(minion))
                        {
                            RShadow = null;
                        }
                    }
                }
            };
            GameObject.OnCreate += delegate (GameObject sender, EventArgs args)
            {
                if (sender.Name == IsDeadName && RTarget != null && RTarget.IsInRange(sender, 200))
                {
                    IsDeadObject = sender;
                }
            };
            GameObject.OnDelete += delegate (GameObject sender, EventArgs args)
            {
                var minion = sender as Obj_AI_Minion;
                if (minion != null && minion.IsAlly && minion.BaseSkinName == ShadowSkinName)
                {
                    if (WShadow.IdEquals(minion))
                    {
                        WShadow = null;
                    }
                    else if (RShadow.IdEquals(minion))
                    {
                        RShadow = null;
                    }
                }
                else if (sender.IdEquals(IsDeadObject))
                {
                    IsDeadObject = null;
                }
            };
            Obj_AI_Base.OnProcessSpellCast += delegate (Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (sender.IsMe)
                {
                    switch (args.Slot)
                    {
                        case SpellSlot.W:
                            if (args.SData.Name == "ZedW")
                            {
                                W.LastCastTime = Core.GameTickCount;
                                W.LastEndPosition = args.End;
                            }
                            break;
                        case SpellSlot.R:
                            if (args.SData.Name == "ZedR")
                            {
                            }
                            break;
                    }
                }
            };
            Evader.OnEvader += delegate (EvaderArgs args)
            {
                var w1Distance = (W.IsReady && IsW1 && EvaderMenu.CheckBox("Evader.W1"))
                    ? MyHero.GetDistanceSqr(args.Sender)
                    : 16000000;
                var w2Distance = (W.IsReady && WShadowIsValid && EvaderMenu.CheckBox("Evader.W2") && !args.WillHitMyHero(WShadow.Position))
                    ? WShadow.GetDistanceSqr(args.Sender)
                    : 16000000;
                var rTarget = TargetSelector.GetTarget(R.Range, DamageType.Physical) ?? args.Sender;
                var r1Distance = (R.IsReady && IsR1 && rTarget != null && R.InRange(rTarget) && EvaderMenu.CheckBox("Evader.R1"))
                    ? MyHero.GetDistanceSqr(args.Sender)
                    : 16000000;
                var r2Distance = (R.IsReady && RShadowIsValid && EvaderMenu.CheckBox("Evader.R2") && !args.WillHitMyHero(RShadow.Position))
                    ? RShadow.GetDistanceSqr(args.Sender)
                    : 16000000;
                var min = Math.Min(w1Distance, Math.Min(w2Distance, Math.Min(r1Distance, r2Distance)));
                if (min < 16000000)
                {
                    if (Math.Abs(min - r2Distance) < float.Epsilon)
                    {
                        R.Cast();
                    }
                    else if (Math.Abs(min - r1Distance) < float.Epsilon)
                    {
                        R.Cast(rTarget);
                    }
                    else if (Math.Abs(min - w2Distance) < float.Epsilon)
                    {
                        W.Cast();
                    }
                    else if (Math.Abs(min - w1Distance) < float.Epsilon)
                    {
                        var wPos = MyHero.ServerPosition +
                                   ((args.Sender.ServerPosition - MyHero.ServerPosition).Normalized() * WRange)
                                       .To2D()
                                       .Perpendicular()
                                       .To3DWorld();
                        W.Cast(wPos);
                    }
                }
            };

            MenuManager.AddSubMenu("热键");
            {
                Orbwalker.RegisterKeyBind(
                    KeysMenu.Add("Combo2", new KeyBind("不用R连招", false, KeyBind.BindTypes.HoldActive, 'A')),
                    Orbwalker.ActiveModes.Combo);
                Orbwalker.RegisterKeyBind(
                    KeysMenu.Add("Harass2", new KeyBind("骚扰 2", false, KeyBind.BindTypes.HoldActive, 'S')),
                    Orbwalker.ActiveModes.Harass);
            }

            Q.AddConfigurableHitChancePercent();

            MenuManager.AddSubMenu("连招");
            {
                ComboMenu.AddStringList("Mode", "R 模式", new[] { "线性", "三角", "鼠标位置" });
                ComboMenu.Add("Q", new CheckBox("使用 Q"));
                ComboMenu.Add("W", new CheckBox("使用 W"));
                ComboMenu.Add("E", new CheckBox("使用 E"));
                ComboMenu.Add("R", new CheckBox("使用 R"));
                ComboMenu.Add("Items", new CheckBox("使用物品"));
                ComboMenu.Add("SwapDead", new CheckBox("使用 W2/R2 如果可击杀敌人"));
                ComboMenu.Add("SwapGapclose", new CheckBox("使用 W2/R2 接近目标"));
                ComboMenu.Add("SwapHP", new Slider("使用 W2/R2 如果生命 <= {0}", 15));
                ComboMenu.Add("Prevent", new CheckBox("R之前不使用技能"));
                if (EntityManager.Heroes.Enemies.Count > 0)
                {
                    ComboMenu.AddGroupLabel("对敌人不使用R");
                    var enemiesAdded = new HashSet<string>();
                    foreach (var enemy in EntityManager.Heroes.Enemies)
                    {
                        if (!enemiesAdded.Contains(enemy.ChampionName))
                        {
                            ComboMenu.Add("BlackList." + enemy.ChampionName, new CheckBox(enemy.ChampionName, false));
                            enemiesAdded.Add(enemy.ChampionName);
                        }
                    }
                }
            }
            MenuManager.AddSubMenu("骚扰");
            {
                HarassMenu.Add("Collision", new CheckBox("为Q检查施法路径 （更多伤害）", false));
                HarassMenu.Add("SwapGapclose", new CheckBox("使用W2如果可击杀"));
                HarassMenu.AddGroupLabel("骚扰 1");
                HarassMenu.Add("Harass1.Q", new CheckBox("使用 Q"));
                HarassMenu.Add("Harass1.W", new CheckBox("使用 W", false));
                HarassMenu.Add("Harass1.E", new CheckBox("使用 E"));
                HarassMenu.Add("Harass1.ManaPercent", new Slider("最低能量使用:", 20));
                HarassMenu.AddGroupLabel("骚扰 2");
                HarassMenu.Add("Harass2.Q", new CheckBox("使用 Q"));
                HarassMenu.Add("Harass2.W", new CheckBox("使用 W"));
                HarassMenu.Add("Harass2.E", new CheckBox("使用 E"));
            }

            MenuManager.AddSubMenu("清线");
            {
                ClearMenu.AddGroupLabel("清线");
                {
                    ClearMenu.Add("LaneClear.Q", new Slider("使用Q如果命中敌人数量 >= {0}", 3, 0, 10));
                    ClearMenu.Add("LaneClear.W", new Slider("使用W如果命中敌人数量 >= {0}", 4, 0, 10));
                    ClearMenu.Add("LaneClear.E", new Slider("使用E如果命中敌人数量 >= {0}", 3, 0, 10));
                    ClearMenu.Add("LaneClear.ManaPercent", new Slider("最低能量使用", 50));
                }
                ClearMenu.AddGroupLabel("尾兵");
                {
                    ClearMenu.AddStringList("LastHit.Q", "使用 Q", new[] { "从不", "中毒的", "一直" }, 1);
                    ClearMenu.AddStringList("LastHit.E", "使用 E", new[] { "从不", "中毒的", "一直" }, 1);
                    ClearMenu.Add("LastHit.ManaPercent", new Slider("最低能量使用", 50));
                }
                ClearMenu.AddGroupLabel("清野");
                {
                    ClearMenu.Add("JungleClear.Q", new CheckBox("使用 Q"));
                    ClearMenu.Add("JungleClear.W", new CheckBox("使用 W"));
                    ClearMenu.Add("JungleClear.E", new CheckBox("使用 W"));
                    ClearMenu.Add("JungleClear.ManaPercent", new Slider("最低能量使用", 20));
                }
            }

            MenuManager.AddKillStealMenu();
            {
                KillStealMenu.Add("Q", new CheckBox("使用 Q"));
                KillStealMenu.Add("W", new CheckBox("使用 W"));
                KillStealMenu.Add("E", new CheckBox("使用 E"));
            }

            MenuManager.AddSubMenu("自动");
            {
                AutomaticMenu.Add("E", new CheckBox("自动使用 E", false));
                AutomaticMenu.Add("SwapDead", new CheckBox("自动使用 W2/R2 如果目标能击杀", false));
            }
            MenuManager.AddSubMenu("Evader");
            {
                EvaderMenu.Add("Evader.W1", new CheckBox("使用 W1", false));
                EvaderMenu.Add("Evader.W2", new CheckBox("使用 W2"));
                EvaderMenu.Add("Evader.R1", new CheckBox("使用 R1"));
                EvaderMenu.Add("Evader.R2", new CheckBox("使用 R2"));
            }
            Evader.Initialize();
            Evader.AddCrowdControlSpells();
            Evader.AddDangerousSpells();
            MenuManager.AddDrawingsMenu();
            {
                Q.AddDrawings();
                W.AddDrawings();
                E.AddDrawings(false);
                R.AddDrawings();
                CircleManager.Circles.Add(new Circle(
                    DrawingsMenu.Add("W.Shadow", new CheckBox("显示W线圈范围")), SharpDX.Color.Blue,
                    () => 100, () => WShadowIsValid,
                    () => WShadow)
                { Width = 1 });

                CircleManager.Circles.Add(new Circle(
                    DrawingsMenu.Add("R.Shadow", new CheckBox("显示R线圈范围")), SharpDX.Color.Orange,
                    () => 100, () => RShadowIsValid,
                    () => RShadow)
                { Width = 1 });
                DrawingsMenu.Add("IsDead", new CheckBox("显示目标可击杀提示"));
                DrawingsMenu.Add("Passive", new CheckBox("显示主动技能冷却提示"));
            }

        }

        public bool RShadowIsValid
        {
            get { return RShadow != null && RShadow.IsValid && !RShadow.IsDead; }
        }

        public bool WShadowIsValid
        {
            get { return WShadow != null && WShadow.IsValid && !WShadow.IsDead; }
        }

        public bool IsW1
        {
            get { return W.Instance.Name == "ZedW"; }
        }

        public bool IsR1
        {
            get { return R.Instance.Name == "ZedR"; }
        }

        private bool IsWaitingShadow
        {
            get { return WShadow == null && W.LastCastTime > 0 && Core.GameTickCount - W.LastCastTime <= W.GetArrivalTime(W.LastEndPosition) + Game.Ping / 2 - Q.CastDelay - 80; }
        }

        private bool IsCombo2
        {
            get { return KeysMenu.KeyBind("Combo2"); }
        }

        private bool IsHarass2
        {
            get { return KeysMenu.KeyBind("Harass2"); }
        }

        private Obj_AI_Base RTarget
        {
            get { return UnitManager.ValidEnemyHeroes.FirstOrDefault(TargetHaveR); }
        }

        public override void OnEndScene()
        {
            if (DrawingsMenu.CheckBox("IsDead"))
            {
                if (IsDeadObject != null)
                {
                    var enemyDead = UnitManager.ValidEnemyHeroes.FirstOrDefault(IsDead);
                    if (enemyDead != null)
                    {
                        IsDeadText.TextValue = enemyDead.ChampionName + " 接受死亡吧";
                        IsDeadText.Draw();
                    }
                }
            }
            base.OnEndScene();
        }

        public override void OnDraw()
        {
            if (DrawingsMenu.CheckBox("Passive"))
            {
                foreach (
                    var enemy in
                        UnitManager.ValidEnemyHeroes.Where(
                            h => h.HealthPercent <= 50f && h.VisibleOnScreen && !h.TargetHaveBuff("zedpassivecd")))
                {
                    Drawing.DrawText(enemy.ServerPosition.WorldToScreen(), System.Drawing.Color.White, "主动技能就绪",
                        6);
                }
            }
            base.OnDraw();
        }

        protected override void PermaActive()
        {
            Range = Q.Range;
            if (WShadow != null && RShadow != null)
            {
                Range = (int)(Q.Range + Math.Max(MyHero.GetDistance(RShadow), MyHero.GetDistance(WShadow)));
            }
            else if (IsW1 && W.IsReady && RShadow != null)
            {
                Range = (int)(Q.Range + Math.Max(MyHero.GetDistance(RShadow), WRange));
            }
            else if (WShadow != null)
            {
                Range = (int)(Q.Range + MyHero.GetDistance(WShadow));
            }
            else if (IsW1 && W.IsReady)
            {
                Range = (Q.Range + WRange);
            }
            var currentList = UnitManager.ValidEnemyHeroesInRange.ToList();
            UnitManager.ValidEnemyHeroesInRange.Clear();
            UnitManager.ValidEnemyHeroesInRange.AddRange(currentList.Where(h => !IsDead(h)));
            Target = TargetSelector.GetTarget(UnitManager.ValidEnemyHeroesInRange, DamageType.Physical);
            var target = UnitManager.ValidEnemyHeroes.FirstOrDefault(h => TargetHaveR(h) && !IsDead(h));
            if (target != null)
            {
                Target = target;
            }
            if (IsDeadObject != null && (!IsDeadObject.IsValid || IsDeadObject.IsDead))
            {
                IsDeadObject = null;
            }
            if (ModeManager.Harass && HarassMenu.CheckBox("Collision"))
            {
                Q.AllowedCollisionCount = 0;
                W.AllowedCollisionCount = 0;
            }
            else
            {
                Q.AllowedCollisionCount = int.MaxValue;
                W.AllowedCollisionCount = int.MaxValue;
            }
            if (AutomaticMenu.CheckBox("E"))
            {
                UnitManager.ValidEnemyHeroesInRange.ForEach(CastE);
            }
            Swap();
            base.PermaActive();
        }

        protected void SwapByCountingEnemies()
        {
            var wCount = (WShadowIsValid && W.IsReady && !WShadow.IsUnderEnemyturret()) ? WShadow.CountEnemiesInRange(400) : 100;
            var rCount = (RShadowIsValid && R.IsReady && !RShadow.IsUnderEnemyturret()) ? RShadow.CountEnemiesInRange(400) : 100;
            var min = Math.Min(rCount, wCount);
            if (MyHero.CountEnemiesInRange(400) > min)
            {
                if (min == wCount)
                {
                    W.Cast();
                }
                else if (min == rCount)
                {
                    R.Cast();
                }
            }
        }

        protected void Swap()
        {
            if (Target != null)
            {
                var distanceSqr = MyHero.GetDistanceSqr(Target);
                var health = Target.TotalShieldHealth();
                var result = GetBestCombo(Target);
                if (IsDeadObject != null && RTarget != null && (AutomaticMenu.CheckBox("SwapDead") || (ComboMenu.CheckBox("SwapDead") && ModeManager.Combo)))
                {
                    SwapByCountingEnemies();
                }
                if (ModeManager.Combo)
                {
                	  if (ComboMenu.Slider("SwapHP") >= MyHero.HealthPercent)
                    {
                        if (!result.IsKillable || MyHero.HealthPercent < Target.HealthPercent)
                        {
                            SwapByCountingEnemies();
                        }
                    }
                    else if (ComboMenu.CheckBox("SwapGapclose") && distanceSqr >= (E.Range * 1.3f).Pow())
                    {
                        var wShadowDistance = (WShadowIsValid && W.IsReady && !WShadow.IsUnderEnemyturret())
                            ? Target.GetDistanceSqr(WShadow)
                            : 16000000f;
                        var rShadowDistance = (RShadowIsValid && R.IsReady && !RShadow.IsUnderEnemyturret())
                            ? Target.GetDistanceSqr(RShadow)
                            : 16000000f;
                        var min = Math.Min(Math.Min(wShadowDistance, rShadowDistance), distanceSqr);
                        if (min <= 500.Pow() && min < distanceSqr)
                        {
                            if (Math.Abs(min - wShadowDistance) < float.Epsilon)
                            {
                                W.Cast();
                            }
                            else if (Math.Abs(min - rShadowDistance) < float.Epsilon)
                            {
                                R.Cast();
                            }
                        }
                    }
                }
                else if (ModeManager.Harass)
                {
                    if (HarassMenu.CheckBox("SwapGapclose") && W.IsReady && !IsW1 && WShadowIsValid &&
                        Target.HealthPercent <= 50f && GetPassiveDamage(Target, health) > 0f && result.IsKillable &&
                        distanceSqr > WShadow.GetDistanceSqr(Target) && WShadow.GetDistanceSqr(Target) <= E.RangeSqr && Target.HealthPercent <= MyHero.HealthPercent)
                    {
                        W.Cast();
                    }
                }
            }
        }

        protected override void Flee()
        {
            if (W.IsReady)
            {
                if (IsW1)
                {
                    MyHero.Spellbook.CastSpell(W.Slot, MousePos);
                }
                else
                {
                    W.Cast();
                }
            }
            if (E.IsReady)
            {
                foreach (var enemy in UnitManager.ValidEnemyHeroesInRange.Where(h => !E.InRange(h)))
                {
                    CastE(enemy);
                }
            }
            base.Flee();
        }

        protected override void KillSteal()
        {
            foreach (var enemy in UnitManager.ValidEnemyHeroesInRange.Where(h => h.HealthPercent <= 40f))
            {
                var result = GetBestCombo(enemy);
                if (KillStealMenu.CheckBox("Q") && (result.Q || Q.IsKillable(enemy)))
                {
                    CastQ(enemy);
                }
                if (KillStealMenu.CheckBox("W") && enemy.HealthPercent <= 20f && NeedsW(enemy) && (result.W || W.IsKillable(enemy)))
                {
                    W.Cast(enemy);
                }
                if (KillStealMenu.CheckBox("E") && (result.E || E.IsKillable(enemy)))
                {
                    E.Cast(enemy);
                }
            }
            base.KillSteal();
        }

        protected override void Combo()
        {
            if (Target != null)
            {
                if (ComboMenu.CheckBox("Items"))
                {
                    ItemManager.CastOffensiveItems(Target);
                }
                if (R.IsReady && IsR1 && !ComboMenu.CheckBox("BlackList." + Target.ChampionName) && !IsCombo2)
                {
                    if (ComboMenu.CheckBox("R"))
                    {
                        CastR(Target);
                    }
                    if (ComboMenu.CheckBox("Prevent"))
                    {
                        return;
                    }
                }
                if (ComboMenu.CheckBox("W") && NeedsW(Target))
                {
                    CastW(Target);
                }
                if (ComboMenu.CheckBox("E"))
                {
                    CastE(Target);
                }
                if (ComboMenu.CheckBox("Q"))
                {
                    CastQ(Target);
                }
            }
            base.Combo();
        }

        protected override void Harass()
        {
            if (Target != null)
            {
                if (IsHarass2)
                {
                    if (HarassMenu.CheckBox("Harass2.W"))
                    {
                        if (ShouldWaitMana)
                        {
                            return;
                        }
                        CastW(Target);
                    }
                    if (HarassMenu.CheckBox("Harass2.E"))
                    {
                        CastE(Target);
                    }
                    if (HarassMenu.CheckBox("Harass2.Q"))
                    {
                        CastQ(Target);
                    }
                }
                else
                {
                    if (MyHero.ManaPercent >= HarassMenu.Slider("Harass1.ManaPercent"))
                    {
                        if (HarassMenu.CheckBox("Harass1.W"))
                        {
                            CastW(Target);
                        }
                        if (HarassMenu.CheckBox("Harass1.E"))
                        {
                            CastE(Target);
                        }
                        if (HarassMenu.CheckBox("Harass1.Q"))
                        {
                            CastQ(Target);
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
                var minion = W.LaneClear(false, ClearMenu.Slider("LaneClear.W"));
                CastW(minion);
                CastQ(Q.LaneClear(false, ClearMenu.Slider("LaneClear.Q")));
                CastE(E.LaneClear(false, ClearMenu.Slider("LaneClear.E")) ?? minion);
            }
            base.LaneClear();
        }

        protected override void LastHit()
        {
            if (MyHero.ManaPercent >= ClearMenu.Slider("LastHit.ManaPercent"))
            {
                Q.LastHit((LastHitType)ClearMenu.Slider("LastHit.Q"));
                E.LastHit((LastHitType)ClearMenu.Slider("LastHit.E"));
            }
            base.LastHit();
        }

        protected override void JungleClear()
        {
            if (MyHero.ManaPercent >= ClearMenu.Slider("JungleClear.ManaPercent"))
            {
                Obj_AI_Minion minion = null;
                if (ClearMenu.CheckBox("JungleClear.W"))
                {
                    minion = W.JungleClear(false);
                    if (ShouldWaitMana)
                    {
                        return;
                    }
                    CastW(minion);
                }
                if (ClearMenu.CheckBox("JungleClear.E"))
                {
                    CastE(E.JungleClear(false) ?? minion);
                }
                if (ClearMenu.CheckBox("JungleClear.Q"))
                {
                    CastQ(Q.JungleClear(false));
                }
            }
            base.JungleClear();
        }

        public void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady && target != null && !IsWaitingShadow)
            {
                var heroDistance = MyHero.GetDistanceSqr(target);
                var wShadowDistance = WShadowIsValid ? target.GetDistanceSqr(WShadow) : 16000000f;
                var rShadowDistance = RShadowIsValid ? target.GetDistanceSqr(RShadow) : 16000000f;
                var min = Math.Min(Math.Min(rShadowDistance, wShadowDistance), heroDistance);
                if (Math.Abs(min - wShadowDistance) < float.Epsilon && heroDistance > QReducedSqr)
                {
                    Q.SourceObject = WShadow;
                }
                else if (Math.Abs(min - rShadowDistance) < float.Epsilon && heroDistance > QReducedSqr)
                {
                    Q.SourceObject = RShadow;
                }
                else
                {
                    Q.SourceObject = MyHero;
                }
                Q.RangeCheckSourceObject = Q.Source;
                var qRange = Q.Range;
                if (W.LastCastTime > 0 && !WShadowIsValid && Core.GameTickCount - W.LastCastTime <= W.GetArrivalTime(W.LastEndPosition))
                {
                    Q.Range = Q.Range + (int)W.LastEndPosition.Distance(Q.Source);
                }
                Q.Cast(target);
                Q.Range = qRange;
            }
        }

        private void CastW(Obj_AI_Base target)
        {
            if (W.IsReady && IsW1 && target != null)
            {
                W.LastCastTime = Core.GameTickCount;
                var r = Q.GetPrediction(target, new CustomSettings { Range = Q.Range + W.Range});
                if (r.HitChancePercent >= Q.HitChancePercent / 2 && Core.GameTickCount - W.LastSentTime > 175)
                {
                    Vector3 wPos;
                    if (RShadow != null)
                    {
                        switch (ComboMenu.Slider("Mode"))
                        {
                            case 1:
                                wPos = MyHero.ServerPosition +
                                       ((r.CastPosition - RShadow.ServerPosition).Normalized() * WRange)
                                           .To2D()
                                           .Perpendicular()
                                           .To3DWorld();
                                break;
                            case 2:
                                wPos = MousePos;
                                break;
                            default:
                                wPos = MyHero.ServerPosition +
                                       (r.CastPosition - RShadow.ServerPosition).Normalized() * WRange;
                                break;
                        }
                    }
                    else
                    {
                        wPos = MyHero.ServerPosition + (r.CastPosition - MyHero.ServerPosition).Normalized() * WRange;
                    }
                    if (MyHero.Spellbook.CastSpell(SpellSlot.W, wPos))
                    {
                        W.LastSentTime = Core.GameTickCount;
                    }
                }
            }
        }

        public void CastE(Obj_AI_Base target)
        {
            if (E.IsReady && target != null && !IsWaitingShadow)
            {
                var heroDistance = MyHero.GetDistanceSqr(target);
                var wShadowDistance = WShadow != null ? target.GetDistanceSqr(WShadow) : 16000000f;
                var rShadowDistance = RShadow != null ? target.GetDistanceSqr(RShadow) : 16000000f;
                var min = Math.Min(Math.Min(rShadowDistance, wShadowDistance), heroDistance);
                if (Math.Abs(min - wShadowDistance) < float.Epsilon)
                {
                    if (WShadow != null) E.SourceObject = WShadow;
                }
                else if (Math.Abs(min - rShadowDistance) < float.Epsilon)
                {
                    if (RShadow != null) E.SourceObject = RShadow;
                }
                else
                {
                    E.SourceObject = MyHero;
                }
                E.RangeCheckSourceObject = E.Source;
                E.Cast(target);
            }
        }

        public void CastR(AIHeroClient target)
        {
            if (R.IsReady && IsR1 && target != null)
            {
                R.Cast(target);
            }
        }

        public bool ShouldWaitMana
        {
            get
            {
                return W.IsReady && IsW1 && MyHero.Mana < W.Mana + Q.Mana;
            }
        }

        public bool NeedsW(Obj_AI_Base target)
        {
            if (MyHero.GetDistanceSqr(target) <= WRange.Pow() &&
                (MyHero.Mana <
                 W.Mana + Q.Mana ||
                 MyHero.Mana <
                 W.Mana + E.Mana))
            {
                return false;
            }
            return true;
        }

        public bool IsDead(AIHeroClient target)
        {
            return IsDeadObject != null && TargetHaveR(target) && IsDeadObject.IsInRange(target, 200);
        }

        private float GetPassiveDamage(Obj_AI_Base target, float health = 0)
        {
            if (Math.Abs(health) < float.Epsilon)
            {
                health = target.Health;
            }
            if (100 * health / target.MaxHealth <= 50f)
            {
                if (target.TargetHaveBuff("zedpassivecd"))
                {
                    return 0f;
                }
                return MyHero.CalculateDamageOnUnit(target, DamageType.Magical,
                    target.MaxHealth * ((int)Math.Truncate((MyHero.Level - 1f) / 6f) * 2f + 6f) / 100f);
            }
            return 0;
        }

        public bool TargetHaveR(AIHeroClient target)
        {
            return target.TargetHaveBuff("zedrtargetmark");
        }

        protected override float GetComboDamage(Obj_AI_Base target, IEnumerable<SpellBase> list)
        {
            var damage = list.Sum(spell => spell.GetDamage(target));
            if (RShadowIsValid)
            {
                var hero = target as AIHeroClient;
                if (hero != null && TargetHaveR(hero))
                {
                    damage += (damage - Ignite.GetDamage(target) - Smite.GetDamage(target)) * (20f + R.Level * 10) / 100f;
                }
            }
            else
            {
                damage += damage * (20f + R.Level * 10) / 100f;
            }
            damage += MyHero.GetAttackDamage(target);
            damage += GetPassiveDamage(target, target.TotalShieldHealth() - damage);
            return damage;
        }

        public override float GetSpellDamage(SpellSlot slot, Obj_AI_Base target)
        {
            if (target != null)
            {
                var level = slot.GetSpellDataInst().Level;
                var result = 0f;
                switch (slot)
                {
                    case SpellSlot.Q:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Physical,
                            40f * level + 35f + 1f * MyHero.FlatPhysicalDamageMod);
                    case SpellSlot.W:
                        if (WShadowIsValid || (IsW1 && W.IsReady))
                        {
                            if (Q.IsReady)
                            {
                                result += GetSpellDamage(SpellSlot.Q, target) / 2f;
                            }
                        }
                        return result;
                    case SpellSlot.E:
                        return MyHero.CalculateDamageOnUnit(target, DamageType.Physical,
                            30f * level + 30f + 0.8f * MyHero.FlatPhysicalDamageMod);
                    case SpellSlot.R:
                        var hero = target as AIHeroClient;
                        if (hero == null)
                        {
                            return 0f;
                        }
                        if (RShadowIsValid || (IsR1 && R.IsReady))
                        {
                            if (Q.IsReady)
                            {
                                result += GetSpellDamage(SpellSlot.Q, target) / 2f;
                            }
                        }
                        if (RShadowIsValid)
                        {
                            if (TargetHaveR(hero))
                            {
                                return result;
                            }
                            return 0f;
                        }
                        return result +
                               MyHero.CalculateDamageOnUnit(target, DamageType.Physical, 1f * MyHero.TotalAttackDamage);
                }
            }
            return base.GetSpellDamage(slot, target);
        }
    }
}