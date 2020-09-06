namespace RNUITrainerSample.Menu
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using Rage;
    using Rage.Native;

    using RAGENativeUI;
    using RAGENativeUI.Elements;

    internal sealed class SpawnVehicleMenu : TrainerMenuBase
    {
        private readonly List<ClassMenu> classMenus = new List<ClassMenu>();
        private readonly SearchResultsMenu searchResultsMenu;

        public SpawnVehicleMenu() : base(SubMenuTitle("SPAWN VEHICLE"))
        {
            searchResultsMenu = new SearchResultsMenu();

            foreach (var vehClass in Model.VehicleModels.GroupBy(GetClass))
            {
                var menu = new ClassMenu(vehClass.Key, vehClass);

                UIMenuItem item = new UIMenuItem(menu.LocalizedClass);

                AddItem(item);
                BindMenuToItem(menu, item);
                classMenus.Add(menu);
            }

            InstructionalButtons.Buttons.Add(new InstructionalButton(InstructionalKey.Space, "Search"));
        }

        public override void ProcessControl()
        {
            base.ProcessControl();

            if (Game.IsKeyDown(Keys.Space))
            {
                var searchTerm = ShowSearchPrompt();
                if (searchTerm != null)
                {
                    DoSearch(searchTerm);
                }
            }
        }

        private void DoSearch(string searchTerm)
        {
            static bool Contains(string str, string term)
                => str.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

            searchResultsMenu.SetSearchResults(searchTerm,
                                               classMenus.SelectMany(c => c.Models)
                                                         .Where(m => Contains(m.ItemName, searchTerm) || Contains(m.ItemSecondaryName, searchTerm)));

            Visible = false;
            searchResultsMenu.Visible = true;
            searchResultsMenu.ParentMenu = this;
        }

        private static string ShowSearchPrompt()
        {
            Plugin.Pool.Draw();

            NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD(6, "", "", "", "", "", "", 32);
            int state;
            while ((state = NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>()) == 0)
            {
                GameFiber.Yield();
                // keep drawing the menus but don't process inputs, since we are blocking the menu processing fiber
                Plugin.Pool.Draw();
            }

            if (state == 1)
            {
                return NativeFunction.Natives.GET_ONSCREEN_KEYBOARD_RESULT<string>();
            }

            return null;
        }

        private static VehicleClass GetClass(Model model) => (VehicleClass)NativeFunction.Natives.xDEDF1C8BD47C2200<int>(model.Hash); // GET_VEHICLE_CLASS_FROM_NAME

        private static void SpawnVehicle(Model model) => GameFiber.StartNew(() =>
        {
            new Vehicle(model, Game.LocalPlayer.Character.GetOffsetPositionFront(5.0f)).Dismiss();
        });

        private readonly struct ModelEntry
        {
            public Model Model { get; }
            public string ItemName { get; }
            public string ItemSecondaryName { get; }

            public ModelEntry(Model model)
            {
                const uint CARNOTFOUND = 3779704296; // Game.GetHashKey("CARNOTFOUND")

                Model = model;
                List<string> nameComponents = new List<string>(2);

                ItemSecondaryName = $"({model.Name})";

                string makeNameLabel = NativeFunction.Natives.xF7AF4F159FF99F97<string>(model.Hash); // _GET_MAKE_NAME_FROM_VEHICLE_MODEL
                if (!string.IsNullOrEmpty(makeNameLabel) && Game.GetHashKey(makeNameLabel) != CARNOTFOUND)
                {
                    nameComponents.Add(Game.GetLocalizedString(makeNameLabel));
                }

                string displayNameLabel = NativeFunction.Natives.xB215AAC32D25D019<string>(model.Hash); // GET_DISPLAY_NAME_FROM_VEHICLE_MODEL
                if (!string.IsNullOrEmpty(displayNameLabel) && Game.GetHashKey(displayNameLabel) != CARNOTFOUND)
                {
                    nameComponents.Add(Game.GetLocalizedString(displayNameLabel));
                }

                if (nameComponents.Count == 0)
                {
                    nameComponents.Add(ItemSecondaryName);
                }

                ItemName = string.Join(" ", nameComponents);
            }
        }

        private sealed class ClassMenu : TrainerMenuBase
        {
            public VehicleClass Class { get; }
            public string LocalizedClass { get; }
            public ModelEntry[] Models { get; }

            public ClassMenu(VehicleClass vehicleClass, IEnumerable<Model> models) : base("")
            {
                Class = vehicleClass;
                string classLabel = $"VEH_CLASS_{(int)vehicleClass}";
                LocalizedClass = Game.GetLocalizedString(classLabel);

                SubtitleText = SubMenuTitle(LocalizedClass.ToUpperInvariant());

                Models = models.Select(m => new ModelEntry(m))
                               .OrderBy(m => m.ItemName)
                               .OrderBy(m => m.ItemSecondaryName)
                               .ToArray();

                AddItems(Models.Select(m => new UIMenuItem(m.ItemName) { RightLabel = m.ItemSecondaryName }));
                OnItemSelect += OnItemActivated;
            }

            private void OnItemActivated(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                SpawnVehicle(Models[index].Model);
            }
        }

        private sealed class SearchResultsMenu : TrainerMenuBase
        {
            public ModelEntry[] Models { get; private set; }

            public SearchResultsMenu() : base("")
            {
                OnItemSelect += OnItemActivated;
            }

            public void SetSearchResults(string searchTerm, IEnumerable<ModelEntry> models)
            {
                SubtitleText = SubMenuTitle($"'{searchTerm}' SEARCH RESULTS");

                Clear();

                Models = models.OrderBy(m => m.ItemName)
                               .OrderBy(m => m.ItemSecondaryName)
                               .ToArray();
                AddItems(Models.Select(m => new UIMenuItem(m.ItemName) { RightLabel = m.ItemSecondaryName }));

                if (Models.Length == 0)
                {
                    DescriptionOverride = $"No vehicle models found for '{searchTerm}'.";
                }
                else
                {
                    DescriptionOverride = null;
                }
            }

            protected override void MenuCloseEv()
            {
                Clear();
                Models = null;

                base.MenuCloseEv();
            }

            private void OnItemActivated(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                SpawnVehicle(Models[index].Model);
            }
        }
    }
}
