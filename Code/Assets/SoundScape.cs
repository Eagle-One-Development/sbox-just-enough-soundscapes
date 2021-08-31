using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sandbox
{
	[Library( "sndscape" )]
	public class SoundScape : Asset
	{
		public static Vector3 Origin = new();
		public class SoundScapeSoundEntry
		{
			public string SoundFile { get; set; }
			public bool RandomPosition { get; set; }
			public int SoundPositionIndex { get; set; }
			public string SoundPositionEntityName { get; set; }
			public Vector3 SoundPosition { get; set; }
			public Vector2 RandomPositionRadiusMinMax { get; set; }
			public RangedFloat Volume { get; set; } = new( 1 );
			public RangedFloat Pitch { get; set; } = new( 1 );
			public RangedFloat RepeatTime { get; set; } = new( -1f );

			private Sound SoundInstance;
			private bool localspace = true;
			private Vector3 GeneratedPosition = new();
			private Vector3 LastPosition = new();

			private TimeSince Generated = new();
			private float ToRepeatTime = 0f;

			public void StopSound()
			{
				if ( SoundInstance.Index != 0 )
				{
					SoundInstance.Stop();
				}
			}

			public void StartSound()
			{

				SoundInstance.Stop();
				if ( SoundPositionIndex >= 0 && SoundScapeEntity.SoundPositions[SoundPositionIndex] is Vector3 ent && ent.Length != 0f )
				{
					LastPosition = ent;
					SoundInstance = Sound.FromWorld( SoundFile, ent );
					localspace = false;
				}
				else if ( !string.IsNullOrEmpty( SoundPositionEntityName ) && Entity.FindByName( SoundPositionEntityName ) is Entity entinst )
				{
					SoundInstance = Sound.FromEntity( SoundFile, entinst );
					localspace = false;
				}
				else if ( RandomPosition )
				{
					float min = RandomPositionRadiusMinMax.x;
					float max = RandomPositionRadiusMinMax.y;
					if ( max == 0f )
					{
						max = min;
					}
					Vector3 Position = new( Rand.Float( -1, 1 ), Rand.Float( -1, 1 ), Rand.Float( -1, 1 ) );
					Position = Position.Normal;
					Position.x *= min;
					Position.y *= min;
					Position.z *= min;
					Position.x += (max - min) * Rand.Float( -1, 1 );
					Position.y += (max - min) * Rand.Float( -1, 1 );
					Position.z += (max - min) * Rand.Float( -1, 1 );
					GeneratedPosition = Position;
					SoundInstance = Sound.FromWorld( SoundFile, Origin + Position );
					localspace = true;

				}
				else
				{
					GeneratedPosition = SoundPosition;
					SoundInstance = Sound.FromWorld( SoundFile, Origin + SoundPosition );
					localspace = true;

				}
				SoundInstance.SetPitch( Pitch.GetValue() );
				SoundInstance.SetVolume( Volume.GetValue() );//Rand.Float( Volume.x, Volume.y == 0 ? Volume.x : Volume.y ) );
				Sounds.Add( SoundInstance );

				ToRepeatTime = RepeatTime.GetValue();
				Generated = 0f;
			}

			public void UpdateSound()
			{
				if ( localspace )
				{
					LastPosition = GeneratedPosition + Origin;
					SoundInstance.SetPosition( LastPosition );
				}
				if ( SoundScapeEntity.DebugSoundscapes ) DebugOverlay.Sphere( LastPosition, 16, Color.Blue, false );
				//Log.Error( Generated + " IDK " + ToRepeatTime );
				if ( RepeatTime.x != -1f && Generated > ToRepeatTime ) StartSound();
			}
		}
		public string[] SoundScapes { get; set; }
		public List<SoundScape> SecondarySoundscapes = new();
		public SoundScapeSoundEntry[] SoundEntries { get; set; }




		public static Dictionary<string, SoundScape> ByName = new();
		public static Dictionary<string, SoundScape> ByPath = new();

		private static SoundScape PlayingSoundScape;

		private static List<Sound> Sounds = new();

		private bool IsActive = false;


		private static SoundScapeEntity SoundScapeEntity;
		protected override void PostLoad()
		{
			ByName[Name] = this;
			ByPath[Path] = this;
		}

		public static void hotloaded()
		{
			foreach ( var item in Sounds )
			{
				item.Stop();
			}
			Sounds = new();
		}

		[Event.Tick.Client]
		public static void ClientTick()
		{
			if ( PlayingSoundScape == null ) return;
			PlayingSoundScape.Update();
			if ( SoundScapeEntity.DebugSoundscapes ) DebugOverlay.Sphere( SoundScapeEntity.Position, SoundScapeEntity.Radius, Color.Green );
		}

		public SoundScape Start()
		{
			IsActive = true;

			foreach ( var item in SecondarySoundscapes )
			{
				item.Start();
			}
			foreach ( var item in SoundEntries )
			{
				item.StartSound();
			}
			return this;
		}
		public static void StartSoundScape( SoundScapeEntity soundScapeEntity )
		{
			if ( SoundScapeEntity == soundScapeEntity || (SoundScapeEntity is not null && SoundScapeEntity.IsInside) || string.IsNullOrEmpty( soundScapeEntity.SoundScapeFileName ) || !ByName.ContainsKey( soundScapeEntity.SoundScapeFileName ) ) return;
			SoundScape sounds = ByName[soundScapeEntity.SoundScapeFileName];

			foreach ( var item in sounds.SoundScapes )
			{
				if ( ByPath.TryGetValue( item, out SoundScape scape ) )
				{
					Log.Info( scape.Name );
					sounds.SecondarySoundscapes.Add( scape );
				}
			}
			PlayingSoundScape?.Stop();
			SoundScapeEntity = soundScapeEntity;
			PlayingSoundScape = sounds.Start();
		}
		public void Stop()
		{
			IsActive = false;
			foreach ( var item in SecondarySoundscapes )
			{
				item.Stop();
			}
			foreach ( var item in SoundEntries )
			{
				item.StopSound();
			}
		}


		public void Update()
		{
			foreach ( var item in SecondarySoundscapes )
			{
				item.Update();
			}
			foreach ( var item in SoundEntries )
			{
				item.UpdateSound();
			}
		}
	}
}
