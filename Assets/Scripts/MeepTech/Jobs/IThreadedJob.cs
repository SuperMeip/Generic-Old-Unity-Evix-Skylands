using System.Collections;

namespace MeepTech.Jobs {

  /// <summary>
  /// Interface for jobs run on other threads
  /// </summary>
  public interface IThreadedJob {

    /// <summary>
    /// Get if the job is done running
    /// </summary>
    bool isDone {
      get;
    }

    /// <summary>
    /// Get if the job is currently running
    /// </summary>
    bool isRunning {
      get;
    }

    /// <summary>
    /// Start the job
    /// </summary>
    void start();

    /// <summary>
    /// Allows you to easily wait in a coroutine for the thread to finish. Just use:
    ///  yield return StartCoroutine(myJob.WaitFor());
    /// </summary>
    /// <returns></returns>
    IEnumerator waitFor();

    /// <summary>
    /// Abort the job
    /// </summary>
    void abort();
  }
}