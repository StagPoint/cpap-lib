// Copyright (c) 2016-2020 StagPoint Software

// ReSharper disable InvalidXmlDocComment
namespace cpaplib
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Replacement for the generic List type which has been optimized for game development to reduce memory allocations,
	/// improve performance, and implement object pooling.
	/// </summary>
	public class ListEx<T> : IList<T>, IDisposable
	{
		#region Private instance fields

		private const int DEFAULT_CAPACITY = 1024;

		private T[] _items = Array.Empty<T>();
		private int _count = 0;

		#endregion

		#region Constructor

		public ListEx()
		{
		}

		public ListEx( IList<T> listToClone )
			: this()
		{
			AddRange( listToClone );
		}

		public ListEx( int capacity )
			: this()
		{
			EnsureCapacity( capacity );
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Returns the number of items in the list
		/// </summary>
		public int Count
		{
			get { return _count; }
		}

		/// <summary>
		/// Returns the number of items this list can hold without needing to 
		/// resize the internal array (for internal use only)
		/// </summary>
		internal int Capacity
		{
			get { return _items.Length; }
		}

		/// <summary>
		/// Gets a value indicating whether the list is read-only. Inherited from IList&lt;&gt;
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets/Sets the item at the specified index
		/// </summary>
		public T this[ int index ]
		{
			get
			{
				if( (uint)index > _count - 1 )
					throw new IndexOutOfRangeException();

				return _items[ index ];
			}
			set
			{
				if( (uint)index > _count - 1 )
					throw new IndexOutOfRangeException();

				_items[ index ] = value;
			}
		}

		/// <summary>
		/// Allows direct access to the underlying <see cref="System.Array"/>
		/// containing this list's data. This array will most likely contain more 
		/// elements than the list reports via the <see cref="Count"/> property.
		/// </summary>
		internal T[] Items
		{
			get { return _items; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the ICollection.
		/// </summary>
		public object SyncRoot
		{
			get { return _items.SyncRoot; }
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Returns a shallow copy of this <see cref="ListEx<T>"/> instance 
		/// </summary>
		/// <returns></returns>
		public ListEx<T> Clone()
		{
			var clone = new ListEx<T>( _count );

			Array.Copy( _items, 0, clone._items, 0, _count );

			clone._count = _count;

			return clone;
		}

		/// <summary>
		/// Sorts the elements in the entire <see cref="ListEx" /> using the default comparer.
		/// </summary>
		public void Sort()
		{
			Array.Sort( _items, 0, _count, null );
		}

		/// <summary>
		/// Sorts the elements in the entire <see cref="ListEx" /> using the specified comparer.
		/// </summary>
		public void Sort( IComparer<T> comparer )
		{
			Array.Sort( _items, 0, _count, comparer );
		}

		/// <summary>
		/// Ensures that the <see cref="ListEx" /> has enough capacity to store <paramref name="Size"/> elements
		/// </summary>
		/// <param name="Size">The total number of items that the list must be able to contain</param>
		public void EnsureCapacity( int Size )
		{
			if( _items.Length == 0 )
			{
				Array.Resize<T>( ref _items, Size );
				return;
			}

			if( _items.Length <= Size )
			{
				var newSize = ( Size / DEFAULT_CAPACITY ) * DEFAULT_CAPACITY + DEFAULT_CAPACITY;
				Array.Resize<T>( ref _items, newSize );
			}
		}

		/// <summary>
		/// Will ensure that the list's Capacity is sufficient to hold the indicated number of additional elements
		/// </summary>
		/// <param name="count">The number of elements that will be added</param>
		public void GrowIfNeeded( int count )
		{
			EnsureCapacity( _count + count );
		}

		/// <summary>
		/// Adds the elements of the specified collection to the end of the <see cref="ListEx"/>
		/// </summary>
		public void AddRange( ListEx<T> list )
		{
			var listCount = list._count;

			EnsureCapacity( _count + listCount );
			Array.Copy( list._items, 0, _items, _count, listCount );
			_count += listCount;
		}

		/// <summary>
		/// Adds the elements of the specified collection to the end of the <see cref="ListEx"/>
		/// </summary>
		public void AddRange( IList<T> list )
		{
			var listCount = list.Count;

			EnsureCapacity( _count + listCount );

			for( int i = 0; i < listCount; i++ )
			{
				_items[ _count++ ] = list[ i ];
			}
		}

		/// <summary>
		/// Adds the elements of the specified collection to the end of the <see cref="ListEx"/>
		/// </summary>
		public void AddRange( T[] list )
		{
			var listLength = list.Length;

			EnsureCapacity( _count + listLength );
			Array.Copy( list, 0, _items, _count, listLength );
			_count += listLength;
		}

		/// <summary>
		/// Determines the index of a specific item in the collection
		/// </summary>
		public int IndexOf( T item )
		{
			return Array.IndexOf<T>( _items, item, 0, _count );
		}

		/// <summary>
		/// Inserts an item to the collection at the specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		public void Insert( int index, T item )
		{
			EnsureCapacity( _count + 1 );

			if( index < _count )
			{
				Array.Copy( _items, index, _items, index + 1, _count - index );
			}

			_items[ index ] = item;
			_count += 1;
		}

		/// <summary>
		/// Inserts an array of items at the specified index
		/// </summary>
		public void InsertRange( int index, T[] array )
		{
			if( array == null )
				throw new ArgumentNullException( "items" );

			if( index < 0 || index > _count )
				throw new ArgumentOutOfRangeException( "index" );

			EnsureCapacity( _count + array.Length );

			if( index < _count )
			{
				Array.Copy( _items, index, _items, index + array.Length, _count - index );
			}

			array.CopyTo( _items, index );

			_count += array.Length;
		}

		/// <summary>
		/// Inserts a collection of items at the specified index
		/// </summary>
		public void InsertRange( int index, ListEx<T> list )
		{
			if( list == null )
				throw new ArgumentNullException( "items" );

			if( index < 0 || index > _count )
				throw new ArgumentOutOfRangeException( "index" );

			EnsureCapacity( _count + list._count );

			if( index < _count )
			{
				Array.Copy( _items, index, _items, index + list._count, _count - index );
			}

			Array.Copy( list._items, 0, _items, index, list._count );

			_count += list._count;
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the collection
		/// </summary>
		public bool Remove( T item )
		{
			var index = IndexOf( item );
			if( index == -1 )
				return false;

			RemoveAt( index );

			return true;
		}

		/// <summary>
		/// Removes all items matching the predicate condition from the list
		/// </summary>
		public void RemoveAll( Predicate<T> predicate )
		{
			var index = 0;
			while( index < _count )
			{
				if( predicate( _items[ index ] ) )
				{
					RemoveAt( index );
				}
				else
				{
					index += 1;
				}
			}
		}

		/// <summary>
		/// Removes the item at the specified index
		/// </summary>
		public void RemoveAt( int index )
		{
			if( index >= _count )
			{
				throw new ArgumentOutOfRangeException();
			}

			_count -= 1;

			if( index < _count )
			{
				Array.Copy( _items, index + 1, _items, index, _count - index );
			}

			_items[ _count ] = default( T );
		}

		/// <summary>
		/// Adds an item to the collection
		/// </summary>
		public void Add( T item )
		{
			EnsureCapacity( _count + 1 );
			_items[ _count++ ] = item;
		}

		/// <summary>
		/// Adds two items to the collection (more efficient than using params keyword, using AddRane(), or calling Add multiple times)
		/// </summary>
		public void Add( T item0, T item1 )
		{
			EnsureCapacity( _count + 2 );
			_items[ _count++ ] = item0;
			_items[ _count++ ] = item1;
		}

		/// <summary>
		/// Adds three items to the collection (more efficient than using params keyword, using AddRane(), or calling Add multiple times)
		/// </summary>
		public void Add( T item0, T item1, T item2 )
		{
			EnsureCapacity( _count + 3 );
			_items[ _count++ ] = item0;
			_items[ _count++ ] = item1;
			_items[ _count++ ] = item2;
		}

		/// <summary>
		/// Removes all items from the collection
		/// </summary>
		public void Clear()
		{
			// http://manski.net/2012/12/net-array-clear-vs-arrayx-0-performance/
			if( _count >= 100 )
			{
				Array.Clear( _items, 0, _count );
			}
			else
			{
				var nullValueForType = default( T );
				for( int i = 0; i < _count; i++ )
				{
					_items[ i ] = nullValueForType;
				}
			}

			_count = 0;
		}

		/// <summary>
		/// Resizes the internal buffer to exactly match the number of elements in the collection
		/// </summary>
		public void TrimExcess()
		{
			Array.Resize( ref _items, _count );
		}

		/// <summary>
		/// Determines whether the collection contains the specified value
		/// </summary>
		public bool Contains( T item )
		{
			if( item == null )
			{
				for( int i = 0; i < _count; i++ )
				{
					if( _items[ i ] == null )
					{
						return true;
					}
				}
				return false;
			}

			EqualityComparer<T> comparer = EqualityComparer<T>.Default;

			for( int j = 0; j < _count; j++ )
			{
				if( comparer.Equals( _items[ j ], item ) )
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Copies the elements of the collection to a <see cref="System.Array"/> instance
		/// </summary>
		/// <param name="array"></param>
		public void CopyTo( T[] array )
		{
			CopyTo( array, 0 );
		}

		/// <summary>
		/// Copies the elements of the collection to an <see cref="System.Array"/> starting at the specified index
		/// </summary>
		public void CopyTo( T[] array, int arrayIndex )
		{
			Array.Copy( _items, 0, array, arrayIndex, _count );
		}

		/// <summary>
		/// Copies the elements of the collection to an <see cref="System.Array"/>
		/// </summary>
		/// <param name="sourceIndex">The starting position in the collection</param>
		/// <param name="dest">The destination array</param>
		/// <param name="destIndex">The position in the array to start copying to</param>
		/// <param name="length">How many elements to copy</param>
		public void CopyTo( int sourceIndex, T[] dest, int destIndex, int length )
		{
			if( sourceIndex + length > _count )
				throw new IndexOutOfRangeException( "sourceIndex" );

			if( dest == null )
				throw new ArgumentNullException( "dest" );

			if( destIndex + length > dest.Length )
				throw new IndexOutOfRangeException( "destIndex" );

			Array.Copy( _items, sourceIndex, dest, destIndex, length );
		}

		/// <summary>
		/// Returns a List&lt;T&gt; collection containing all elements of this collection
		/// </summary>
		/// <returns></returns>
		public List<T> ToList()
		{
			var list = new List<T>( _count );
			list.AddRange( this.ToArray() );
			return list;
		}

		/// <summary>
		/// Returns an array containing all elements of this collection
		/// </summary>
		public T[] ToArray()
		{
			var array = new T[ _count ];

			Array.Copy( _items, 0, array, 0, _count );

			return array;
		}

		/// <summary>
		/// Returns a subset of the collection's items as an array
		/// </summary>
		public T[] ToArray( int index, int length )
		{
			var array = new T[ _count ];

			if( _count > 0 )
			{
				CopyTo( index, array, 0, length );
			}

			return array;
		}

		#endregion

		#region IEnumerable<T> implementation

		/// <summary>
		/// Returns an allocation-free enumerator that can be used in foreach loops without extra allocations or boxing of value types
		/// </summary>
		/// <returns></returns>
		public ListExEnumerator<T> GetEnumeratorEx()
		{
			return new ListExEnumerator<T>( _items, _count );
		}

		// NOTE: The IEnumerable<T> implementation here is horribly broken on iOS, and until
		// I can figure out a way to implement typed enumerators that do work on iOS, please
		// use a for(;;) loop instead of foreach(). Note that this may also apply to using
		// LINQ queries, which may use foreach() or an GetEnumerator() internally.

		/// <summary>
		/// Returns an IEnumerator instance that can be used to iterate through
		/// the elements in this list.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return ListExEnumerator.Obtain( this, null );
		}

		/// <summary>
		/// Returns an IEnumerator instance that can be used to iterate through
		/// the elements in this list.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ListExEnumerator.Obtain( this, null );
		}

		#endregion

		#region IDisposable implementation

		/// <summary>
		/// Clears the list memory and releases the list back to the object pool.
		/// </summary>
		public void Dispose()
		{
			_items = null;
		}

		#endregion

		#region System.Object overrides

		public override string ToString()
		{
			return string.Format( "ListEx<{0}>( {1}, {2} )", typeof( T ).Name, this.Count, this.Capacity );
		}

		#endregion

		#region Nested classes

		public struct ListExEnumerator<TData>
		{
			private TData[] m_data;
			private int m_count;
			private int m_index;

			public ListExEnumerator( TData[] data, int count )
			{
				m_data = data;
				m_count = count;
				m_index = -1;
			}

			public TData Current
			{
				get { return m_data[ m_index ]; }
			}

			public bool MoveNext()
			{
				return ++m_index < m_count;
			}

			public ListExEnumerator<TData> GetEnumerator()
			{
				return this;
			}
		}

		private class ListExEnumerator : IEnumerator<T>, IEnumerable<T>
		{
			#region Private variables

			private ListEx<T> list;
			private Func<T, bool> predicate;
			private int currentIndex;
			private T currentValue;
			private bool isValid = false;

			#endregion

			#region Pooling

			public static ListExEnumerator Obtain( ListEx<T> list, Func<T, bool> predicate )
			{
				var enumerator = new ListExEnumerator();
				enumerator.ResetInternal( list, predicate );

				return enumerator;
			}

			#endregion

			#region IEnumerator<T> Members

			public T Current
			{
				get
				{
					if( !this.isValid )
						throw new InvalidOperationException( "The enumerator is no longer valid" );

					return this.currentValue;
				}
			}

			#endregion

			#region Private utility methods

			private void ResetInternal( ListEx<T> list, Func<T, bool> predicate )
			{
				this.isValid = true;
				this.list = list;
				this.predicate = predicate;
				this.currentIndex = 0;
				this.currentValue = default( T );
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
			}

			#endregion

			#region IEnumerator Members

			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			public bool MoveNext()
			{
				if( !this.isValid )
				{
					throw new InvalidOperationException( "The enumerator is no longer valid" );
				}

				while( this.currentIndex < this.list.Count )
				{
					var valueAtIndex = this.list[ currentIndex++ ];
					if( predicate != null )
					{
						if( !predicate( valueAtIndex ) )
							continue;
					}

					this.currentValue = valueAtIndex;
					return true;
				}

				this.currentValue = default( T );
				return false;
			}

			public void Reset()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			public IEnumerator<T> GetEnumerator()
			{
				return this;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this;
			}

			#endregion
		}

		private class FunctorComparer : IComparer<T>, IDisposable
		{
			#region Static variables

			private static Queue<FunctorComparer> pool = new Queue<FunctorComparer>();

			#endregion

			#region Private instance variables

			private Comparison<T> comparison;

			#endregion

			#region Object pooling

			public static FunctorComparer Obtain( Comparison<T> comparison )
			{
				var comparer = ( pool.Count > 0 ) ? pool.Dequeue() : new FunctorComparer();
				comparer.comparison = comparison;
				return comparer;
			}

			public void Release()
			{
				this.comparison = null;

				if( !pool.Contains( this ) )
				{
					pool.Enqueue( this );
				}
			}

			#endregion

			#region IComparer<T> implementation

			public int Compare( T x, T y )
			{
				return this.comparison( x, y );
			}

			#endregion

			#region IDisposable implementation

			public void Dispose()
			{
				this.Release();
			}

			#endregion
		}

		#endregion
	}

	public static class ListExtensions
	{
		/// <summary>
		/// Ensures that the list contains the internal capacity to add the indicated number of elements.
		/// </summary>
		/// <param name="capacity">The number of elements that will be added.</param>
		public static void EnsureCapacity<T>( this List<T> list, int capacity )
		{
			if( capacity > list.Capacity )
			{
				list.Capacity = capacity;
			}
		}

		/// <summary>
		/// Determines whether the collection contains the specified reference by checking object references only,
		/// which can be faster than checking for equality using an EqualityComparer, but only works for reference
		/// types. If this collection contains a value type, an exception will be thrown.
		/// </summary>
		public static bool ContainsReference<T>( this List<T> list, T item ) where T : class
		{
			var count = list.Count;

			for( int i = 0; i < count; i++ )
			{
				if( object.ReferenceEquals( list[ i ], item ) )
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the collection. This function is faster (in some
		/// cases *WAY* faster) than Remove(), but will not retain the order of the elements contained in the list.
		/// </summary>
		public static bool FastRemove<T>( this List<T> list, T item )
		{
			var index = list.IndexOf( item );
			if( index == -1 )
				return false;

			if( index < list.Count - 1 )
			{
				list[ index ] = list[ list.Count - 1 ];
			}

			list.RemoveAt( list.Count - 1 );

			return true;
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the collection. This function is faster (in some
		/// cases *WAY* faster) than Remove(), but will not retain the order of the elements contained in the list.
		/// This function searches for the item using object reference comparisons only, which can be faster than 
		/// checking for equality using an EqualityComparer, but only works for reference types.
		/// </summary>
		public static bool FastRemoveReference<T>( this List<T> list, T item ) where T : class
		{
			var index = IndexOfReference( list, item );
			if( index == -1 )
				return false;

			var maxIndex = list.Count - 1;

			if( index < maxIndex )
			{
				list[ index ] = list[ maxIndex ];
			}

			list.RemoveAt( maxIndex );

			return true;
		}

		/// <summary>
		/// Removes an object by searching only for the specified object reference, which can be faster 
		/// than checking for equality using an EqualityComparer, but only works for reference
		/// types. 
		/// </summary>
		public static bool RemoveReference<T>( this List<T> list, T obj ) where T : class
		{
			var index = IndexOfReference( list, obj );
			if( index == -1 )
				return false;

			list.RemoveAt( index );

			return true;
		}

		/// <summary>
		/// Determines the index of a specific item in the collection by checking object references only, which
		/// can be faster than checking for equality using an EqualityComparer, but only works for reference
		/// types.
		/// </summary>
		public static int IndexOfReference<T>( this List<T> list, object obj )
		{
			var count = list.Count;
			for( int i = 0; i < count; i++ )
			{
				if( object.ReferenceEquals( obj, list[ i ] ) )
				{
					return i;
				}
			}

			return -1;
		}
	}
}
