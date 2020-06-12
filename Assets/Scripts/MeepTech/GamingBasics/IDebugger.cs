namespace MeepTech.GamingBasics {
  public interface IDebugger {

    /// <summary>
    /// If the debugger is enabled.
    /// </summary>
    bool isEnabled {
      get;
    }

    /// <summary>
    /// Log a debug message
    /// </summary>
    /// <param name="debugMessage"></param>
    void log(string debugMessage);

    /// <summary>
    /// log an error message.
    /// </summary>
    /// <param name="debugMessage"></param>
    void logError(string debugMessage);
  }
}