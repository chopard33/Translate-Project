using System.Linq;
using AutoBuddy.MainLogics;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace AutoBuddy.MyChampLogic
{
    internal class Sivir : IChampLogic
    {
        public float MaxDistanceForAA { get { return int.MaxValue; } }
        public float OptimalMaxComboDistance { get { return AutoWalker.p.AttackRange; } }
        public float HarassDistance { get { return AutoWalker.p.AttackRange; } }


        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        public Sivir()
        {
            //                     1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18
            skillSequence = new[] {1, 2, 3, 1, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3};
            ShopSequence =
                "3340:Buy,1036:Buy,2003:StartHpPot,1053:Buy,1042:Buy,1001:Buy,3006:Buy,1036:Buy,1038:Buy,3072:Buy,2003:StopHpPot,1042:Buy,1051:Buy,3086:Buy,1042:Buy,1042:Buy,1043:Buy,3085:Buy,2015:Buy,3086:Buy,3094:Buy,1018:Buy,1038:Buy,3031:Buy,1037:Buy,3035:Buy,3033:Buy";

            Q = new Spell.Skillshot(SpellSlot.Q, 1250, SkillShotType.Linear, 0, 1350, 150);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Active(SpellSlot.R);

            Game.OnTick += Game_OnTick;
        }

        public int[] skillSequence { get; private set; }
        public LogicSelector Logic { get; set; }

        public string ShopSequence { get; private set; }

        public void Harass(AIHeroClient target)
        {
        }

        public void Survi()
        {
            /*
            if (R.IsReady() || W.IsReady())
            {
                AIHeroClient chaser =
                    EntityManager.Heroes.Enemies.FirstOrDefault(
                        chase => chase.Distance(AutoWalker.p) < 600 && chase.IsVisible());
                if (chaser != null)
                {
                    if (R.IsReady())
                        R.Cast(chaser);

                    if (W.IsReady())
                        W.Cast(chaser);

                    if (Q.IsReady() && Q.IsLearned)
                        Q.Cast();
                }
            }*/
        }

        public void Combo(AIHeroClient target)
        {
            if (target != null)
            {
                if (target != null && Q.GetPrediction(target).HitChance >= HitChance.Medium && target.Distance(AutoWalker.p) > 550)
                {
                    int count = 0;
                    foreach (var e in Q.GetPrediction(target).CollisionObjects)
                    {
                        if (e.NetworkId != target.NetworkId)
                        {
                            count++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (target.Health < AutoWalker.p.GetSpellDamage(target, SpellSlot.Q))
                    {
                        Q.Cast(Q.GetPrediction(target).CastPosition);
                    }
                }

                if (target != null && W.IsReady())
                {
                    W.Cast();
                }
            }
        }

        private void Game_OnTick(System.EventArgs args)
        {


        }
    }
}