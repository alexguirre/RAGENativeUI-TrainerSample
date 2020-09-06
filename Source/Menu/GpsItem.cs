namespace RNUITrainerSample.Menu
{
    using System.Linq;

    using Rage;
    using Rage.Native;

    using RAGENativeUI;
    using RAGENativeUI.Elements;

    internal sealed class GpsItem : UIMenuNumericScrollerItem<int>
    {
        private const string PlaceDescription = "Select to place the waypoint.";
        private const string RemoveDescription = "Select to remove the waypoint.";

        public GpsItem() : base("Quick GPS", RemoveDescription, 0, Destinations.Length - 1, 1)
        {
            Formatter = i => Destinations[i].Name;
            Activated += OnActivated;
        }

        protected override void OnSelectedIndexChanged(int oldIndex, int newIndex)
        {
            Description = Destinations[newIndex].Locations == null ? RemoveDescription : PlaceDescription;

            base.OnSelectedIndexChanged(oldIndex, newIndex);
        }

        private void OnActivated(UIMenu sender, UIMenuItem selectedItem)
        {
            var dest = Destinations[Value];

            if (dest.Locations == null)
            {
                NativeFunction.Natives.xD8E694757BCEA8E9(); // _DELETE_WAYPOINT
            }
            else
            {
                var playerPos = Game.LocalPlayer.Character.Position;
                var closest = dest.Locations.OrderBy(loc => Vector3.DistanceSquared(playerPos, loc)).First();
                NativeFunction.Natives.SetNewWaypoint(closest.X, closest.Y);
            }
            NativeFunction.Natives.RefreshWaypoint();
        }

        private static readonly Destination[] Destinations = new[]
        {
            new Destination("None", null),
            new Destination(
                "Ammu-Nation", new[]
                {
                    new Vector3(1697.979f, 3753.2f, 0.0f),
                    new Vector3(245.2711f, -45.8126f, 0.0f),
                    new Vector3(844.1248f, -1025.571f, 0.0f),
                    new Vector3(-325.8904f, 6077.026f, 0.0f),
                    new Vector3(-664.2718f, -943.3646f, 0.0f),
                    new Vector3(-1313.948f, -390.9637f, 0.0f),
                    new Vector3(-1111.238f, 2688.463f, 0.0f),
                    new Vector3(-3165.231f, 1082.855f, 0.0f),
                    new Vector3(2569.612f, 302.576f, 0.0f),
                    new Vector3(17.6804f, -1114.288f, 0.0f),
                    new Vector3(811.8699f, -2149.102f, 0.0f),
                })
        };

        private readonly struct Destination
        {
            public string Name { get; }
            public Vector3[] Locations { get; }

            public Destination(string name, Vector3[] locations)
                => (Name, Locations) = (name, locations);
        } 
    }
}
