using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using OneForWeek.Draw;
using OneForWeek.Util;
using OneForWeek.Util.MenuSettings;
using OneForWeek.Util.Misc;
using SharpDX;

namespace OneForWeek.Plugin.Hero
{
    class Cassiopeia : PluginModel, IChampion
    {
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Skillshot R;

        private static int skinId = 1;

        private float _lastECast = 0f;

        private float lastQCast = 0f;

        public void Init()
        {
            InitVariables();
            DamageIndicator.Initialize(Spells.GetComboDamage);
        }

        public void InitVariables()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Circular, castDelay: 400, spellWidth: 75);
            W = new Spell.Skillshot(SpellSlot.W, 850, SkillShotType.Circular, spellWidth: 125);
            E = new Spell.Targeted(SpellSlot.E, 700);
            R = new Spell.Skillshot(SpellSlot.R, 825, SkillShotType.Cone, spellWidth: 80);
            InitMenu();

            Orbwalker.OnPostAttack += OnAfterAttack;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        public void InitMenu()
        {
            Menu = MainMenu.AddMenu(GCharname, GCharname);

            Menu.AddLabel("Version: " + GVersion);
            Menu.AddSeparator();
            Menu.AddLabel("By MrArticuno 由CH汉化");

            DrawMenu = Menu.AddSubMenu("线圈显示 - " + GCharname, GCharname + "线圈显示");
            DrawMenu.AddGroupLabel("线圈显示");
            DrawMenu.Add("只显示无冷却技能", new CheckBox("只显示无冷却技能", false));
            DrawMenu.Add("关闭线圈", new CheckBox("关闭线圈"));
            DrawMenu.AddSeparator();
            //Q
            DrawMenu.Add("显示Q", new CheckBox("显示Q"));
            DrawMenu.AddColorItem("colorQ");
            DrawMenu.AddWidthItem("widthQ");
            //W
            DrawMenu.Add("显示W", new CheckBox("显示W"));
            DrawMenu.AddColorItem("colorW");
            DrawMenu.AddWidthItem("widthW");
            //E
            DrawMenu.Add("显示E", new CheckBox("显示E"));
            DrawMenu.AddColorItem("colorE");
            DrawMenu.AddWidthItem("widthE");
            //R
            DrawMenu.Add("显示R", new CheckBox("显示R"));
            DrawMenu.AddColorItem("colorR");
            DrawMenu.AddWidthItem("widthR");

            ComboMenu = Menu.AddSubMenu("连招 - " + GCharname, GCharname + "连招");
            ComboMenu.AddGroupLabel("连招");
            ComboMenu.Add("连招Q", new CheckBox("连招Q", true));
            ComboMenu.Add("连招W", new CheckBox("连招W", true));
            ComboMenu.Add("连招E", new CheckBox("连招E", true));
            ComboMenu.Add("连招R", new CheckBox("连招R", true));
            ComboMenu.AddGroupLabel("连招杂项");
            ComboMenu.Add("使用W如果Q没中", new CheckBox("使用W如果Q没中", true));
            ComboMenu.Add("连招屏蔽普攻", new CheckBox("连招时不进行普攻", false));
            ComboMenu.AddLabel("此选项会无视最少敌人数量使用才R的设置");
            ComboMenu.Add("闪现R", new CheckBox("闪现R连招如果敌人可以被击杀", false));
            ComboMenu.Add("至少敌人数量面对使用R", new Slider("至少敌人数量面对使用R: ", 2, 0, 5));

            HarassMenu = Menu.AddSubMenu("骚扰 - " + GCharname, GCharname + "骚扰");
            HarassMenu.AddGroupLabel("骚扰");
            HarassMenu.Add("骚扰Q", new CheckBox("骚扰Q", true));
            HarassMenu.Add("骚扰W", new CheckBox("骚扰W", true));
            HarassMenu.Add("骚扰E", new CheckBox("骚扰E", true));
            HarassMenu.AddGroupLabel("骚扰杂项");
            HarassMenu.Add("骚扰屏蔽普攻", new CheckBox("连招时不进行普攻", false));
            HarassMenu.Add("骚扰E设置", new CheckBox("中毒才使用E", true));

            LaneClearMenu = Menu.AddSubMenu("清线 - " + GCharname, GCharname + "清线");
            LaneClearMenu.AddGroupLabel("清线");
            LaneClearMenu.Add("清线Q", new CheckBox("清线Q", true));
            LaneClearMenu.Add("清线W", new CheckBox("清线W", true));
            LaneClearMenu.Add("清线E", new CheckBox("清线E", true));
            LaneClearMenu.Add("清线E设置1", new CheckBox("可击杀才使用E", false));
            LaneClearMenu.Add("清线E设置2", new CheckBox("中毒才使用E", true));

            LastHitMenu = Menu.AddSubMenu("尾兵 - " + GCharname, GCharname + "尾兵");
            LastHitMenu.AddGroupLabel("尾兵");
            LastHitMenu.Add("尾兵Q", new CheckBox("尾兵Q", true));
            LastHitMenu.Add("尾兵W", new CheckBox("尾兵W", true));
            LastHitMenu.Add("尾兵E", new CheckBox("尾兵E", true));

            JungleClearMenu = Menu.AddSubMenu("清野 - " + GCharname, GCharname + "清野");
            JungleClearMenu.AddGroupLabel("清野");
            JungleClearMenu.Add("清野Q", new CheckBox("清野Q", true));
            JungleClearMenu.Add("清野W", new CheckBox("清野W", true));
            JungleClearMenu.Add("清野E", new CheckBox("清野E", true));
            JungleClearMenu.Add("清野E设置1", new CheckBox("可击杀才使用E", false));
            JungleClearMenu.Add("清野E设置2", new CheckBox("中毒才使用E", true));


            MiscMenu = Menu.AddSubMenu("杂项 - " + GCharname, GCharname + "杂项");
            MiscMenu.Add("换肤", new Slider("换肤: ", 1, 1, 5));
            MiscMenu.Add("E设置", new CheckBox("只对中毒的目标使用E", true));
            MiscMenu.Add("E延迟", new Slider("延迟E: ", 150, 0, 500));
            MiscMenu.Add("抢人头", new CheckBox("尝试抢人头", true));
            MiscMenu.Add("使用W间距", new CheckBox("使用W造成间距", true));
            MiscMenu.Add("使用R间距", new CheckBox("使用R造成间距", true));
            MiscMenu.Add("防空大（R)", new CheckBox("屏蔽R如果空大", true));
            MiscMenu.Add("低血量间距设置", new Slider("最低血量进行R造成间距: ", 40, 0, 100));
            MiscMenu.Add("技能打断", new CheckBox("打断威胁的技能", true));

        }

        public void OnCombo()
        {
            var target = TargetSelector.GetTarget(R.Range + 400, DamageType.Magical);

            if (target == null || !target.IsValidTarget(Q.Range)) return;

            var flash = Player.Spells.FirstOrDefault(a => a.SData.Name == "summonerflash");

            if (Misc.IsChecked(ComboMenu, "连招Q") && Q.IsReady() && target.IsValidTarget(Q.Range) && !IsPoisoned(target))
            {
                var predictionQ = Q.GetPrediction(target);

                if (predictionQ.HitChancePercent >= 80)
                {
                    Q.Cast(predictionQ.CastPosition);
                    lastQCast = Game.Time;
                }
            }

            if (Misc.IsChecked(ComboMenu, "连招W") && W.IsReady() && target.IsValidTarget(W.Range))
            {
                if (Misc.IsChecked(ComboMenu, "使用W如果Q没中"))
                {
                    if ((!IsPoisoned(target) && !Q.IsReady()) &&
                        (lastQCast - Game.Time) < -0.43f)
                    {
                        var predictionW = W.GetPrediction(target);

                        if (predictionW.HitChancePercent >= 70)
                        {
                            W.Cast(predictionW.CastPosition);
                        }
                    }
                }
                else
                {
                    var predictionW = W.GetPrediction(target);

                    if (predictionW.HitChancePercent >= 70)
                    {
                        W.Cast(predictionW.CastPosition);
                    }
                }
            }

            if (Misc.IsChecked(ComboMenu, "连招E") && E.IsReady() && target.IsValidTarget(E.Range) && (IsPoisoned(target) || !Misc.IsChecked(MiscMenu, "E设置")) && canCastE())
            {
                E.Cast(target);
            }

            if (Misc.IsChecked(ComboMenu, "连招R") && R.IsReady())
            {
                if (Misc.IsChecked(ComboMenu, "闪现R") && PossibleDamage(target) > target.Health && target.IsFacing(_Player) && target.Distance(_Player) > R.Range && (flash != null && flash.IsReady))
                {
                    Player.CastSpell(flash.Slot, target.Position);
                    Core.DelayAction(() => R.Cast(target), 250);
                }

                var countFacing = EntityManager.Heroes.Enemies.Count(t => t.IsValidTarget(R.Range) && t.IsFacing(_Player) && ProbablyFacing(t));

                if (Misc.GetSliderValue(ComboMenu, "至少敌人数量面对使用R") <= countFacing && target.IsFacing(_Player) && target.IsValidTarget(R.Range - 50))
                {
                    R.Cast(target);
                }
            }
        }

        private static bool ProbablyFacing(Obj_AI_Base target)
        {
            var predictPos = Prediction.Position.PredictUnitPosition(target, 250);

            return predictPos.Distance(Player.Instance.ServerPosition)  < target.ServerPosition.Distance(Player.Instance.ServerPosition);
        }

        public void OnHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null || !target.IsValidTarget(Q.Range)) return;

            if (Misc.IsChecked(HarassMenu, "骚扰Q") && Q.IsReady() && target.IsValidTarget(Q.Range) && !IsPoisoned(target))
            {
                var predictionQ = Q.GetPrediction(target);

                if (predictionQ.HitChancePercent >= 80)
                {
                    Q.Cast(predictionQ.CastPosition);
                    lastQCast = Game.Time;
                }
            }

            if (Misc.IsChecked(HarassMenu, "骚扰W") && W.IsReady() && target.IsValidTarget(W.Range))
            {
                if((!IsPoisoned(target) && !Q.IsReady()) &&
                        (lastQCast - Game.Time) < -0.43f)
                    {
                    var predictionW = W.GetPrediction(target);

                    if (predictionW.HitChancePercent >= 70)
                    {
                        W.Cast(predictionW.CastPosition);
                    }
                }
            }

            if (Misc.IsChecked(HarassMenu, "骚扰E") && E.IsReady() && target.IsValidTarget(E.Range) && (IsPoisoned(target)) && canCastE())
            {
                E.Cast(target);
            }
        }

        public void OnLaneClear()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions;

            if (minions == null || !minions.Any()) return;

            var bestFarmQ =
                Misc.GetBestCircularFarmLocation(
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(_Player) <= Q.Range)
                        .Select(xm => xm.ServerPosition.To2D())
                        .ToList(), Q.Width, Q.Range);
            var bestFarmW =
                Misc.GetBestCircularFarmLocation(
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(_Player) <= W.Range)
                        .Select(xm => xm.ServerPosition.To2D())
                        .ToList(), W.Width, W.Range);

            if (Misc.IsChecked(LaneClearMenu, "清线Q") && Q.IsReady() && bestFarmQ.MinionsHit > 0)
            {
                Q.Cast(bestFarmQ.Position.To3D());
            }

            if (Misc.IsChecked(LaneClearMenu, "清线W") && W.IsReady() && bestFarmW.MinionsHit > 0)
            {
                W.Cast(bestFarmW.Position.To3D());
            }

            if (Misc.IsChecked(LaneClearMenu, "清线E") && E.IsReady())
            {
                if (Misc.IsChecked(LaneClearMenu, "清线E设置1"))
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(
                            t =>
                                t.IsValidTarget(E.Range) && _Player.GetSpellDamage(t, SpellSlot.E) > t.Health &&
                                (!Misc.IsChecked(LaneClearMenu, "清线E设置2") || IsPoisoned(t)));

                    if (minion != null)
                        E.Cast(minion);
                }
                else
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(
                            t =>
                                t.IsValidTarget(E.Range) &&
                                (Misc.IsChecked(LaneClearMenu, "清线E设置2") || IsPoisoned(t)));

                    if (minion != null)
                        E.Cast(minion);
                }
            }

        }

        public void OnJungleClear()
        {
            var minions = EntityManager.MinionsAndMonsters.Monsters;

            if (minions == null || !minions.Any(m => m.IsValidTarget(900))) return;

            var bestFarmQ =
                Misc.GetBestCircularFarmLocation(
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(_Player) <= Q.Range)
                        .Select(xm => xm.ServerPosition.To2D())
                        .ToList(), Q.Width, Q.Range);
            var bestFarmW =
                Misc.GetBestCircularFarmLocation(
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(_Player) <= W.Range)
                        .Select(xm => xm.ServerPosition.To2D())
                        .ToList(), W.Width, W.Range);

            if (Misc.IsChecked(JungleClearMenu, "清野Q") && Q.IsReady() && bestFarmQ.MinionsHit > 0)
            {
                Q.Cast(bestFarmQ.Position.To3D());
            }

            if (Misc.IsChecked(JungleClearMenu, "清野W") && W.IsReady() && bestFarmW.MinionsHit > 0)
            {
                W.Cast(bestFarmW.Position.To3D());
            }

            if (Misc.IsChecked(JungleClearMenu, "清野E") && E.IsReady())
            {
                if (Misc.IsChecked(JungleClearMenu, "清野E设置1"))
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.EnemyMinions.First(
                            t =>
                                t.IsValidTarget(E.Range) && _Player.GetSpellDamage(t, SpellSlot.E) > t.Health &&
                                (!Misc.IsChecked(JungleClearMenu, "清野E设置2") || IsPoisoned(t)));

                    if (minion != null)
                        E.Cast(minion);
                }
                else
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.EnemyMinions.First(
                            t =>
                                t.IsValidTarget(E.Range) &&
                                (Misc.IsChecked(JungleClearMenu, "清野E设置2") || IsPoisoned(t)));

                    if (minion != null)
                        E.Cast(minion);
                }
            }

        }

        public void OnFlee()
        {

        }

        public void OnGameUpdate(EventArgs args)
        {
            if (skinId != Misc.GetSliderValue(MiscMenu, "换肤"))
            {
                skinId = Misc.GetSliderValue(MiscMenu, "换肤");
                Player.SetSkinId(skinId);
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    if (Misc.IsChecked(ComboMenu, "连招屏蔽普攻"))
                        Orbwalker.DisableAttacking = true;

                    OnCombo();
                    break;
                case Orbwalker.ActiveModes.Flee:
                    OnFlee();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    if (Misc.IsChecked(HarassMenu, "骚扰屏蔽普攻"))
                        Orbwalker.DisableAttacking = true;
                    OnHarass();
                    break;
            }

            if (Orbwalker.DisableAttacking && (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Combo && Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Harass))
                Orbwalker.DisableAttacking = false;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                OnLaneClear();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                OnJungleClear();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                OnLastHit();

            if (Misc.IsChecked(MiscMenu, "抢人头"))
                KS();
        }

        private void OnLastHit()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => m.IsValidTarget(E.Range) && Player.Instance.GetSpellDamage(m, SpellSlot.E) > m.Health);

            if (minions == null || !minions.Any() || !minions.Any(m => m.IsValidTarget(E.Range))) return;

            var target = minions.FirstOrDefault();

            if (Misc.IsChecked(LastHitMenu, "尾兵Q") && Q.IsReady() && Q.IsInRange(target) && !IsPoisoned(target))
            {
                Q.Cast(target.ServerPosition);
                lastQCast = Game.Time;
            }

            if (Misc.IsChecked(LastHitMenu, "尾兵W") && W.IsReady() && W.IsInRange(target) && !IsPoisoned(target))
            {
                if (Misc.IsChecked(ComboMenu, "使用W如果Q没中"))
                {
                    if ((!IsPoisoned(target) && !Q.IsReady()) &&
                        (lastQCast - Game.Time) < -0.43f)
                    {
                        var predictionW = W.GetPrediction(target);

                        if (predictionW.HitChancePercent >= 70)
                        {
                            W.Cast(predictionW.CastPosition);
                        }
                    }
                }
                else
                {
                    var predictionW = W.GetPrediction(target);

                    if (predictionW.HitChancePercent >= 70)
                    {
                        W.Cast(predictionW.CastPosition);
                    }
                }
            }

            if (Misc.IsChecked(LastHitMenu, "尾兵E") && E.IsReady() && E.IsInRange(target) && IsPoisoned(target))
            {
                E.Cast(target);
            }
        }

        public void OnDraw(EventArgs args)
        {
            if (Misc.IsChecked(DrawMenu, "关闭线圈")) return;

            if (Misc.IsChecked(DrawMenu, "只显示无冷却技能") ? Q.IsReady() : Misc.IsChecked(DrawMenu, "显示Q"))
            {
                new Circle { Color = DrawMenu.GetColor("colorQ"), BorderWidth = DrawMenu.GetWidth("widthQ"), Radius = Q.Range }.Draw(Player.Instance.Position);
            }

            if (Misc.IsChecked(DrawMenu, "只显示无冷却技能") ? W.IsReady() : Misc.IsChecked(DrawMenu, "显示W"))
            {
                new Circle { Color = DrawMenu.GetColor("colorW"), BorderWidth = DrawMenu.GetWidth("widthW"), Radius = W.Range }.Draw(Player.Instance.Position);
            }

            if (Misc.IsChecked(DrawMenu, "只显示无冷却技能") ? E.IsReady() : Misc.IsChecked(DrawMenu, "显示E"))
            {
                new Circle { Color = DrawMenu.GetColor("colorE"), BorderWidth = DrawMenu.GetWidth("widthE"), Radius = E.Range }.Draw(Player.Instance.Position);
            }

            if (Misc.IsChecked(DrawMenu, "只显示无冷却技能") ? R.IsReady() : Misc.IsChecked(DrawMenu, "显示R"))
            {
                new Circle { Color = DrawMenu.GetColor("colorR"), BorderWidth = DrawMenu.GetWidth("widthR"), Radius = R.Range }.Draw(Player.Instance.Position);
            }
        }

        public void OnAfterAttack(AttackableUnit target, EventArgs args)
        {

        }

        public void OnPossibleToInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (!sender.IsEnemy) return;

            if (Misc.IsChecked(MiscMenu, "技能打断") && interruptableSpellEventArgs.DangerLevel >= DangerLevel.High && R.IsReady() && R.IsInRange(sender))
            {
                R.Cast(sender);
            }
        }

        public void OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if(!sender.IsEnemy) return;

            if ((e.End.Distance(_Player) < 50 || e.Sender.IsAttackingPlayer) && Misc.IsChecked(MiscMenu, "使用R间距") &&
                _Player.HealthPercent < Misc.GetSliderValue(MiscMenu, "低血量间距设置") && R.IsReady() && R.IsInRange(sender))
            {
                R.Cast(sender);
            }else if ((e.End.Distance(_Player) < 50 || e.Sender.IsAttackingPlayer) && Misc.IsChecked(MiscMenu, "使用W间距") && W.IsReady() && W.IsInRange(sender))
            {
                W.Cast(e.End);
            }
        }

        public void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if(!sender.IsMe) return;

            if (args.SData.Name == "CassiopeiaTwinFang")
            {
                _lastECast = Game.Time;
            }

            if (args.SData.Name == "CassiopeiaPetrofyingGaze" && Misc.IsChecked(MiscMenu, "防空大（R)"))
            {
                if (EntityManager.Heroes.Enemies.Count(t => t.IsValidTarget(R.Range) && t.IsFacing(_Player)) < 1)
                {
                    args.Process = false;
                }
            }

        }

        public void GameObjectOnCreate(GameObject sender, EventArgs args)
        {

        }

        public void GameObjectOnDelete(GameObject sender, EventArgs args)
        {

        }

        private static void KS()
        {

            if (E.IsReady() && EntityManager.Heroes.Enemies.Any(t => t.IsValidTarget(E.Range) && t.Health < _Player.GetSpellDamage(t, SpellSlot.E)))
            {
                E.Cast(EntityManager.Heroes.Enemies.FirstOrDefault(t => t.IsValidTarget(E.Range) && t.Health < _Player.GetSpellDamage(t, SpellSlot.E)));
            }

            if (Q.IsReady() && EntityManager.Heroes.Enemies.Any(t => t.IsValidTarget(Q.Range) && t.Health < _Player.GetSpellDamage(t, SpellSlot.Q)))
            {
                var predictionQ = Q.GetPrediction(EntityManager.Heroes.Enemies.FirstOrDefault(t => t.IsValidTarget(Q.Range) && t.Health < _Player.GetSpellDamage(t, SpellSlot.Q)));

                if (predictionQ.HitChancePercent >= 70)
                {
                    Q.Cast(predictionQ.CastPosition);
                }
            }
        }

        private static float PossibleDamage(Obj_AI_Base target)
        {
            var damage = 0f;
            if (R.IsReady())
                damage += _Player.GetSpellDamage(target, SpellSlot.R);
            if (E.IsReady())
                damage += _Player.GetSpellDamage(target, SpellSlot.E);
            if (W.IsReady())
                damage += _Player.GetSpellDamage(target, SpellSlot.W);
            if (Q.IsReady())
                damage += _Player.GetSpellDamage(target, SpellSlot.Q);

            return damage;
        }

        public bool canCastE()
        {
            return (_lastECast - Game.Time + Misc.GetSliderValue(MiscMenu, "E延迟") / 1000f) < 0;
        }

        public bool IsPoisoned(Obj_AI_Base target)
        {
            return target.HasBuffOfType(BuffType.Poison);
        }
    }
}
