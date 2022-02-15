using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Soundscape.Extensions;
using Hammer;
using System.Linq;
using System.Collections.Generic;

namespace Sandbox
{

	[Skip] //SKIP Entity
	public abstract partial class BaseSoundScapeFader : Entity
	{
		[ClientVar( "jess_debug_fader" )]
		public static bool DebugFader { get; set; } = false;

		/// <summary>
		/// Seperated by , uses TagList Internally
		/// </summary>
		[Property, FGDType( "string" )] public TagList FromTag { get; set; } = new();
		/// <summary>
		/// Seperated by , uses TagList Internally
		/// </summary>
		[Property, FGDType( "string" )] public TagList MinValues { get; set; } = new();
		/// <summary>
		/// Seperated by , uses TagList Internally
		/// </summary>
		[Property, FGDType( "string" )] public TagList MaxValues { get; set; } = new();

		[Net] public List<string> fromTags { get; set; }
		[Net] public List<float> minValues { get; set; }
		[Net] public List<float> maxValues { get; set; }




		public float[] CurrentValues;


		/// <summary>
		/// Optional:
		/// If specified will only Fade if the current Soundscape is the specified one.
		/// </summary>
		[Property, FGDType( "target_destination" ), Net] public string SoundscapeEntity { get; set; }

		public SoundScapeEntity soundScapeEntity;

		public bool IsEnabled = true;
		public bool IsInside = false;

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;

			fromTags = FromTag.ToList();

			for ( int i = 0; i < fromTags.Count; i++ )
			{
				minValues.Add( MinValues.ElementAtOrDefault( i ).ToFloat() );
				maxValues.Add( MaxValues.ElementAtOrDefault( i ).ToFloat( 1f ) );
			}
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			GetSoundscapeEntity();

			CurrentValues = new float[fromTags.Count];
		}

		private async void GetSoundscapeEntity()
		{
			await GameTask.NextPhysicsFrame();

			soundScapeEntity = FindByName( SoundscapeEntity ) as SoundScapeEntity;
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
			for ( int i = 0; i < fromTags.Count; i++ )
			{

				SoundScape.FadeTagTo( fromTags[i], state ? maxValues[i] : minValues[i], time );
			}

		}

	}

	[Library( "snd_soundscape_fader_point_jess" ), Skip, Sphere( "InnerRadius", 77, 147, 191 ), Sphere( "OuterRadius", 255, 255, 255 )]
	public partial class SoundScapeTagFaderPoint : BaseSoundScapeFader
	{

		[Property, Net] public float InnerRadius { get; set; } = 50f;
		[Property, Net] public float OuterRadius { get; set; } = 100f;





		public float InnerRadiusSqrt;
		public float OuterRadiusSqrt;

		public override void ClientSpawn()
		{
			InnerRadiusSqrt = InnerRadius * InnerRadius;
			OuterRadiusSqrt = OuterRadius * OuterRadius;
		}





		[Event.Tick.Client]
		public void ClientTick()
		{
			if ( !IsEnabled || Local.Pawn == null ) return;
			if ( DebugFader )
			{
				DebugOverlay.Sphere( Position, InnerRadius, Color.FromBytes( 77, 147, 191 ) );
				DebugOverlay.Sphere( Position, OuterRadius, Color.FromBytes( 255, 255, 255 ) );
			}
			var EyePosition = Local.Pawn.EyePosition;
			if ( (EyePosition - Position).LengthSquared < OuterRadiusSqrt )
			{
				for ( int i = 0; i < fromTags.Count; i++ )
				{
					string tag = fromTags[i];

					CurrentValues[i] = MathX.LerpTo( minValues[i], maxValues[i], (EyePosition - Position).LengthSquared.Remap( InnerRadiusSqrt, OuterRadiusSqrt, 0f, 1f ) );
					if ( CurrentValues[i].AlmostEqual( minValues[i], 0.1f ) ) CurrentValues[i] = minValues[i];
					IsInside = true;

					if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
					{

						SoundScape.FadeTagTo( tag, CurrentValues[i], 0f );

					}
				}
			}
			else if ( IsInside )
			{
				for ( int i = 0; i < fromTags.Count; i++ )
				{
					string tag = fromTags[i];
					CurrentValues[i] = maxValues[i];
					IsInside = false;

					if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
					{

						SoundScape.FadeTagTo( tag, maxValues[i], 0f );

					}
				}
			}

		}






	}
	[Library( "snd_soundscape_fader_obb_jess" ), Skip, BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 )]
	public partial class SoundScapeTagFaderOBB : BaseSoundScapeFader
	{
		[Property, Skip, Net] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property, Skip, Net] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property, Skip, Net] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property, Skip, Net] public Vector3 outer_maxs { get; set; } = new( 500 );


		protected BBox innerbbox;
		protected BBox outerbbox;

		public float CurrentValue = 0f;

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			innerbbox = new( inner_mins, inner_maxs );
			outerbbox = new( outer_mins, outer_maxs );
		}

		[Event.Tick.Client]
		public void ClientTick()
		{
			if ( !IsEnabled || Local.Pawn == null ) return;

			if ( DebugFader )
			{
				var outerbboxWorld = outerbbox.ToWorldSpace( this );
				DebugOverlay.Box( outerbboxWorld.Mins, outerbboxWorld.Maxs, Color.White, 0f );
				var innerbboxWorld = innerbbox.ToWorldSpace( this );
				DebugOverlay.Box( innerbboxWorld.Mins, innerbboxWorld.Maxs, Color.Blue, 0f );
				for ( int i = 0; i < fromTags.Count; i++ )
				{
					DebugOverlay.ScreenText( i, $"Tag:{fromTags[i].Trim()} ,CurrentValue: {CurrentValues[i]} , MinValue : {minValues[i]} ,  MaxValue :{maxValues[i]}" );
				}
			}

			var EyePosition = Local.Pawn.EyePosition;
			if ( outerbbox.Contains( new( Transform.PointToLocal( EyePosition ) ) ) )
			{
				IsInside = true;

				var innerPoint = innerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePosition );
				var outerPoint = outerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePosition );
				if ( DebugFader )
				{
					DebugOverlay.Line( innerPoint, outerPoint );
				}
				if ( innerbbox.Contains( new( Transform.PointToLocal( EyePosition ) ) ) )
				{
					for ( int i = 0; i < fromTags.Count; i++ )
					{
						CurrentValues[i] = maxValues[i];
					}
					return;
				}

				for ( int i = 0; i < fromTags.Count; i++ )
				{
					CurrentValues[i] = MathX.LerpTo( maxValues[i], minValues[i], (innerPoint - Local.Pawn.EyePosition).Length.Remap( 0, (innerPoint - outerPoint).Length, 0, 1 ) );
				}
				if ( DebugFader ) DebugOverlay.Line( innerPoint, Local.Pawn.EyePosition );


				if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
				{
					for ( int i = 0; i < fromTags.Count; i++ )
					{
						float item = CurrentValues[i];
						SoundScape.SetTagTo( fromTags[i].Trim(), item );
					}
				}

			}
			else if ( IsInside )
			{
				IsInside = false;

				for ( int i = 0; i < fromTags.Count; i++ )
				{
					CurrentValues[i] = minValues[i];
				}
				if ( innerbbox.Contains( new( Transform.PointToLocal( EyePosition ) ) ) )
				{
					for ( int i = 0; i < fromTags.Count; i++ )
					{
						CurrentValues[i] = maxValues[i];
					}
				}

				if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
				{
					for ( int i = 0; i < fromTags.Count; i++ )
					{
						float item = CurrentValues[i];
						SoundScape.SetTagTo( fromTags[i].Trim(), item );
					}
				}
			}
		}



	}
}



