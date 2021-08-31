using System;
using System.Threading.Tasks;

namespace Sandbox
{
	public static class SoundExtensions
	{
		public static async Task Stop( this Sound sound, float seconds, int steps = 100 )
		{
			for ( int i = 0; i < steps; i++ )
			{
				sound.SetVolume( 1 - (i * 1 / (float)steps) );
				await GameTask.DelaySeconds( seconds / steps );
			}
			sound.Stop();
		}
	}
}
