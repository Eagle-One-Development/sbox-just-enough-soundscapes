using System;
using System.Collections.Generic;
using Hammer;


namespace Sandbox
{
	[Library( "snd_better_soundscape" )]
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




		public bool IsInside = false;


		[Event.Tick.Client]
		public void ClientTick()
		{
			SoundScape.Origin = (Local.Pawn.Camera as Camera).Pos;
			if ( Position.Distance( Local.Pawn.Position ) < Radius )
			{
				if ( !IsInside )
				{
					if ( DebugSoundscapes ) Log.Error( "Starting new Soundscape: " + SoundScapeFileName );

					SoundScape.StartSoundScape( this );
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
