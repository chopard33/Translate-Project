using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace KoreanAIO.Managers
{
    public static class MenuManager
    {
        public static Menu Menu;
        public static bool MenuCompleted;
        public static readonly Dictionary<string, Menu> SubMenus = new Dictionary<string, Menu>();
        public static readonly List<Action> PendingActions = new List<Action>();
        public static void Initialize()
        {
            Menu = MainMenu.AddMenu("韩国中单合集 CH汉化", "KoreanAIO Build: 6.1.0, Champion: " + AIO.MyHero.ChampionName + ".");
        }

        public static void AddDrawingsMenu()
        {
            AddSubMenu("线圈");
            GetSubMenu("线圈").Add("Disable", new CheckBox("关闭线圈", false));
            GetSubMenu("线圈").AddSeparator();
            GetSubMenu("线圈").Add("DamageIndicator", new CheckBox("敌方HP显示技能总伤害"));
            var target = GetSubMenu("线圈").Add("Target", new CheckBox("目标身上显示圈"));
            CircleManager.Circles.Add(new Circle(target, new ColorBGRA(255, 0, 0, 100), () => 120f, () => AIO.CurrentChampion.Target != null, () => AIO.CurrentChampion.Target) {Width = 5});
            GetSubMenu("线圈").AddSeparator();
        }

        public static void AddKillStealMenu()
        {
            AddSubMenu("抢人头");
            GetSubMenu("抢人头").Add("Ignite", new CheckBox("使用点燃抢头"));
            GetSubMenu("抢人头").Add("Smite", new CheckBox("使用惩戒抢头"));
            GetSubMenu("抢人头").AddSeparator();
        }
        public static void AddSubMenu(string name)
        {
            SubMenus[name] = Menu.AddSubMenu(name);
        }

        public static Menu GetSubMenu(string s)
        {
            return (from t in SubMenus where t.Key.Equals(s) select t.Value).FirstOrDefault();
        }


        public static void AddStringList(this Menu m, string uniqueId, string displayName, string[] values, int defaultValue = 0)
        {
            var mode = m.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
            {
                sender.DisplayName = displayName + ": " + values[args.NewValue];
            };
        }

        public static int Slider(this Menu m, string s)
        {
            if (m != null && AIO.Initialized)
            {
                return m[s].Cast<Slider>().CurrentValue;
            }
            return -1;
        }

        public static bool CheckBox(this Menu m, string s)
        {
            return m != null && AIO.CurrentChampion != null && AIO.Initialized && m[s].Cast<CheckBox>().CurrentValue;
        }

        public static bool KeyBind(this Menu m, string s)
        {
            return m != null && AIO.CurrentChampion != null && AIO.Initialized && m[s].Cast<KeyBind>().CurrentValue;
        }

    }
}
