namespace RNUITrainerSample
{
    using System.Windows.Forms;
    using Rage;
    using RAGENativeUI;
    using RAGENativeUI.PauseMenu;

    internal static class Plugin
    {
        public static MenuPool Pool { get; } = new MenuPool();
        private static UIMenu TrainerMenu { get; set; }

        public static void Main()
        {
            Game.Console.Print("- Press Shift+F5 to open the trainer menu");

            TrainerMenu = new Menu.TrainerMenu();

            Game.DisplayHelp($"Press ~{Keys.LShiftKey.GetInstructionalId()}~ ~+~ ~{Keys.F5.GetInstructionalId()}~ to open the trainer menu.");

            while (true)
            {
                GameFiber.Yield();

                if (Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Keys.F5) &&
                    !UIMenu.IsAnyMenuVisible && !TabView.IsAnyPauseMenuVisible)
                {
                    TrainerMenu.Visible = true;
                }

                // process input and draw the visible menus
                Pool.ProcessMenus();
            }
        }
    }
}
