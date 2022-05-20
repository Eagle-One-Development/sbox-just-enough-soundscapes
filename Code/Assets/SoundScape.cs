using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sandbox
{
	[GameResource( "SoundScape", "sndscape", "Create SoundScapes", Icon = "graphic_eq" )]
	public partial class SoundScape : GameResource
	{
		public static Vector3 Origin = new();
		public string[] SoundScapes { get; set; }
		public List<SoundScape> SecondarySoundscapes = new();
		public SoundScapeSoundEntry[] SoundEntries { get; set; }

		public Dictionary<string, List<SoundScapeSoundEntry>> ActiveSoundsByTag = new();




		public static Dictionary<string, SoundScape> ByName = new();
		public static Dictionary<string, SoundScape> ByPath = new();

		public static SoundScape PlayingSoundScape;

		private static List<Sound> Sounds = new();

		private bool IsActive = false;


		public static SoundScapeEntity SoundScapeEntity;
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
				if ( item.IsActive ) continue;
				item.Start();
			}
			foreach ( var item in SoundEntries )
			{
				item.StartSound();

				if ( !item.SoundTag.Any() ) continue;
				foreach ( var tag in item.SoundTag )
				{
					string t = tag.ToString();
					if ( !ActiveSoundsByTag.ContainsKey( t ) ) ActiveSoundsByTag.Add( t, new() );
					if ( ActiveSoundsByTag.TryGetValue( t, out List<SoundScapeSoundEntry> entryList ) )
					{
						entryList.Add( item );
					}

				}

			}
			Event.Register( this );
			return this;
		}
		public static bool StartSoundScape( SoundScapeEntity soundScapeEntity )
		{
			if ( string.IsNullOrEmpty( soundScapeEntity.SoundScapeFileName ) || !ByName.ContainsKey( soundScapeEntity.SoundScapeFileName ) ) return false;
			SoundScape sounds = ByName[soundScapeEntity.SoundScapeFileName];

			foreach ( var item in sounds.SoundScapes )
			{
				if ( ByPath.TryGetValue( item, out SoundScape scape ) )
				{
					sounds.SecondarySoundscapes.Add( scape );
				}
			}
			PlayingSoundScape?.Stop();
			SoundScapeEntity = soundScapeEntity;
			PlayingSoundScape = sounds.Start();
			if ( SoundScapeEntity.DebugSoundscapes ) Log.Error( "Starting new Soundscape: " + soundScapeEntity.SoundScapeFileName );
			return true;
		}
		public void Stop()
		{
			IsActive = false;
			foreach ( var item in SecondarySoundscapes )
			{
				if ( !item.IsActive ) continue;
				item.Stop();
			}
			foreach ( var item in SoundEntries )
			{
				item.StopSound();
			}

			Event.Unregister( this );
			ActiveSoundsByTag = new();
		}


		public void Update()
		{
			Event.Run( "JESS_UpdateSoundscapes" );

		}
		[Event( "JESS_UpdateSoundscapes" )]
		public void UpdateSounds()
		{
			foreach ( var item in SoundEntries )
			{
				item.UpdateSound();
			}
		}


		public static void FadeTagTo( string Tag, float Volume, float seconds = 0.1f, int steps = 100 )
		{

			if ( PlayingSoundScape != null && PlayingSoundScape.ActiveSoundsByTag.ContainsKey( Tag ) )
			{
				foreach ( var item in SoundScape.PlayingSoundScape?.ActiveSoundsByTag[Tag] )
				{
					item.FadeVolumeTo( Volume, seconds, steps );

				}
			}
		}
		public static void SetTagTo( string Tag, float Volume )
		{

			if ( PlayingSoundScape != null && PlayingSoundScape.ActiveSoundsByTag.ContainsKey( Tag ) )
			{
				foreach ( var item in SoundScape.PlayingSoundScape?.ActiveSoundsByTag[Tag] )
				{
					item.SetVolumeTo( Volume );

				}
			}
		}

	}
}
