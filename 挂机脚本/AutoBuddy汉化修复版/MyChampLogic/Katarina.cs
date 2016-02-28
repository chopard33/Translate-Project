using System.Linq;
using AutoBuddy.MainLogics;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace AutoBuddy.MyChampLogic
{
    internal class Katarina : IChampLogic
    {
        public float MaxDistanceForAA { get { return int.MaxValue; } }
        public float OptimalMaxComboDistance { get { return AutoWalker.p.AttackRange; } }
        public float HarassDistance { get { return AutoWalker.p.AttackRange; } }

        public static Spell.Targeted Q;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Active R;

        private static readonly Item Zhonya = new Item((int)ItemId.Zhonyas_Hourglass);

        public Katarina()
        {
            //                     1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18
            skillSequence = new[] { 1, 3, 2, 1, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3 };
            // ShopSequence =
            //   "3340:Buy,1001:Buy,2003:StartHpPot,1052:Buy,3113:Buy,3020:Buy,1058:Buy,3285:Buy,1052:Buy,2003:StopHpPot,1026:Buy,1058:Buy,3089:Buy,1052:Buy,3191:Buy,1058:Buy,3157:Buy,1026:Buy,3135:Buy,3094:Buy,1018:Buy,1038:Buy,3031:Buy,1037:Buy,3035:Buy,3033:Buy";

            ShopSequence = "3340:Buy,1001:Buy,2003:StartHpPot,2003:StartHpPot,2003:StartHpPot,1052:Buy,3113:Buy,3020:Buy,1058:Buy,3285:Buy,1052:Buy,2003:StopHpPot,3089:Buy,3157:Buy,3135:Buy,3001:Buy";

            Q = new Spell.Targeted(SpellSlot.Q, 675);
            W = new Spell.Active(SpellSlot.W, 375);
            E = new Spell.Targeted(SpellSlot.E, 700);
            R = new Spell.Active(SpellSlot.R, 550);

            Game.OnTick += Game_OnTick;
        }

        /* Made By: MarioGK */
        public int[] skillSequence { get; private set; }
        public LogicSelector Logic { get; set; }
        public string ShopSequence { get; private set; }

        public void Harass(AIHeroClient target)
        {
            if (Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
        }

        public void Survi()
        {
            if (Zhonya.IsReady() && AutoWalker.p.HealthPercent <= 15 && !AutoWalker.p.IsRecalling())
            {
                Zhonya.Cast();
            }
        }

        public void Combo(AIHeroClient target)
        {
            if (target.IsZombie)
                return;

            if (Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }

            //if (E.IsReady() && target.HealthPercent < Player.Instance.HealthPercent - 10)
            if (E.IsReady() && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }

            if (W.IsReady() && target.IsValidTarget(W.Range))
            {
                W.Cast();
            }

            if (R.IsReady() && AutoWalker.p.Distance(target.Position) <= R.Range && target != null &&
                target.IsVisible && !target.IsDead && !Q.IsReady() && !W.IsReady() && !E.IsReady())
            {
                Orbwalker.DisableMovement = true;
                Orbwalker.DisableAttacking = true;
                R.Cast();
            }
        }

        private void Game_OnTick(System.EventArgs args)
        {
            if (!AutoWalker.p.HasBuff("katarinarsound") || !AutoWalker.p.Spellbook.IsChanneling)
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;
            }
            else
            {
                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
            }


            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                var laneMinion =
                    EntityManager.MinionsAndMonsters.GetLaneMinions()
                        .OrderByDescending(m => m.Health)
                        .FirstOrDefault(
                            m => m.IsValidTarget(Q.Range) && m.Health <= Player.Instance.GetSpellDamage(m, SpellSlot.Q));
                if (laneMinion == null) return;

                if (Q.IsReady())
                {
                    Q.Cast(laneMinion);
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                var lastMinion =
                    EntityManager.MinionsAndMonsters.GetLaneMinions()
                        .OrderByDescending(m => m.Health)
                        .FirstOrDefault(
                            m =>
                                m.IsValidTarget(Q.Range) &&
                                !m.IsInRange(Player.Instance, Player.Instance.GetAutoAttackRange()) &&
                                m.Health <= Player.Instance.GetSpellDamage(m, SpellSlot.Q));
                if (lastMinion == null) return;

                if (Q.IsReady())
                {
                    Q.Cast(lastMinion);
                }

                if (W.IsReady() && lastMinion.IsValidTarget(W.Range))
                {
                    W.Cast();
                }
            }
        }
    }
}