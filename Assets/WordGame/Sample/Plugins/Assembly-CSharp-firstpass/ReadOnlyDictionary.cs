using System;
using System.Collections;
using System.Collections.Generic;

public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	private readonly IDictionary<TKey, TValue> _dictionary;

	public ICollection<TKey> Keys => null;

	public ICollection<TValue> Values => null;

	public TValue this[TKey key] => default(TValue);

	TValue IDictionary<TKey, TValue>.this[TKey key]
	{
		get
		{
			return default(TValue);
		}
		set
		{
		}
	}

	public int Count => 0;

	public bool IsReadOnly => false;

	public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
	{
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
	}

	public bool ContainsKey(TKey key)
	{
		return false;
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		return false;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		value = default(TValue);
		return false;
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return false;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		return false;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return null;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return null;
	}

	private static Exception ReadOnlyException()
	{
		return null;
	}
}
