using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Soundscape.Extensions;
using Hammer;

namespace Sandbox
{

	[Skip] //SKIP Entity
	public abstract partial class BaseSoundScapeFader : Entity
	{
		[ClientVar( "jess_debug_fader" )]
		public static bool DebugFader { get; set; } = false;

		[Property, Net] public float MinValue { get; set; } = 0f;
		[Property, Net] public float MaxValue { get; set; } = 1f;



		[Property, Net] public string FromTag { get; set; }
		/// <summary>
		/// Optional:
		/// If specified will only Fade if the current Soundscape is the specified one.
		/// </summary>
		[Property( FGDType = "target_destination" ), Net] public string SoundscapeEntity { get; set; }

		public SoundScapeEntity soundScapeEntity;

		public bool IsEnabled = true;
		public bool IsInside = false;

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			GetSoundscapeEntity();
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

	[Library( "snd_soundscape_fader_point_jess" ), Sphere( "InnerRadius", 77, 147, 191 ), Sphere( "OuterRadius", 255, 255, 255 )]
	public partial class SoundScapeTagFaderPoint : BaseSoundScapeFader
	{

		[Property, Net] public float InnerRadius { get; set; } = 50f;
		[Property, Net] public float OuterRadius { get; set; } = 100f;





		public float InnerRadiusSqrt;
		public float OuterRadiusSqrt;

		public float CurrentValue = 0f;

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






	}
	[Library( "snd_soundscape_fader_obb_jess" ), BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 )]
	public partial class SoundScapeTagFaderOBB : BaseSoundScapeFader
	{
		[Property( Hammer = false ), Net] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property( Hammer = false ), Net] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property( Hammer = false ), Net] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property( Hammer = false ), Net] public Vector3 outer_maxs { get; set; } = new( 500 );


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


				DebugOverlay.ScreenText( CurrentValue.ToString() );
			}

			var Eyepos = Local.Pawn.EyePos;
			if ( outerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
			{
				IsInside = true;

				var innerPoint = innerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePos );
				var outerPoint = outerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePos );
				if ( DebugFader )
				{
					DebugOverlay.Line( innerPoint, outerPoint );
					//DebugOverlay.ScreenText( 0, (innerPoint - outerPoint).Length.ToString() );
					//DebugOverlay.ScreenText( 1, (innerPoint - Local.Pawn.EyePos).Length.ToString() );

				}
				if ( innerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
				{
					CurrentValue = MaxValue;
					return;
				}


				CurrentValue = MathX.LerpTo( MaxValue, MinValue, (innerPoint - Local.Pawn.EyePos).Length.Remap( 0, (innerPoint - outerPoint).Length, 0, 1 ) );
				if ( DebugFader ) DebugOverlay.Line( innerPoint, Local.Pawn.EyePos );


				if ( soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, steps: 10 );
				}
				else if ( !soundScapeEntity.IsValid() )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, steps: 10 );
				}



			}
			else if ( IsInside )
			{
				IsInside = false;

				CurrentValue = MinValue;
				if ( innerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
				{
					CurrentValue = MaxValue;
				}

				if ( soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, steps: 1 );
				}
				else if ( !soundScapeEntity.IsValid() )
				{
					SoundScape.FadeTagTo( FromTag, CurrentValue, steps: 1 );
				}
			}
		}



	}
	[Library( "snd_soundscape_multi_fader_obb_jess" )]
	public partial class SoundScapeTagMultiFaderOBB : SoundScapeTagFaderOBB
	{
		public new string FromTag { get; set; }
		public new float MinValue { get; set; } = 0f;
		public new float MaxValue { get; set; } = 1f;

		/// <summary>
		/// Enter Tags seperated by ,
		/// </summary>
		[Property( FGDType = "text_block" ), Net] public string FromTags { get; set; }
		/// <summary>
		/// Enter MinValues seperated by ,
		/// </summary>
		[Property( FGDType = "text_block" ), Net] public string MinValues { get; set; }
		/// <summary>
		/// Enter MaxValues seperated by ,
		/// </summary>
		[Property( FGDType = "text_block" ), Net] public string MaxValues { get; set; }

		string[] tags;
		float[] minValues;
		float[] maxValues;


		float[] currentValues;



		public override void ClientSpawn()
		{
			base.ClientSpawn();

			tags = FromTags.Replace( '\n', ' ' ).Split( ',' );
			int index = 0;
			minValues = new float[tags.Length];
			foreach ( var item in MinValues.Replace( '\n', ' ' ).Split( ',' ) )
			{
				if ( index < tags.Length )
					minValues[index] = StringX.ToFloat( item.Trim() );
				index++;
			}

			maxValues = new float[tags.Length];
			index = 0;
			foreach ( var item in MaxValues.Replace( '\n', ' ' ).Split( ',' ) )
			{
				if ( index < tags.Length )
					maxValues[index] = StringX.ToFloat( item.Trim() );
				index++;
			}

			currentValues = new float[tags.Length];
		}


		[Event.Tick.Client]
		public new void ClientTick()
		{
			if ( !IsEnabled || Local.Pawn == null ) return;

			if ( DebugFader )
			{
				var outerbboxWorld = outerbbox.ToWorldSpace( this );
				DebugOverlay.Box( outerbboxWorld.Mins, outerbboxWorld.Maxs, Color.White, 0f );
				var innerbboxWorld = innerbbox.ToWorldSpace( this );
				DebugOverlay.Box( innerbboxWorld.Mins, innerbboxWorld.Maxs, Color.Blue, 0f );
				for ( int i = 0; i < currentValues.Length; i++ )
				{
					DebugOverlay.ScreenText( i, $"Tag:{tags[i].Trim()} ,CurrentValue: {currentValues[i]} , MinValue : {minValues[i]} ,  MaxValue :{maxValues[i]}" );
				}
			}

			var Eyepos = Local.Pawn.EyePos;
			if ( outerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
			{
				IsInside = true;

				var innerPoint = innerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePos );
				var outerPoint = outerbbox.ClosestPointToWorldSpace( Transform, Local.Pawn.EyePos );
				if ( DebugFader )
				{
					DebugOverlay.Line( innerPoint, outerPoint );
				}
				if ( innerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
				{
					for ( int i = 0; i < currentValues.Length; i++ )
					{
						currentValues[i] = maxValues[i];
					}
					return;
				}

				for ( int i = 0; i < currentValues.Length; i++ )
				{
					currentValues[i] = MathX.LerpTo( maxValues[i], minValues[i], (innerPoint - Local.Pawn.EyePos).Length.Remap( 0, (innerPoint - outerPoint).Length, 0, 1 ) );
				}
				if ( DebugFader ) DebugOverlay.Line( innerPoint, Local.Pawn.EyePos );


				if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
				{
					for ( int i = 0; i < currentValues.Length; i++ )
					{
						float item = currentValues[i];
						SoundScape.SetTagTo( tags[i].Trim(), item );
					}
				}

			}
			else if ( IsInside )
			{
				IsInside = false;

				for ( int i = 0; i < currentValues.Length; i++ )
				{
					currentValues[i] = minValues[i];
				}
				if ( innerbbox.Contains( new( Transform.PointToLocal( Eyepos ) ) ) )
				{
					for ( int i = 0; i < currentValues.Length; i++ )
					{
						currentValues[i] = maxValues[i];
					}
				}

				if ( (soundScapeEntity.IsValid() && SoundScape.SoundScapeEntity == soundScapeEntity) || !soundScapeEntity.IsValid() )
				{
					for ( int i = 0; i < currentValues.Length; i++ )
					{
						float item = currentValues[i];
						SoundScape.SetTagTo( tags[i].Trim(), item );
					}
				}
			}
		}


	}
}



