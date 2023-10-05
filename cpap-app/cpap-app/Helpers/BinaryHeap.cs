// Copyright 2011-2023 StagPoint Software
using System;
using System.Numerics;

namespace cpap_app.Helpers;

internal class BinaryHeap<T> where T : struct, INumber<T>
{
	#region Public properties

	/// <summary>
	/// Returns the number of values stored in the heap
	/// </summary>
	public int Count
	{
		get => _count;
	}

	/// <summary>
	/// Returns the current internal storage capacity of the BinaryHeap
	/// </summary>
	public int Capacity
	{
		get => _capacity;
	}

	/// <summary>
	/// Returns true if the heap is empty
	/// </summary>
	public bool IsEmpty
	{
		get => _count == 0;
	}

	#endregion

	#region Private data

	private const int GROWTH_FACTOR = 2;

	private T[] _items;
	private int _count;
	private int _capacity;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the BinaryHeap class with a the indicated capacity
	/// </summary>
	public BinaryHeap( int capacity = 1024 )
	{
		if( capacity < 1 )
		{
			throw new ArgumentException( "Capacity must be greater than zero" );
		}

		_count    = 0;
		_capacity = capacity;
		_items    = new T[ capacity ];
	}

	#endregion

	#region Public methods

	/// <summary>
	/// Removes all items from the heap.
	/// </summary>
	public void Clear()
	{
		this._count = 0;
		Array.Clear( _items, 0, _items.Length );
	}

	/// <summary>
	/// Returns the first item in the heap without removing it from the heap
	/// </summary>
	/// <returns></returns>
	public T Peek()
	{
		if( this._count == 0 )
		{
			throw new InvalidOperationException( "Cannot peek at first item when the heap is empty." );
		}

		return _items[ 0 ];
	}

	/// <summary>
	/// Adds a key and value to the heap.
	/// </summary>
	/// <param name="item">The item to add to the heap.</param>
	public void Enqueue( T item )
	{
		if( _count == _capacity )
		{
			EnsureCapacity( _count + 1 );
		}

		_items[ _count ] = item;

		heapifyUp( _count, item );

		_count++;
	}

	/// <summary>
	/// Removes and returns the first item in the heap.
	/// </summary>
	/// <returns>The first value in the heap.</returns>
	public T Dequeue()
	{
		if( this._count == 0 )
		{
			throw new InvalidOperationException( "Cannot remove item from an empty heap" );
		}

		// Stores the key of root node to be returned
		var v = _items[ 0 ];

		// Decrease heap size by 1
		_count -= 1;

		// Copy the last node to the root node and clear the last node
		_items[ 0 ]      = _items[ _count ];
		_items[ _count ] = default;

		// Restore the heap property of the tree
		heapifyDown( 0, _items[ 0 ] );

		return v;
	}

	/// <summary>
	/// Ensures that there is large enough internal capacity to store the indicated number of
	/// items without having to re-allocate the internal buffer.
	/// </summary>
	/// <param name="count"></param>
	public void EnsureCapacity( int count )
	{
		while( count > this._capacity )
		{
			_capacity *= GROWTH_FACTOR;
		}

		Array.Resize( ref _items, _capacity );
	}

	#endregion

	#region Private utility methods

	private int heapifyUp( int index, T item )
	{
		var parent = (index - 1) >> 1;

		while( parent > -1 && item < _items[ parent ] )
		{
			// Swap nodes
			_items[ index ] = _items[ parent ];

			index  = parent;
			parent = (index - 1) >> 1;
		}

		_items[ index ] = item;

		return index;
	}

	private int heapifyDown( int parent, T item )
	{
		while( true )
		{
			var index = 0;

			int ch1 = (parent << 1) + 1;
			if( ch1 >= _count )
				break;

			int ch2 = (parent << 1) + 2;
			if( ch2 >= _count )
			{
				index = ch1;
			}
			else
			{
				index = (_items[ ch1 ] < _items[ ch2 ]) ? ch1 : ch2;
			}

			if( item < _items[ index ] )
				break;

			_items[ parent ] = _items[ index ]; // Swap nodes

			parent = index;
		}

		_items[ parent ] = item;

		return parent;
	}

	#endregion

	#region Debugging support

	public override string ToString()
	{
		return string.Format( "Count={0}", _count );
	}

	#endregion
}
