using System;
using System.Collections.Generic;
using Hammer;


namespace Sandbox
{
	[Library( "snd_soundscape_jess" )]
	[EditorSprite( "editor/env_soundscape.vmat" )]
	[DrawSphere( "Radius" )]
	public partial class SoundScapeEntity : Entity
	{
		[ClientVar( "jess_debug_soundscape" )]
		public static bool DebugSoundscapes { get; set; } = false;

		public SoundScapeEntity()
		{
			Transmit = TransmitType.Always;
			Event.Register( this );
		}

		[Property, Net]
		public int Radius { get; set; } = 128;
		[Property, Net]
		public bool NeedsLineOfSight { get; set; }

		[Property( "soundscape" ), Net]
		public string SoundScapeFileName { get; set; }

		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition0 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition1 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition2 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition3 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition4 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition5 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition6 { get; set; }
		[Property( FGDType = "target_destination" ), Net]
		public string SoundPosition7 { get; set; }


		[Net] public List<Vector3> SoundPositions { get; set; } = new();

		public override void Spawn()
		{
			base.Spawn();
			GetPositions();
		}

		public async void GetPositions()
		{
			await GameTask.Delay( 100 );
			var props = Reflection.GetProperties( this );
			foreach ( var item in props )
			{
				if ( item.Name.StartsWith( "SoundPosition" ) )
				{
					if ( item.Value != null && FindByName( item.Value.ToString() ) is Entity ent )
						SoundPositions.Add( ent.Position );
					else SoundPositions.Add( new() );
				}
			}
		}


		/// <summary>
		/// Input: Tag,Volume,Time  split by comma \n
		/// So like this: Exterior,0.5,5 
		/// </summary>
		/// <param name="activator"></param>
		/// <param name="TagVolumeArrayTime"></param>
		[Input]
		public void FadeTagTo( Entity activator = null, string TagVolumeArrayTime = null )
		{
			if ( activator is Player p )
				ClientStuff( To.Single( p ), TagVolumeArrayTime );

		}
		[ClientRpc]
		public void ClientStuff( string TagVolumeArrayTime = null )
		{
			string[] strarr = TagVolumeArrayTime.Split( ',' );
			if ( strarr.Length >= 2 )
			{
				if ( SoundScape.PlayingSoundScape.ActiveSoundsByTag.ContainsKey( strarr[0] ) )
				{
					foreach ( var item in SoundScape.PlayingSoundScape?.ActiveSoundsByTag[strarr[0]] )
					{
						if ( strarr.Length == 3 )
						{
							item.FadeVolumeTo( StringX.ToFloat( strarr?[1], 1 ), StringX.ToFloat( strarr?[2], 1 ) );
						}
						else
						{
							item.FadeVolumeTo( StringX.ToFloat( strarr?[1], 1 ) );
						}

					}
				}

			}
		}




		public bool IsInside = false;


		[Event.Tick.Client]
		public void ClientTick()
		{
			SoundScape.Origin = (Local.Pawn.Camera as Camera).Pos;
			if ( Position.Distance( Local.Pawn.Position ) < Radius )
			{
				if ( !IsInside )
				{
					if ( NeedsLineOfSight && !Trace.Ray( Position, Local.Pawn.Position ).Ignore( Local.Pawn ).Run().Hit || !NeedsLineOfSight )
					{
						SoundScape.StartSoundScape( this );
					}
					if ( DebugSoundscapes ) Log.Error( "Starting new Soundscape: " + SoundScapeFileName );


				}
				IsInside = true;
			}
			else
			{
				if ( DebugSoundscapes ) DebugOverlay.Sphere( Position, Radius, Color.Red );
				IsInside = false;
			}
		}


	}
}
