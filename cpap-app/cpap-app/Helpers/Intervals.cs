// Copyright 2017 StagPoint Software

namespace StagPoint.Math
{
	using System;
	using System.Collections.Generic;

	internal readonly struct Interval1D : IComparable<Interval1D>
	{
		#region Public fields

		public static readonly Interval1D Empty = new Interval1D( 0f, 0f );

		public readonly float Min;
		public readonly float Max;
		public readonly float Length;

		#endregion

		#region Public properties 

		public float MidPoint
		{
			get => Min + Length * 0.5f;
		}

		#endregion 

		#region Constructor

		public Interval1D( float min, float max )
		{
			this.Min = min;
			this.Max = max;
			this.Length = ( max - min );
		}

		#endregion

		#region Public functions

		public static float GetOverlap( float aMin, float aMax, float bMin, float bMax )
		{
			if( bMin > aMax || aMin > bMax )
			{
				return 0f;
			}

			var minOfMaxes = ( aMax < bMax ) ? aMax : bMax;
			var maxOfMins = ( aMin > bMin ) ? aMin : bMin;

			return ( minOfMaxes - maxOfMins );
		}

		public static Interval1D GetOverlappingInterval( float aMin, float aMax, float bMin, float bMax )
		{
			if( bMin > aMax || aMin > bMax )
			{
				return Interval1D.Empty;
			}

			var minOfMaxes = ( aMax < bMax ) ? aMax : bMax;
			var maxOfMins = ( aMin > bMin ) ? aMin : bMin;

			return new Interval1D( maxOfMins, minOfMaxes );
		}

		public Interval1D GetOverlap( ref Interval1D other )
		{
			if( other.Min > this.Max || this.Min > other.Max )
			{
				return Empty;
			}

			var minOfMaxes = ( this.Max < other.Max ) ? this.Max : other.Max;
			var maxOfMins = ( this.Min > other.Min ) ? this.Min : other.Min;

			return new Interval1D( maxOfMins, minOfMaxes );
		}

		/// <summary>
		/// NOTE: Does not clear the results list before adding any new intervals that result from this call!
		/// </summary>
		public int Subtract( ref Interval1D other, List<Interval1D> results )
		{
			if( other.Min > this.Max || this.Min > other.Max )
			{
				results.Add( this );
				return 1;
			}

			int count = 0;

			if( this.Min < other.Min )
			{
				count++;
				results.Add( new Interval1D( this.Min, Math.Min( this.Max, other.Min ) ) );
			}

			if( this.Max > other.Max )
			{
				count++;
				results.Add( new Interval1D( Math.Max( other.Max, this.Min ), this.Max ) );
			}

			return count;
		}

		#endregion

		#region System.Object overrides

		public override string ToString()
		{
			return $"{Min:F3} -> {Max:F3}";
		}

		#endregion
		
		#region IComparable interface implementation 
		
		public int CompareTo( Interval1D other )
		{
			return this.Min.CompareTo( other.Min );
		}

		#endregion
	}
}
