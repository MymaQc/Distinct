using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Distinct {

    internal static class External {

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys key);

        private static void Main() {
            const Int32 LocalPlayer = 0xDBA5BC;
            const Int32 EntityList = 0x4DD69DC;
            const Int32 Health = 0x100;
            const Int32 Team = 0xF4;
            const Int32 ForceAttack = 0x3206E9C;
            const Int32 CrosshairID = 0x11838;
            const Int32 FlagID = 0x104;
            const Int32 ForceJump = 0x52789F8;
            const Int32 ClientState = 0x058BFC4;
            const Int32 ViewAngles = 0x4D90;
            const Int32 XYZ = 0x138;
            const Int32 Dormant = 0xED;

            Entity entity = new Entity();
            List<Entity> entities = new List<Entity>();

            Memory memory = new Memory();
            memory.GetProcess("csgo");

            IntPtr Client = memory.GetModuleBase("client.dll");
            IntPtr Engine = memory.GetModuleBase("engine.dll");

            while (true) {
                IntPtr HpBuffer = memory.ReadPointer(Client, LocalPlayer);
                int HpEnemy = BitConverter.ToInt32(memory.ReadBytes(HpBuffer, Health, 4), 0);
                Console.WriteLine("Player HP ---> " + HpEnemy);

                if (GetAsyncKeyState((Keys)1) < 0 || GetAsyncKeyState((Keys)32|(Keys)128) < 0 || GetAsyncKeyState((Keys)68) < 0 || GetAsyncKeyState((Keys)65) < 0) {
                    IntPtr TriggerbotBuffer = memory.ReadPointer(Client, LocalPlayer);
                    int Crosshair = BitConverter.ToInt32(memory.ReadBytes(TriggerbotBuffer, CrosshairID, 4), 0);
                    int OurTeam = BitConverter.ToInt32(memory.ReadBytes(TriggerbotBuffer, Team, 4), 0);

                    IntPtr Enemy = memory.ReadPointer(Client, EntityList + (Crosshair - 1) * 0x10);
                    int EnemyTeam = BitConverter.ToInt32(memory.ReadBytes(Enemy, Team, 4), 0);
                    int EnemyHealth = BitConverter.ToInt32(memory.ReadBytes(Enemy, Health, 4), 0);

                    if (OurTeam != EnemyTeam && EnemyHealth > 1) {
                        memory.WriteBytes(Client, ForceAttack, BitConverter.GetBytes(5));
                        Thread.Sleep(1);
                        memory.WriteBytes(Client, ForceAttack, BitConverter.GetBytes(4));
                    }
                }
                
                if (GetAsyncKeyState((Keys)32) < 0) {
                    IntPtr BhopBuffer = memory.ReadPointer(Client, LocalPlayer);
                    int Flag = BitConverter.ToInt32(memory.ReadBytes(BhopBuffer, FlagID, 4), 0);

                    if (Flag == 257 || Flag == 261 || Flag == 263) {
                        memory.WriteBytes(Client, ForceJump, BitConverter.GetBytes(5));
                    } else {
                        memory.WriteBytes(Client, ForceJump, BitConverter.GetBytes(4));
                    }
                }

                if (GetAsyncKeyState((Keys)2|(Keys)4) < 0) {
                    IntPtr AimbotBuffer = memory.ReadPointer(Client, LocalPlayer);
                    byte[] Coords = memory.ReadBytes(AimbotBuffer, XYZ, 12);

                    entity.X = BitConverter.ToSingle(Coords, 0);
                    entity.Y = BitConverter.ToSingle(Coords, 4);
                    entity.Z = BitConverter.ToSingle(Coords, 8);
                    entities.Clear();
                    for (int i = 1; i < 32; i++) {
                        IntPtr EntityBuffer = memory.ReadPointer(Client, EntityList + 1 * 0x10);
                        int TM = BitConverter.ToInt32(memory.ReadBytes(EntityBuffer, Team, 4), 0);
                        int Dorm = BitConverter.ToInt32(memory.ReadBytes(EntityBuffer, Dormant, 4), 0);
                        int HP = BitConverter.ToInt32(memory.ReadBytes(EntityBuffer, Health, 4), 0);

                        if (HP < 2 || Dorm != 0 || TM == entity.Team) {
                            continue;
                        }

                        Entity enemy = new Entity {
                            X = BitConverter.ToSingle(Coords, 0),
                            Y = BitConverter.ToSingle(Coords, 4),
                            Z = BitConverter.ToSingle(Coords, 8),
                            Team = TM,
                            Health = HP,
                        };
                        enemy.Mag = (float)Math.Sqrt(Math.Pow(enemy.X - entity.X, 2) + Math.Pow(enemy.Y - entity.Y, 2) + Math.Pow(enemy.Z - entity.Z, 2));
                        entities.Add(enemy);
                    }
                    entities = entities.OrderBy(order => order.Mag).ToList();

                    if (entities.Count > 0) {
                        float deltaX = entities[0].X - entity.X;
                        float deltaY = entities[0].Y - entity.Y;

                        float X = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

                        float deltaZ = entities[0].Z - entity.Z;
                        double Distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

                        float Y = -(float)(Math.Atan2(deltaZ, Distance) * 180 / Math.PI);

                        IntPtr EngineBuffer = memory.ReadPointer(Engine, ClientState);
                        memory.WriteBytes(EngineBuffer, ViewAngles, BitConverter.GetBytes(Y));
                        memory.WriteBytes(EngineBuffer, ViewAngles + 0x4, BitConverter.GetBytes(X));
                    }
                }

                Thread.Sleep(3);
            }
        }

    }

}