﻿using EloBuddy;
using EloBuddy.SDK.Menu;
using KoreanAIO.Utilities;

namespace KoreanAIO.Managers
{
    public static class DrawingsManager
    {
        public static Menu Menu {
            get { return MenuManager.GetSubMenu("线圈"); }
        }

        public static void Initialize()
        {
            Drawing.OnDraw += delegate
            {
                if (AIO.MyHero.IsDead || Menu.CheckBox("Disable"))
                {
                    return;
                }
                AIO.CurrentChampion.OnDraw();
                CircleManager.Draw();
                ToggleManager.Draw();
            };
            Drawing.OnEndScene += delegate
            {
                if (AIO.MyHero.IsDead || Menu.CheckBox("Disable"))
                {
                    return;
                }
                AIO.CurrentChampion.OnEndScene();
                if (Menu.CheckBox("DamageIndicator"))
                {
                    DamageIndicator.Draw();
                }
            };
        }

    }
}
