


public class Error: Exception
{
    public Error(string message) : base(message)
    {
        DebLogger.Error($"Error type {this.GetType().Name}.\n Message: {message}");
    }
     

}
