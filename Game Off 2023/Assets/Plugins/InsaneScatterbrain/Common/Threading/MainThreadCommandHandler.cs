using System;
using System.Collections.Concurrent;

namespace InsaneScatterbrain.Threading
{
    /// <summary>
    /// Basic handler of main thread commands. It queues the commands and executes any commands in the queue on update.
    /// </summary>
    public class MainThreadCommandHandler : IMainThreadCommandHandler
    {
        private readonly ConcurrentQueue<IMainThreadCommand> queue = new ConcurrentQueue<IMainThreadCommand>();

        public void Queue(MainThreadCommand command, bool waitForCompletion = true)
        {
            queue.Enqueue(command);

            if (!waitForCompletion) return;
            
            command.WaitForCompletion();
        }

        /// <inheritdoc>
        ///     <cref>IMainThreadCommandHandler.Queue</cref>
        /// </inheritdoc>
        [Obsolete("This overload will likely be removed in 2.0.")]
        public void Queue(Action action, bool waitForCompletion = true)
        {
            var command = new MainThreadCommand(action);
            queue.Enqueue(command);

            if (!waitForCompletion) return;
            
            command.WaitForCompletion();
        }

        /// <inheritdoc cref="IMainThreadCommandHandler.Update"/>
        public void Update()
        {
            if (queue.Count == 0) return;
                
            while (queue.Count > 0)
            {
                queue.TryDequeue(out var command);
                command.Execute(); 
                if (!command.Done) queue.Enqueue(command);
            }
        }
    }
}
