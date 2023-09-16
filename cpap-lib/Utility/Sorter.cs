using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if ALLOW_UNSAFE

namespace cpaplib
{
	public class Sorter
	{
		private ListEx<float> _sortBuffer;
		private uint[]        _workBuffer;

		public Sorter() { throw new NotImplementedException(); }

		public Sorter( int bufferSize = short.MaxValue )
		{
			_sortBuffer = new ListEx<float>( bufferSize );
			_workBuffer = null;
		}

		public void Clear()
		{
			_sortBuffer.Clear();
		}

		public void AddRange( IList<double> values )
		{
			_sortBuffer.GrowIfNeeded( values.Count );
			
			foreach( var value in values )
			{
				_sortBuffer.Add( (float)value );
			}
		}

		public ListEx<float> Sort()
		{
#if ALLOW_UNSAFE
			if( _workBuffer == null || _workBuffer.Length < _sortBuffer.Count )
			{
				_workBuffer = new uint[ _sortBuffer.Count ];
			}
			
			RadixSort( _sortBuffer.Items, _workBuffer, _sortBuffer.Count );
#else
			_sortBuffer.Sort();
#endif

			return _sortBuffer;
		}

		#region Unsafe functions

#if ALLOW_UNSAFE

		private static unsafe void RadixSort( float[] values, uint[] workBuffer, int length )
		{
			fixed( float* dataPtr = &values[ 0 ] )
			{
				uint* encodedData = (uint*)dataPtr;

				// Convert the values to be sorted into uints that represent the same value and are
				// guaranteed to be sortable
				for( int i = 0; i < length; i++ )
				{
					encodedData[ i ] = encodeFloat( values[ i ] );
				}

				fixed( uint* workPtr = &workBuffer[ 0 ] )
				{
					RadixSort( encodedData, workPtr, length );

					// Convert the values back into floats
					for( int i = 0; i < length; i++ )
					{
						values[ i ] = decodeFloat( encodedData[ i ] );
					}
				}
			}
		}

		private static unsafe void RadixSort( uint* data, uint* work, int length )
		{
			const int KEY_SIZE       = sizeof( float );
			const int OFFSETS_LENGTH = KEY_SIZE * 256;

			uint* offsets = stackalloc uint[ OFFSETS_LENGTH ];
			for( int i = 0; i < OFFSETS_LENGTH; i++ )
			{
				offsets[ i ] = 0x00;
			}

			byte* key = (byte*)data; // Used to isolate individual bytes from the sort key instead of using "shift and mask"

			uint* h0 = offsets;
			uint* h1 = offsets + 256;
			uint* h2 = offsets + 512;
			uint* h3 = offsets + 768;

			// Build histogram by counting the number of occurrences of each possible byte value for each byte in the
			// source key value.
			for( int i = 0; i < length; i++, key += 4 )
			{
				h0[ key[ 0 ] ]++;
				h1[ key[ 1 ] ]++;
				h2[ key[ 2 ] ]++;
				h3[ key[ 3 ] ]++;
			}

			// Convert the histogram into target indices
			{
				uint tsum = 0, sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0;
				for( int i = 0; i < 256; i++ )
				{
					tsum    = h0[ i ] + sum0;
					h0[ i ] = sum0;
					sum0    = tsum;

					tsum    = h1[ i ] + sum1;
					h1[ i ] = sum1;
					sum1    = tsum;

					tsum    = h2[ i ] + sum2;
					h2[ i ] = sum2;
					sum2    = tsum;

					tsum    = h3[ i ] + sum3;
					h3[ i ] = sum3;
					sum3    = tsum;
				}
			}

			// Make one pass through the array for each byte in the key field
			for( int currentByte = 0; currentByte < KEY_SIZE; currentByte++ )
			{
				// The targetIndices variable points to the set of 256 target indices for the current key byte
				uint* targetIndices = offsets + (256 * currentByte);

				key = (byte*)data; // Used to isolate individual bytes from the sort key instead of using "shift and mask"
				uint* currentElement = data;

				// Copy all elements from source to work buffer ordered by current radix value. Because counting sort
				// is a stable sort, this iteratively sorts all keys.
				for( int i = 0; i < length; i++, currentElement++, key += 4 )
				{
					uint index = targetIndices[ key[ currentByte ] ]++;
					work[ index ] = *currentElement;
				}

				// Swap array pointers to avoid need for block-copying data.
				var temp = data;
				data = work;
				work = temp;
			}
		}

		/// <summary>
		/// Encodes a floating point value into a 32-bit unsigned integer value suitable for use as a radix sort key
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static unsafe uint encodeFloat( float value )
		{
			unchecked
			{
				// Canonicalize any possible -0.0f values
				value += 0f;

				uint f    = *(uint*)&value;
				int  temp = -(int)(f >> 31);
				uint mask = (uint)(temp | (1 << 31));

				return f ^ mask;
			}
		}

		/// <summary>
		/// Decodes a previously encoded 32-bit floating point value
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static unsafe float decodeFloat( uint value )
		{
			unchecked
			{
				uint mask   = ((value >> 31) - 1) | (1u << 31);
				var  result = value ^ mask;

				return *(float*)&result;
			}
		}

#endif

		#endregion
	}
}

#endif
