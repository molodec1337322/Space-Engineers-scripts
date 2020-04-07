using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace Script1
{
    public sealed class Program : MyGridProgram
    {
        const string THRUST_UP = "тягаВверх";
        const string THRUST_DOWN = "тягаВниз";
        const string THRUST_RIGHT = "тягаВправо";
        const string THRUST_LEFT = "тягаВлево";
        const string THRUST_FORWARD = "тягаВперед";
        const string THRUST_BACKWARD = "тягаНазад";
        const string ARTIFICIAL_MASS = "допМасса";

        const string COCKPIT = "кокпит"; // название необходимых блоков в игре


        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); // буфер блоков

        List<IMyGravityGenerator> thrustUp = new List<IMyGravityGenerator>();
        List<IMyGravityGenerator> thrustDown = new List<IMyGravityGenerator>();
        List<IMyGravityGenerator> thrustRight = new List<IMyGravityGenerator>();
        List<IMyGravityGenerator> thrustLeft = new List<IMyGravityGenerator>();
        List<IMyGravityGenerator> thrustForward = new List<IMyGravityGenerator>();
        List<IMyGravityGenerator> thrustBackward = new List<IMyGravityGenerator>();

        List<IMyArtificialMassBlock> artificialMass = new List<IMyArtificialMassBlock>();

        IMyCockpit cockpit;

        MyShipVelocities velocity;

        public Program()
        {
            /*
             * 
             * Инициализация блоков управления
             * 
             */
            GridTerminalSystem.GetBlockGroupWithName(THRUST_UP).GetBlocks(blocks);
            GetBlockGroup(thrustUp, blocks);

            GridTerminalSystem.GetBlockGroupWithName(THRUST_DOWN).GetBlocks(blocks);
            GetBlockGroup(thrustDown, blocks);

            GridTerminalSystem.GetBlockGroupWithName(THRUST_RIGHT).GetBlocks(blocks);
            GetBlockGroup(thrustRight, blocks);

            GridTerminalSystem.GetBlockGroupWithName(THRUST_LEFT).GetBlocks(blocks);
            GetBlockGroup(thrustLeft, blocks);

            GridTerminalSystem.GetBlockGroupWithName(THRUST_FORWARD).GetBlocks(blocks);
            GetBlockGroup(thrustForward, blocks);

            GridTerminalSystem.GetBlockGroupWithName(THRUST_BACKWARD).GetBlocks(blocks);
            GetBlockGroup(thrustBackward, blocks);

            GridTerminalSystem.GetBlockGroupWithName(ARTIFICIAL_MASS).GetBlocks(blocks);
            GetBlockGroup(artificialMass, blocks);

            cockpit = GridTerminalSystem.GetBlockWithName(COCKPIT) as IMyCockpit; 

            Runtime.UpdateFrequency = UpdateFrequency.Update1; // частота обновлений скрипта (60 раз за секунду)
        }

        private void GetBlockGroup<BlockTypeInterface>(List<BlockTypeInterface> generators, List<IMyTerminalBlock> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                generators.Add((BlockTypeInterface)blocks[i]);
            }
        }

        /// <summary>
        /// Включение генераторов гравитации
        /// </summary>
        /// <param name="generators"></param>
        private void ThrustOn(List<IMyGravityGenerator> generators) 
        {
            for(int i = 0; i < generators.Count; i++)
            {
                generators[i].ApplyAction("OnOff_On");
            }
        }

        /// <summary>
        /// Выключение генераторов гравитации
        /// </summary>
        /// <param name="generators"></param>
        private void ThrustOff(List<IMyGravityGenerator> generators)
        {
            for (int i = 0; i < generators.Count; i++)
            {
                generators[i].ApplyAction("OnOff_Off");
            }
        }


        private void ArtificialMassOnOff(string on_off)
        {
            for (int i = 0; i < artificialMass.Count; i++)
            {
                artificialMass[i].ApplyAction(on_off);
            }
        }


        /// <summary>
        /// гаситель инерции
        /// </summary>
        private void InertionCompensator()
        {
            velocity = cockpit.GetShipVelocities(); // получение скорости корабля относительно мира

            Vector3D V3Dfow = cockpit.WorldMatrix.Forward; // вектор направления вперед у корабля
            Vector3D V3Dup = cockpit.WorldMatrix.Up; // вектор направления вверх у корабля
            Vector3D V3Dleft = cockpit.WorldMatrix.Left; // вектор направления влево у корабля

            Vector3D velocityV3Dleft = new Vector3D(velocity.LinearVelocity.X, 0, 0); // вектор скорости по оси X (лево/право)
            Vector3D velocityV3Dup = new Vector3D(0, velocity.LinearVelocity.Y, 0); // вектор скорости по оси Y (вверх/вниз)
            Vector3D velocityV3Dfow = new Vector3D(0, 0, velocity.LinearVelocity.Z); // вектор скорости по оси Z (вперед/назад)

            double projectionLeft = velocityV3Dleft.Length() * Vector3D.Dot(velocityV3Dleft, V3Dleft) + 
                velocityV3Dfow.Length() * Vector3D.Dot(velocityV3Dfow, V3Dleft) + 
                velocityV3Dup.Length() * Vector3D.Dot(velocityV3Dup, V3Dleft);

            double projectionUp = velocityV3Dleft.Length() * Vector3D.Dot(velocityV3Dleft, V3Dup) +
                velocityV3Dfow.Length() * Vector3D.Dot(velocityV3Dfow, V3Dup) +
                velocityV3Dup.Length() * Vector3D.Dot(velocityV3Dup, V3Dup);

            double projectionFow = velocityV3Dleft.Length() * Vector3D.Dot(velocityV3Dleft, V3Dfow) +
                velocityV3Dfow.Length() * Vector3D.Dot(velocityV3Dfow, V3Dfow) +
                velocityV3Dup.Length() * Vector3D.Dot(velocityV3Dup, V3Dfow); // нахождение длин проекций по 3 осям

            /*
             * Управление тягой вперед/назад
             */
            if (projectionFow > 10)
            {
                ThrustOn(thrustBackward);
                ThrustOff(thrustForward);
            }
            else if (projectionFow < -10)
            {
                ThrustOn(thrustForward);
                ThrustOff(thrustBackward);
            }
            else
            {
                ThrustOff(thrustForward);
                ThrustOff(thrustBackward);
            }

            /*
             * Управление тягой влево/вправо
             */
            if (projectionLeft > 10)
            {
                ThrustOn(thrustRight);
                ThrustOff(thrustLeft);
                
            }
            else if (projectionLeft < -10)
            {
                ThrustOn(thrustLeft);
                ThrustOff(thrustRight);
            }
            else
            {
                ThrustOff(thrustRight);
                ThrustOff(thrustLeft);
            }

            /*
             * Управление тягой вверх/вниз
             */
            if (projectionUp > 10)
            {
                ThrustOn(thrustDown);
                ThrustOff(thrustUp);
            }
            else if (projectionUp < -10)
            {
                ThrustOn(thrustUp);
                ThrustOff(thrustDown);
            }
            else
            {
                ThrustOff(thrustUp);
                ThrustOff(thrustDown);
            }
        }

        public void Main(string args)
        {
            /*
             * Управление кораблем с помощью клавишь на клавиатуре 
             */


            if (cockpit.MoveIndicator.X > 0)
            {
                ThrustOn(thrustRight);
                ThrustOff(thrustLeft);
            }
            else if (cockpit.MoveIndicator.X < 0)
            {
                ThrustOn(thrustLeft);
                ThrustOff(thrustRight);
            }
            else
            {
                ThrustOff(thrustRight);
                ThrustOff(thrustLeft);
            }


            if (cockpit.MoveIndicator.Z > 0)
            {
                ThrustOn(thrustBackward);
                ThrustOff(thrustForward);
            }
            else if (cockpit.MoveIndicator.Z < 0)
            {
                ThrustOn(thrustForward);
                ThrustOff(thrustBackward);
            }
            else
            {
                ThrustOff(thrustForward);
                ThrustOff(thrustBackward);
            }


            if (cockpit.MoveIndicator.Y > 0)
            {
                ThrustOn(thrustUp);
                ThrustOff(thrustDown);
            }
            else if (cockpit.MoveIndicator.Y < 0)
            {
                ThrustOn(thrustDown);
                ThrustOff(thrustUp);
            }
            else
            {
                ThrustOff(thrustUp);
                ThrustOff(thrustDown);
            }


            /*
             * Если никакого управления нет, то блоки искусственной массы, необходимы для работы двигателя, выключаются (тк утяжеляют корабль)
             */
            if(cockpit.MoveIndicator.X == 0 && cockpit.MoveIndicator.Y == 0 && cockpit.MoveIndicator.Z == 0)
            {
                ArtificialMassOnOff("OnOff_On");
                InertionCompensator();
            }
            else
            {
                ArtificialMassOnOff("OnOff_On");
            }
        }

        public void Save()
        { }

    }
}
