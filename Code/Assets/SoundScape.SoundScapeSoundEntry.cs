namespace Sandbox
{
	public partial class SoundScape
	{
		public class SoundScapeSoundEntry
		{
			public string SoundFile { get; set; }
			public TagList SoundTag { get; set; }
			public bool RandomPosition { get; set; }
			public int SoundPositionIndex { get; set; }
			public string SoundPositionEntityName { get; set; }
			public Vector3 SoundPosition { get; set; }
			public Vector2 RandomPositionRadiusMinMax { get; set; }
			public RangedFloat Volume { get; set; } = new( 1 );
			public float CurrentVolume = 1f;
			public RangedFloat Pitch { get; set; } = new( 1 );
			public RangedFloat RepeatTime { get; set; } = new( -1f );



			public Sound SoundInstance;
			private bool localspace = true;
			private Vector3 GeneratedPosition = new();
			private Vector3 LastPosition = new();

			private TimeSince Generated = new();
			private float ToRepeatTime = 0f;

			public void StopSound()
			{
				SoundInstance.Stop();
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
				CurrentVolume = Volume.GetValue();
				SoundInstance.SetVolume( CurrentVolume );//Rand.Float( Volume.x, Volume.y == 0 ? Volume.x : Volume.y ) );
				Sounds.Add( SoundInstance );

				ToRepeatTime = RepeatTime.GetValue();
				Generated = 0f;
			}

			public void UpdateSound()
			{
				if ( SoundScapeEntity.DebugSoundscapes ) DebugOverlay.Sphere( LastPosition, 16, Color.Blue, false );
				if ( localspace )
				{
					LastPosition = GeneratedPosition + Origin;
					SoundInstance.SetPosition( LastPosition );
				}
				//Log.Error( Generated + " IDK " + ToRepeatTime );
				if ( RepeatTime.x != -1f && Generated > ToRepeatTime ) StartSound();
			}

			public async void FadeVolumeTo( float volume, float seconds = 1, int steps = 100 )
			{
				if ( volume == CurrentVolume ) return;

				var initialVolume = CurrentVolume;
				var volumeMod = initialVolume - volume;
				for ( int i = 0; i < steps; i++ )
				{
					SoundInstance.SetVolume( initialVolume - (i * volumeMod / steps) );
					CurrentVolume = initialVolume - (i * volumeMod / steps);
					await GameTask.DelaySeconds( seconds / steps );
				}
			}
			public void SetVolumeTo( float volume )
			{
				SoundInstance.SetVolume( volume );
				CurrentVolume = volume;
			}
		}

	}
}
