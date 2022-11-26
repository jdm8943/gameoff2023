namespace InsaneScatterbrain.Threading
{
    /// <summary>
    /// Interface for commands to execute on the main thread.
    /// </summary>
    public interface IMainThreadCommand
    {
        /// <summary>
        /// Returns whether the command is done executing or not.
        /// </summary>
        bool Done { get; }
        
        /// <summary>
        /// Executes the command.
        /// </summary>
        void Execute();
    }
}
