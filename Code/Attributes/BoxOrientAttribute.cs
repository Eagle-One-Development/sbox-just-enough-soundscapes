using System;
using System.Collections.Generic;
using System.Text;
using Sandbox;

namespace Hammer
{
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
	public class BoxOrientAttribute : MetaDataAttribute
	{
		internal string mins;
		internal string maxs;
		internal int defaultValue;

		public BoxOrientAttribute( string mins, string maxs, int defaultValue = 100 )
		{
			this.mins = mins;
			this.maxs = maxs;
			this.defaultValue = defaultValue;
		}

		public override void AddBody( StringBuilder sb )
		{
			sb.AppendLine( $"	{mins}(vector) : \"{mins.Replace( " ", "_" ).Replace( "\t", "_" ).ToTitleCase()}\" :  \"{-defaultValue} {-defaultValue} {-defaultValue}\" :" );
			sb.AppendLine( $"	{maxs}(vector) : \"{maxs.Replace( " ", "_" ).Replace( "\t", "_" ).ToTitleCase()}\" :  \"{defaultValue } {defaultValue } {defaultValue }\" :" );
		}

		public override void AddHeader( StringBuilder sb )
		{
			sb.Append( $"box_oriented({mins}, {maxs}) " );
		}
	}
}
