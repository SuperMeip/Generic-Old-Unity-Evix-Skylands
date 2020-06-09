using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MeepTech.Jobs {

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  public abstract class QueueManagerJob<QueueItemType> : ThreadedJob 
    where QueueItemType : IComparable<QueueItemType> {

    /// <summary>
    /// Child job for doing work on objects in the queue
    /// </summary>
    protected abstract class QueueTaskChildJob<ParentQueueItemType> : ThreadedJob
      where ParentQueueItemType : IComparable<ParentQueueItemType> {

      /// <summary>
      /// The queue item this job will do work on
      /// </summary>
      protected ParentQueueItemType queueItem;

      /// <summary>
      /// The cancelation sources for waiting jobs
      /// </summary>
      protected QueueManagerJob<ParentQueueItemType> jobManager;

      /// <summary>
      /// Constructor
      /// </summary>
      protected QueueTaskChildJob(ParentQueueItemType queueItem, QueueManagerJob<ParentQueueItemType> jobManager) {
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
        jobManager.runningJobCount--;
      }
    }

    /// <summary>
    /// The currently queued up job count
    /// </summary>
    public int queueCount {
      get => queue.Count();
    }
    
    /// <summary>
    /// The queue this job is managing
    /// </summary>
    protected ConcurrentQueue<QueueItemType> queue;

    /// <summary>
    /// If an item has been canceled, we just skip it when the queue runs.
    /// </summary>
    protected ConcurrentDictionary<QueueItemType, bool> canceledItems;

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
    protected QueueManagerJob(int maxChildJobsCount = 25) {
      runningJobCount = 0;
      this.maxChildJobsCount = maxChildJobsCount;
      queue = new ConcurrentQueue<QueueItemType>();
      canceledItems = new ConcurrentDictionary<QueueItemType, bool>();
    }

    /// <summary>
    /// Get the type of job we're managing in this queue
    /// </summary>
    /// <returns></returns>
    protected abstract QueueTaskChildJob<QueueItemType> getChildJob(QueueItemType queueObject);

    /// <summary>
    /// Add a bunch of objects to the queue for processing
    /// </summary>
    /// <param name="queueObjects"></param>
    /// <param name="sortQueue">whether or not to sort the queue on add.</param>
    public void enQueue(QueueItemType[] queueObjects, bool sortQueue = true) {
      foreach (QueueItemType queueObject in queueObjects) {
        // if the chunk has already been canceled, don't requeue it right now
        if (!(canceledItems.TryGetValue(queueObject, out bool hasBeenCanceled) && hasBeenCanceled)
          && !queue.Contains(queueObject)) {
          queue.Enqueue(queueObject);
        }
      }

      if (sortQueue) {
        this.sortQueue();
      }
      // if the queue manager job isn't running, start it
      if (!isRunning) {
        try {
          start();
        // if two items were enqueued quick sometimes it can try to start the same threat twice
        } catch (System.Threading.ThreadStateException) {
          return;
        }
      }
    }

    /// <summary>
    /// if there's any child jobs running for the given ojects, stop them and dequeue
    /// </summary>
    /// <param name="queueObject"></param>
    /// <param name="sortQueue">whether or not to sort the queue on add.</param>
    public void deQueue(QueueItemType[] queueObjects, bool sortQueue = true) {
      if (isRunning) {
        foreach (QueueItemType queueObject in queueObjects) {
          if (queue.Contains(queueObject)) {
            canceledItems.TryAdd(queueObject, true);
          }
        }

        if (sortQueue) {
          this.sortQueue();
        }
      }
    }

    /// <summary>
    /// The threaded function to run
    /// </summary>
    protected override void jobFunction() {
      // run while we have a queue
      QueueItemType queueItem;
      while (queue.Count() > 0) {
        if (queue.TryPeek(out queueItem)) {
          /// validate the queue item
          // check if the item has a cancel token. If it does, 
          if (canceledItems.TryGetValue(queueItem, out bool isCanceled)) {
            // if it has a cancelation token stored, and that token is true, lets try to switch it off, and then equeue the current item from the queue.
            if (isCanceled
              && canceledItems.TryUpdate(queueItem, false, true)
              && queue.TryDequeue(out queueItem)
            ) {
              canceledItems.TryRemove(queueItem, out _);
            }
            // if there's a cancelation token for this item, but it's set to false, we can just remove it.
            if (!isCanceled) {
              if (queue.TryDequeue(out queueItem)) {
                canceledItems.TryRemove(queueItem, out _);
              }
            }

            continue;
          }

          // check if we have a validity function
          if (!isAValidQueueItem(queueItem)) {
            if (queue.TryDequeue(out queueItem)) {
              onQueueItemInvalid(queueItem);
            }

            continue;
          }

          // if we have space, pop off the top of the queue and run it as a job.
          if (runningJobCount < maxChildJobsCount && itemIsReady(queueItem) && queue.TryDequeue(out queueItem)) {
            runningJobCount++;
            // The child job is responsible for removing itself from the running job count when done.
            //    see QueueTaskChildJob.finallyDo()
            getChildJob(queueItem).start();
          }
        }
      }
    }

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
    protected virtual void onQueueItemInvalid(QueueItemType queueItem) {
      return;
    }

    /// <summary>
    /// Sort the queue after each run?
    /// </summary>
    protected virtual void sortQueue() {}
  }
}