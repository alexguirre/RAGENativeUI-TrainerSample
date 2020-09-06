namespace RNUITrainerSample.Menu
{
    using Rage;

    using RAGENativeUI;
    using RAGENativeUI.Elements;

    internal sealed class TrainerMenu : TrainerMenuBase
    {
        public TrainerMenu() : base(TopTitle)
        {
            var spawnVehicle = new UIMenuItem("Spawn Vehicle");
            var vehicleOptions = new UIMenuItem("Vehicle Options");
            var quickGps = new GpsItem();
            var teleportToWaypoint = new UIMenuItem("Teleport to Waypoint");
            teleportToWaypoint.Activated += (s, i) => TeleportToWaypoint();

            AddItems(spawnVehicle, vehicleOptions, quickGps, teleportToWaypoint);
            BindMenuToItem(new SpawnVehicleMenu(), spawnVehicle);
            BindMenuToItem(new VehicleOptionsMenu(), vehicleOptions);
        }

        private void TeleportToWaypoint()
        {
            var wp = World.GetWaypointBlip();
            if (wp)
            {
                var pos = wp.Position;
                pos.Z = World.GetGroundZ(pos, true, true) ?? 0.0f;

                Game.LocalPlayer.Character.Position = pos;
            }
        }
    }

    internal abstract class TrainerMenuBase : UIMenu
    {
        public const string TopTitle = "TRAINER";
        public static string SubMenuTitle(string title) => $"{TopTitle}: {title}";

        public TrainerMenuBase(string title) : base("", title)
        {
            Plugin.Pool.Add(this);

            Width += 0.04f;
            RemoveBanner();
        }
    }
}
