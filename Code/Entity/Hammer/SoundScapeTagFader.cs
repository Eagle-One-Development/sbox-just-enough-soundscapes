using System;
using Hammer;

namespace Sandbox
{

	public abstract class BaseSoundScapeFader : Entity
	{

	}
	[Library( "snd_soundscape_fader_point_jess" )]
	public class SoundScapeTagFaderPoint : BaseSoundScapeFader
	{

	}
	[Library( "snd_soundscape_fader_aabb_jess" ), BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 )]
	public class SoundScapeTagFaderAABB : BaseSoundScapeFader
	{
		[Property( Hammer = false )] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property( Hammer = false )] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property( Hammer = false )] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property( Hammer = false )] public Vector3 outer_maxs { get; set; } = new( 500 );

	}
	[Library( "snd_soundscape_fader_direction_aabb_jess" ), BoxOrient( "inner_mins", "inner_maxs", 50 ), BoxOrient( "outer_mins", "outer_maxs", 100 )]
	public class SoundScapeTagFaderDirectionalAABB : BaseSoundScapeFader
	{
		[Property( Hammer = false )] public Vector3 inner_mins { get; set; } = new( -1000 );
		[Property( Hammer = false )] public Vector3 inner_maxs { get; set; } = new( 1000 );
		[Property( Hammer = false )] public Vector3 outer_mins { get; set; } = new( -500 );
		[Property( Hammer = false )] public Vector3 outer_maxs { get; set; } = new( 500 );

	}
}
