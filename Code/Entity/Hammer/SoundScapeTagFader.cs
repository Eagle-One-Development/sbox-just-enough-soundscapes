using System;
using System.Diagnostics.Tracing;
using Hammer;

namespace Sandbox
{

	public abstract class BaseSoundScapeFader : Entity
	{
		[ClientVar( "jess_debug_fader" )]
		public static bool DebugFader { get; set; } = false;

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;
		}

	}
	[Library( "snd_soundscape_fader_point_jess" ), Sphere( "InnerRadius", 77, 147, 191 ), Sphere( "OuterRadius", 255, 255, 255 )]
	public partial class SoundScapeTagFaderPoint : BaseSoundScapeFader
	{

		[Property, Net] public float InnerRadius { get; set; } = 50f;
		[Property, Net] public float OuterRadius { get; set; } = 100f;

		[Property, Net] public float MinValue { get; set; } = 0f;
		[Property, Net] public float MaxValue { get; set; } = 1f;



		[Property, Net] public string FromTag { get; set; }
		/// <summary>
		/// Optional:
		/// If specified will only Fade if the current Soundscape is the specified one.
		/// </summary>
		[Property( FGDType = "target_destination" ), Net] public string SoundscapeEntity { get; set; }

		public SoundScapeEntity soundScapeEntity;

		public float InnerRadiusSqrt;
		public float OuterRadiusSqrt;

		public float CurrentValue = 0f;

		public override void ClientSpawn()
		{
			InnerRadiusSqrt = InnerRadius * InnerRadius;
			OuterRadiusSqrt = OuterRadius * OuterRadius;

			GetSoundscapeEntity();
		}
		private async void GetSoundscapeEntity()
		{
			await GameTask.NextPhysicsFrame();

			soundScapeEntity = FindByName( SoundscapeEntity ) as SoundScapeEntity;
		}

		public bool IsInside = false;
		public bool IsEnabled = true;

		[Event.Tick.Client]
		public void ClientTick()
		{
			if ( !IsEnabled ) return;
			if ( DebugFader )
			{
				DebugOverlay.Sphere( Position, InnerRadius, Color.FromBytes( 77, 147, 191 ) );
				DebugOverlay.Sphere( Position, OuterRadius, Color.FromBytes( 255, 255, 255 ) );
				DebugOverlay.ScreenText( 0, $"{CurrentValue}" );
			}
			var Eyepos = Local.Pawn.EyePos;
			if ( (Eyepos - Position).LengthSquared < OuterRadiusSqrt )
			{
				CurrentValue = MathX.LerpTo( MinValue, MaxValue, (Eyepos - Position).LengthSquared.Remap( InnerRadiusSqrt, OuterRadiusSqrt, 0f, 1f ) );
				if ( CurrentValue.AlmostEqual( MinValue, 0.1f ) ) CurrentValue = MinValue;
				IsInside = true;

				if ( soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, 0f );
				}
				else if ( !soundScapeEntity.IsValid() )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, 0f );
				}
			}
			else if ( IsInside )
			{
				CurrentValue = MaxValue;
				IsInside = false;

				if ( soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity )
				{
					SoundScape.FadeTagTo( FromTag, MaxValue, 0f );
				}
				else if ( !soundScapeEntity.IsValid() )
				{
					SoundScape.FadeTagTo( FromTag, MaxValue, 0f );
				}
			}

		}

		[Input]
		public void EnableFader( Entity activator = null )
		{
			if ( activator is Player p )
				SetFaderCL( To.Single( p ), true );
		}
		[Input]
		public void DisableFader( Entity activator = null )
		{
			if ( activator is Player p )
				SetFaderCL( To.Single( p ), false );
		}

		[ClientRpc]
		public void SetFaderCL( bool state )
		{
			IsEnabled = state;
		}

		[Input]
		public void SetMinValue( Entity activator = null, string time = null )
		{
			if ( activator is Player p )
				SetTagValueCL( To.Single( p ), false, time != null ? StringX.ToFloat( time ) : 1f );
		}
		[Input]
		public void SetMaxValue( Entity activator = null, string time = null )
		{
			if ( activator is Player p )
				SetTagValueCL( To.Single( p ), true, time != null ? StringX.ToFloat( time ) : 1f );
		}

		[ClientRpc]
		public void SetTagValueCL( bool state, float time )
		{
			if ( state )
			{
				SoundScape.FadeTagTo( FromTag, MaxValue, time );
			}
			else
			{
				SoundScape.FadeTagTo( FromTag, MinValue, time );
			}
		}




	}
	[Library( "snd_soundscape_fader_aabb_jess" ), BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 )]
	public partial class SoundScapeTagFaderAABB : BaseSoundScapeFader
	{
		[Property( Hammer = false ), Net] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property( Hammer = false ), Net] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property( Hammer = false ), Net] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property( Hammer = false ), Net] public Vector3 outer_maxs { get; set; } = new( 500 );

		[Event.Tick.Client]
		public void ClientTick()
		{

		}


	}
	[Library( "snd_soundscape_fader_direction_aabb_jess" ), BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 ), DrawAngles( "DirectionAngle", "angles" )]
	public partial class SoundScapeTagFaderDirectionalAABB : BaseSoundScapeFader
	{
		[Property( Hammer = false ), Net] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property( Hammer = false ), Net] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property( Hammer = false ), Net] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property( Hammer = false ), Net] public Vector3 outer_maxs { get; set; } = new( 500 );
		[Property, Net] public Angles DirectionAngle { get; set; } = new( Vector3.Forward.EulerAngles );

		[Event.Tick.Client]
		public void ClientTick()
		{

		}


	}

	public static class FloatExtensions
	{
		public static float Remap( this float input, float inputMin, float inputMax, float min, float max )
		{
			return min + (input - inputMin) * (max - min) / (inputMax - inputMin);
		}
	}
}
