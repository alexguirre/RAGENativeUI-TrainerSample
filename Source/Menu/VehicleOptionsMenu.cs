namespace RNUITrainerSample.Menu
{
    using System;
    using System.Drawing;

    using Rage;
    using Rage.Native;

    using RAGENativeUI;
    using RAGENativeUI.Elements;

    internal sealed class VehicleOptionsMenu : VehicleTrainerMenuBase
    {
        private readonly UIMenuItem repair, speedBomb;
        private readonly UIMenuNumericScrollerItem<float> driveForce, topSpeed, dirtLevel;
        private readonly UIMenuNumericScrollerItem<int> doors;

        private GameFiber speedBombFiber;

        public VehicleOptionsMenu() : base(SubMenuTitle("VEHICLE OPTIONS"))
        {
            repair = new UIMenuItem("Repair");
            repair.Activated += (s, i) => { if (CurrentVehicle) CurrentVehicle.Repair(); };

            driveForce = new UIMenuNumericScrollerItem<float>("Drive Force", "", 0.0f, 1000.0f, 0.01f) { Value = 0.0f }.WithTextEditing();
            driveForce.IndexChanged += (s, o, n) => { if (CurrentVehicle) CurrentVehicle.DriveForce = driveForce.Value; };

            topSpeed = new UIMenuNumericScrollerItem<float>("Top Speed (m/s)", "", 0.0f, 1000.0f, 0.01f) { Value = 0.0f }.WithTextEditing();
            topSpeed.IndexChanged += (s, o, n) => { if (CurrentVehicle) CurrentVehicle.TopSpeed = topSpeed.Value; };

            dirtLevel = new UIMenuNumericScrollerItem<float>("Dirt Level", "", 0.0f, 15.0f, 0.01f) { Value = 0.0f }.WithTextEditing();
            dirtLevel.IndexChanged += (s, o, n) => { if (CurrentVehicle) CurrentVehicle.DirtLevel = dirtLevel.Value; };

            doors = new UIMenuNumericScrollerItem<int>("Doors", "Select which door to open and close.", -1, 0, 1) { Value = DoorIdAll };
            doors.Formatter = i => doors.Maximum == DoorIdNone ? "" : i switch
            {
                DoorIdAll => "All",
                _ => Game.GetLocalizedString($"PM_DPV{i}") ?? i.ToString()
            };
            doors.IndexChanged += (s, o, n) => doors.Description = doors.Maximum == DoorIdNone ? "" : doors.Value switch
            {
                DoorIdAll => "Select to open all doors.",
                DoorIdNone => "Select to close all doors.",
                _ => "Select which door to open and close."
            };
            doors.Activated += (s, i) => DoorActivated(doors.Value);

            speedBomb = new UIMenuItem("Speed Bomb");
            speedBomb.Activated += (s, i) =>
            {
                if (!CurrentVehicle)
                {
                    return;
                }

                speedBomb.Enabled = false;
                speedBombFiber = GameFiber.StartNew(SpeedBombRoutine);
            };

            AddItems(repair, driveForce, topSpeed, dirtLevel, doors, speedBomb);
            this.WithFastScrollingOn(driveForce, topSpeed, dirtLevel);
        }

        protected override void UpdateItems(bool vehChanged)
        {
            bool exists = CurrentVehicle;
            repair.Enabled = exists;
            speedBomb.Enabled = exists && speedBombFiber == null;
            driveForce.Enabled = exists;
            topSpeed.Enabled = exists;
            dirtLevel.Enabled = exists;
            doors.Enabled = exists;
            if (exists)
            {
                driveForce.Value = CurrentVehicle.DriveForce;
                topSpeed.Value = CurrentVehicle.TopSpeed;
                dirtLevel.Value = CurrentVehicle.DirtLevel;
                if (vehChanged)
                {
                    doors.Maximum = GetNumberOfVehicleDoors(CurrentVehicle);
                    doors.Reformat();
                }
            }
        }

        private const int DoorIdAll = -1, DoorIdNone = 0;
        private void DoorActivated(int doorId)
        {
            if (!CurrentVehicle)
            {
                return;
            }

            if (doorId == DoorIdAll || doorId == DoorIdNone)
            {
                int doorCount = GetNumberOfVehicleDoors(CurrentVehicle);
                for (int door = 0; door < doorCount; door++)
                {
                    if (doorId == DoorIdAll)
                    {
                        CurrentVehicle.Doors[door].Open(instantly: false);
                    }
                    else
                    {
                        CurrentVehicle.Doors[door].Close(instantly: false);
                    }
                }
            }
            else
            {
                int doorIndex = doorId - 1;
                var door = CurrentVehicle.Doors[doorIndex];

                if (door.IsOpen)
                {
                    door.Close(instantly: false);
                }
                else
                {
                    door.Open(instantly: false);
                }
            }
        }

        private static int GetNumberOfVehicleDoors(Vehicle veh) => NativeFunction.Natives.x92922A607497B14D<int>(veh); // _GET_NUMBER_OF_VEHICLE_DOORS

        private void SpeedBombRoutine()
        {
            var veh = CurrentVehicle;
            if (!veh)
            {
                return;
            }

            float maxSpeed = MathHelper.ConvertMetersPerSecondToKilometersPerHour(veh.TopSpeed - 5.0f); // kmph
            float minSpeed = maxSpeed - 50.0f; // kmph

            var timerBars = new TimerBarPool();
            var speedTB = new BarTimerBar("SPEED");
            speedTB.Markers.Add(new TimerBarMarker(minSpeed / maxSpeed));
            var detonationTB = new BarTimerBar("DETONATION");
            var timeTB = new TextTimerBar("TIME TO DISARM", "00:00");
            timeTB.TextStyle = timeTB.TextStyle.With(font: TextFont.ChaletLondonFixedWidthNumbers);

            timerBars.Add(speedTB, timeTB);

            const uint DisarmTime = 120_000; // ms
            const float DetonationTime = 10.0f; // s
            
            bool hasEnoughSpeed = false;
            bool isDetonationTBAdded = false;
            float detonation = 0.0f;
            uint endTime = Game.GameTime + DisarmTime;
            while (veh)
            {
                float speed = MathHelper.ConvertMetersPerSecondToKilometersPerHour(veh.Speed);

                if (speed >= minSpeed)
                {
                    if (!hasEnoughSpeed)
                    {
                        speedTB.ForegroundColor = HudColor.Green.GetColor();
                        speedTB.BackgroundColor = Color.FromArgb(120, speedTB.ForegroundColor);
                        hasEnoughSpeed = true;
                    }

                    detonation -= (1.0f / DetonationTime * 1.5f) * Game.FrameTime;
                }
                else
                {
                    if (hasEnoughSpeed)
                    {
                        speedTB.ForegroundColor = HudColor.Red.GetColor();
                        speedTB.BackgroundColor = Color.FromArgb(120, speedTB.ForegroundColor);
                        if (detonation < 0.0f)
                        {
                            detonation = 0.0f;
                        }
                        hasEnoughSpeed = false;
                    }

                    detonation += (1.0f / DetonationTime) * Game.FrameTime;

                    if (detonation >= 1.0f)
                    {
                        veh.Explode(makeExplosion: true);
                        break;
                    }
                }
                detonationTB.Percentage = detonation;

                if (isDetonationTBAdded)
                {
                    if (detonation <= 0.0f)
                    {
                        timerBars.Remove(detonationTB);
                        isDetonationTBAdded = false;
                    }
                }
                else
                {
                    if (detonation > 0.0f)
                    {
                        timerBars.Clear(); // note, there is no Insert at index
                        timerBars.Add(speedTB, detonationTB, timeTB);
                        isDetonationTBAdded = true;
                    }
                }

                speedTB.Percentage = speed / maxSpeed;

                uint currTime = Game.GameTime;
                if (currTime >= endTime)
                {
                    break;
                }
                else
                {
                    uint remaining = endTime - currTime;
                    timeTB.Text = TimeSpan.FromMilliseconds(remaining).ToString("mm\\:ss");
                }

                timerBars.Draw();

                GameFiber.Yield();
            }

            speedBomb.Enabled = CurrentVehicle;
            speedBombFiber = null;
        }
    }

    internal abstract class VehicleTrainerMenuBase : TrainerMenuBase
    {
        private Vehicle currentVehicle;

        protected Vehicle CurrentVehicle
        {
            get => currentVehicle;
            set
            {
                currentVehicle = value;

                UpdateItems(true);
            }
        }

        public VehicleTrainerMenuBase(string title) : base(title)
        {
        }

        public override void ProcessControl()
        {
            var playerCurrVehicle = Game.LocalPlayer.Character.LastVehicle;

            if (CurrentVehicle != playerCurrVehicle)
            {
                CurrentVehicle = playerCurrVehicle;
            }

            UpdateItems(false);

            base.ProcessControl();
        }

        protected virtual void UpdateItems(bool vehChanged) { }
    }
}
