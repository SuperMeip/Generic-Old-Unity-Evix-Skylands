using MeepTech.GamingBasics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MeepTech.Jobs {

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  public abstract class QueueManagerJob2<QueueItemType> : ThreadedJob
    where QueueItemType : IComparable<QueueItemType> {

    /// <summary>
    /// The default maximum child job count
    /// </summary>
    public const int DefaultMaxChildJobCount = 20;

    /// <summary>
    /// The currently queued up job count
    /// </summary>
    public int queueCount {
      get => queue.Count();
    }

    /// <summary>
    /// The queue this job is managing
    /// </summary>
    protected List<QueueItemType> queue;

    /// <summary>
    /// Items added to the queue externally
    /// </summary>
    protected ConcurrentBag<QueueItemType> newlyAddedQueueItems;

    /// <summary>
    /// If an item has been canceled, we just skip it when the queue runs.
    /// </summary>
    protected ConcurrentDictionary<QueueItemType, bool> canceledItems;

    /// <summary>
    /// Used for reporting what items this job manager is currently running on
    /// </summary>
    ConcurrentDictionary<QueueItemType, IThreadedJob> runningChildJobs;

    /// <summary>
    /// (Optional) a bottleneck for queue items to process at once
    /// </summary>
    int queueItemsToProcessBottleneck = 0;

    /// <summary>
    /// The max number of child jobs allowed
    /// </summary>
    int maxChildJobsCount;

    /// <summary>
    /// The number of running jobs
    /// </summary>
    int runningJobCount;

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    protected QueueManagerJob2(int maxChildJobsCount = DefaultMaxChildJobCount, int queueBottleneck = 0) {
      runningJobCount = 0;
      this.maxChildJobsCount = maxChildJobsCount;
      queueItemsToProcessBottleneck = queueBottleneck;
      queue = new List<QueueItemType>();
      newlyAddedQueueItems = new ConcurrentBag<QueueItemType>();
      canceledItems = new ConcurrentDictionary<QueueItemType, bool>();
      runningChildJobs = new ConcurrentDictionary<QueueItemType, IThreadedJob>();
    }

    /// <summary>
    /// Add a bunch of objects to the queue for processing
    /// </summary>
    /// <param name="queueObjects"></param>
    /// <param name="sortQueue">whether or not to sort the queue on add.</param>
    public void enqueue(QueueItemType[] queueObjects) {
      foreach (QueueItemType queueObject in queueObjects) {
        // if the chunk has already been canceled, don't requeue it right now
        if (!(canceledItems.TryGetValue(queueObject, out bool hasBeenCanceled) && hasBeenCanceled)) {
          newlyAddedQueueItems.Add(queueObject);
        }
      }

      // if the queue manager job isn't running, start it
      if (!isRunning) {
        start();
      }
    }

    /// <summary>
    /// if there's any child jobs running for the given ojects, stop them and dequeue
    /// </summary>
    /// <param name="queueObject"></param>
    /// <param name="sortQueue">whether or not to sort the queue on add.</param>
    public void dequeue(QueueItemType[] queueObjects) {
      if (isRunning) {
        foreach (QueueItemType queueObject in queueObjects) {
          if (queue.Contains(queueObject)) {
            canceledItems.TryAdd(queueObject, true);
          }
        }
      }
    }

    /// <summary>
    /// Get all the queue items in an array
    /// </summary>
    /// <returns></returns>
    public QueueItemType[] getAllQueuedItems() {
      return queue.ToArray();
    }

    /// <summary>
    /// Get all the items this job manager is currently running on
    /// </summary>
    /// <returns></returns>
    public QueueItemType[] getAllItemsWithRunningJobs() {
      return runningChildJobs.Keys.ToArray();
    }

    /// <summary>
    /// Get the type of job we're managing in this queue
    /// </summary>
    /// <returns></returns>
    protected abstract IThreadedJob getChildJob(QueueItemType queueObject);

    /// <summary>
    /// validate queue items
    /// </summary>
    /// <param name="queueItem"></param>
    /// <returns></returns>
    protected virtual bool isAValidQueueItem(QueueItemType queueItem) {
      return true;
    }

    /// <summary>
    /// if the queue item is ready to go, or should be put back in the queue
    /// </summary>
    /// <param name="queueItem"></param>
    /// <returns></returns>
    protected virtual bool itemIsReady(QueueItemType queueItem) {
      return true;
    }

    /// <summary>
    /// Do something when we find the queue item to be invalid before removing it
    /// </summary>
    protected virtual void onQueueItemInvalid(QueueItemType queueItem) { }

    /// <summary>
    /// Sort the queue after each run?
    /// </summary>
    protected virtual void sortQueue() { }

    /// <summary>
    /// Do something when new items are added to the queue
    /// </summary>
    protected virtual void onNewItemsQueued() {
      sortQueue();
    }

    /// <summary>
    /// The threaded function to run
    /// </summary>
    protected override void jobFunction() {
      while (newlyAddedQueueItems.Count > 0 || queue.Count > 0) {
        queueNewlyAddedItems();
        int queueItemsProcessed = -1;
        queue.RemoveAll(queueItem => {
          // incrememnt the bottleneck checker
          queueItemsProcessed++;
          if (queueItemsToProcessBottleneck > 0 && queueItemsProcessed >= queueItemsToProcessBottleneck) {
            return false;
          }

          // if the item has been canceled. Remove it.
          if (itemIsCanceled(queueItem)) {
            // World.Debugger.log($"{threadName} canceled {queueItem}");
            return true;
          }

          // if the item is just invalid, remove it
          if (!isAValidQueueItem(queueItem)) {
            onQueueItemInvalid(queueItem);
            return true;
          }

          // if we have space, pop off the top of the queue and run it as a job.
          if (runningJobCount < maxChildJobsCount && itemIsReady(queueItem)) {
            runningJobCount++;
            IThreadedJob childJob = getChildJob(queueItem);
            runningChildJobs.TryAdd(queueItem, childJob);
            childJob.start();
            // World.Debugger.log($"{threadName} has started job for {queueItem}");
            return true;
          }

          return false;
        });
      }
    }

    /// <summary>
    /// De-increment how many jobs are running when one finishes.
    /// </summary>
    internal virtual void onJobComplete(QueueItemType queueItem) {
      runningChildJobs.TryRemove(queueItem, out _);
      runningJobCount--;
    }

    /// <summary>
    /// Attempt to queue newly items from the bag
    /// </summary>
    void queueNewlyAddedItems() {
      int itemsQueued = 0;
      // get the # of assigned controllers at this moment in the bag.
      int newlyQueuedItemCount = newlyAddedQueueItems.Count;
      // we'll try to take that many items out this loop around.
      while (0 < newlyQueuedItemCount--) {
        if (newlyAddedQueueItems.TryTake(out QueueItemType newQueueItem)
          && !queue.Contains(newQueueItem)
        ) {
          itemsQueued++;
          queue.Add(newQueueItem);
          // World.Debugger.log($"{threadName} has added {newQueueItem} to queue");
        }
      }

      onNewItemsQueued();
    }

    /// <summary>
    /// Check if the queue item has been canceled.
    /// </summary>
    /// <param name="queueItem"></param>
    /// <returns></returns>
    bool itemIsCanceled(QueueItemType queueItem) {
      if (canceledItems.TryGetValue(queueItem, out bool isCanceled)) {
        // if it has a cancelation token stored, and that token is true, lets try to switch it off, and then equeue the current item from the queue.
        if (isCanceled && canceledItems.TryUpdate(queueItem, false, true)) {
          canceledItems.TryRemove(queueItem, out _);
          return true;
        }

        // if there's a cancelation token for this item, but it's set to false, we can just remove it.
        if (!isCanceled) {
          canceledItems.TryRemove(queueItem, out _);
        }
      }

      return false;
    }

    /// <summary>
    /// Child job for doing work on objects in the queue
    /// </summary>
    protected abstract class QueueTaskChildJob<ParentQueueManagerType, ParentQueueItemType> : ThreadedJob
      where ParentQueueManagerType : QueueManagerJob2<ParentQueueItemType>
      where ParentQueueItemType : IComparable<ParentQueueItemType> {

      /// <summary>
      /// The queue item this job will do work on
      /// </summary>
      protected ParentQueueItemType queueItem;

      /// <summary>
      /// The cancelation sources for waiting jobs
      /// </summary>
      protected ParentQueueManagerType jobManager;

      /// <summary>
      /// Constructor
      /// </summary>
      protected QueueTaskChildJob(ParentQueueItemType queueItem, ParentQueueManagerType jobManager) {
        this.queueItem = queueItem;
        this.jobManager = jobManager;
      }

      /// <summary>
      /// The do work function
      /// </summary>
      /// <param name="queueItem"></param>
      /// <param name="cancellationToken"></param>
      protected abstract void doWork(ParentQueueItemType queueItem);

      /// <summary>
      /// Threaded function
      /// </summary>
      protected override void jobFunction() {
        doWork(queueItem);
      }

      /// <summary>
      /// On done, set the space free in the parent job
      /// </summary>
      protected override void finallyDo() {
        jobManager.onJobComplete(queueItem);
      }
    }
  }
}