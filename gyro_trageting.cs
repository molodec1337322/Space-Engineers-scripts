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

namespace Script3
{
    public sealed class Program : MyGridProgram
    {

		const string GYRO_NAME = "gyros";
		const string REMCON_NAME = "remcom";
		const string THRUSTERS_FORWARD_NAME = "thrusts forward";

		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

		IMyCameraBlock cam;
		IMyRemoteControl remCon;
		List<IMyGyro> gyros = new List<IMyGyro>();
		List<IMyThrust> thrusts = new List<IMyThrust>();
		float gyroMutiplier = 4.0f;

		Vector3D target = new Vector3D(-52151.28, 36483.21, 15614.71);

		int ticks;
		const int targetTicks = 6;
		const int engineStartTicks = 36;

        public Program()
        {
			remCon = GridTerminalSystem.GetBlockWithName(REMCON_NAME) as IMyRemoteControl;

			GridTerminalSystem.GetBlockGroupWithName(GYRO_NAME).GetBlocks(blocks);
			GetGyros(gyros, blocks);
			GridTerminalSystem.GetBlockGroupWithName(THRUSTERS_FORWARD_NAME).GetBlocks(blocks);
			GetThrusts(thrusts, blocks);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

		private void GetThrusts(List<IMyThrust> thrusts, List<IMyTerminalBlock> blocks)
		{
			for(int i = 0; i < blocks.Count; i++)
			{
				thrusts.Add(blocks[i] as IMyThrust);
			}
		}

		private void GetGyros(List<IMyGyro> gyros, List<IMyTerminalBlock> blocks)
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				gyros.Add(blocks[i] as IMyGyro);
			}
		}

		/// <summary>
		/// Возвращает углы наводки на цель
		/// </summary>
		/// <param name="Target">координаты точки, нак оторую требуется доводка</param>
		/// <returns></returns>
		private Vector3D GetNavAngles(Vector3D Target)
		{
			Vector3D V3DCenter = remCon.GetPosition();
			Vector3D V3Dfow = remCon.WorldMatrix.Forward;
			Vector3D V3Dup = remCon.WorldMatrix.Up;
			Vector3D V3Dleft = remCon.WorldMatrix.Left;

			Vector3D TargetNorm = Vector3D.Normalize(Target - V3DCenter);

			double TargetPitch = Math.Acos(Vector3D.Dot(V3Dup, TargetNorm)) - (Math.PI / 2);
			double TargetYaw = Math.Acos(Vector3D.Dot(V3Dleft, TargetNorm)) - (Math.PI / 2);
			double TargetRoll = 0.0f;

			return new Vector3D(TargetYaw, -TargetPitch, TargetRoll);
		}

		private void SetGyroOverride(bool overrideOnOff, Vector3D target, float power = 1)
		{
			for(int i = 0; i < gyros.Count; i++)
			{
				gyros[i].GyroOverride = overrideOnOff;

				gyros[i].SetValue("Power", power);
				gyros[i].SetValue("Yaw", (float)target.GetDim(0));
				gyros[i].SetValue("Pitch", (float)target.GetDim(1));
				gyros[i].SetValue("Roll", (float)target.GetDim(2));
			}
		}

		private void ThrustsOnOff(double percents)
		{
			for(int i = 0; i < thrusts.Count; i++)
			{
				thrusts[i].ThrustOverridePercentage = (float)percents;
			}
		}


		public void Main(string args)
        {
			ticks++;
			if (ticks < targetTicks)
			{
				ThrustsOnOff(1);
			}
			if (ticks >= targetTicks)
			{
				SetGyroOverride(true, GetNavAngles(target) * gyroMutiplier, 1);
			}
			if(ticks >= engineStartTicks)
			{
				ThrustsOnOff(1);
			}
			
		}

        public void Save()
        { }

    }
}
