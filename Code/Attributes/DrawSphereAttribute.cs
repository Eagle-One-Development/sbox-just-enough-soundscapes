using System;
using System.Collections.Generic;
using System.Text;
using Sandbox;

namespace Hammer
{
	public class DrawSphereAttribute : MetaDataAttribute
	{
		internal string RadiusKV;
		public DrawSphereAttribute()
		{
		}

		public DrawSphereAttribute( string RadiusKV )
		{
			this.RadiusKV = RadiusKV;
		}

		public override void AddHeader( StringBuilder sb )
		{
			if ( !string.IsNullOrEmpty( RadiusKV ) )
				sb.Append( $"sphere({RadiusKV.ToLower()}) " );
		}
	}
}
