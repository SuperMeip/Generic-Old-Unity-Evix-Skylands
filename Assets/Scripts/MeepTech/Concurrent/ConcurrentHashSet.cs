using System;
using System.Collections.Generic;
using System.Threading;

namespace Meeptech.Concurrent {
  /// <summary>
  /// A concurrent hash set.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ConcurrentHashSet<T> : IDisposable {
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    private readonly HashSet<T> _hashSet = new HashSet<T>();

    #region Implementation of ICollection<T> ...ish
    /// <summary>
    /// Add item to the hash set.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Add(T item) {
      try {
        _lock.EnterWriteLock();
        return _hashSet.Add(item);
      } finally {
        if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Empty the hash set
    /// </summary>
    public void Clear() {
      try {
        _lock.EnterWriteLock();
        _hashSet.Clear();
      } finally {
        if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Check if the hash set contains a given item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(T item) {
      try {
        _lock.EnterReadLock();
        return _hashSet.Contains(item);
      } finally {
        if (_lock.IsReadLockHeld) _lock.ExitReadLock();
      }
    }

    /// <summary>
    /// Remove an item from the hash set
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(T item) {
      try {
        _lock.EnterWriteLock();
        return _hashSet.Remove(item);
      } finally {
        if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Get the items in the hash set
    /// </summary>
    public int Count {
      get {
        try {
          _lock.EnterReadLock();
          return _hashSet.Count;
        } finally {
          if (_lock.IsReadLockHeld) _lock.ExitReadLock();
        }
      }
    }
    #endregion

    #region Dispose
    public void Dispose() {
      if (_lock != null) _lock.Dispose();
    }
    #endregion
  }
}