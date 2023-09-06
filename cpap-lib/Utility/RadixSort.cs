using System.Runtime.CompilerServices;

#if ALLOW_UNSAFE

namespace cpaplib
{
	public static unsafe class RadixSort
	{
		public static void Sort( ListEx<float> samples )
		{
			const int KEY_SIZE       = sizeof( float );
			const int OFFSETS_LENGTH = KEY_SIZE * 256;

			int    length = samples.Count;
			uint[] buffer = new uint[ length ];
			
			fixed (float* dataPtr = &samples.Items[0] )
			{
				uint* data = (uint*)dataPtr;
				
				// Encode the floats so that they can be correctly sorted as unsigned integers 
				float* current = dataPtr;
				for( int i = 0; i < length; i++, current++ )
				{
					data[ i ] = encodeFloat( *current );
				}
				
				fixed( uint* workPtr = &buffer[ 0 ] )
				{
					uint* work = workPtr;

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
				
				// Decode the sorted uints back into floats 
				current = dataPtr;
				for( int i = 0; i < length; i++ )
				{
					*current++ = decodeFloat( data[ i ] );
				}
			}
		}
		
		/// <summary>
		/// Encodes a floating point value into a 32-bit unsigned integer value suitable for use as a radix sort key
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static uint encodeFloat( float value )
		{
			unchecked
			{
				// Canonicalize any possible -0.0f values
				value += 0f;

				uint f    = *(uint*)&value;
				int  temp = -(int)( f >> 31 );
				uint mask = (uint)( temp | ( 1 << 31 ) );

				return f ^ mask;
			}
		}

		/// <summary>
		/// Decodes a previously encoded 32-bit floating point value
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static float decodeFloat( uint value )
		{
			unchecked
			{
				uint mask   = ( ( value >> 31 ) - 1 ) | ( 1u << 31 );
				var  result = value ^ mask;

				return *(float*)&result;
			}
		}
	}
}

#endif