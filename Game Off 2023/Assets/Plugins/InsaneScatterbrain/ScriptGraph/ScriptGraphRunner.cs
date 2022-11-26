using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsaneScatterbrain.Serialization;
using InsaneScatterbrain.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace InsaneScatterbrain.ScriptGraph
{
    /// <summary>
    /// The script graph runner is a component that acts as a wrapper for the script graph processor, so that
    /// it can be run easily in the editor or at runtime without writing any additional code.
    /// </summary>
    [AddComponentMenu("Map Graph/Map Graph Runner")]
    public class ScriptGraphRunner : ScriptGraphMonoBehaviour
    {
        /// <summary>
        /// Global event that's called whenever checks fail when a graph is ran by any script graph runner.
        /// </summary>
        public static event Action<ScriptGraphRunner, IEnumerable<IScriptNode>> OnChecksFailed;
        
        /// <summary>
        /// Global event that's called whenever a graph runner is about to run.
        /// </summary>
        public static event Action<ScriptGraphRunner> OnRun;
        
        /// <summary>
        /// Global event that's called whenever a graph is stopped, finishing successfully or otherwise.
        /// </summary>
        public static event Action<ScriptGraphRunner> OnStop;
        
        /// <summary>
        /// Contains all the data set for the input parameters.
        /// </summary>
        [SerializeField, HideInInspector] private DataBag paramsIn = new DataBag();
        
        /// <summary>
        /// If true the script graph is processed on awake.
        /// </summary>
        [SerializeField] private bool runOnAwake = false;
        
        /// <summary>
        /// If true the script graph is processed on start.
        /// </summary>
        [SerializeField] private bool runOnStart = false;
        
        /// <summary>
        /// If true the script graph is processed on its own thread. This is useful if you don't want the processing
        /// to block the main thread, but it might take slightly longer due to some additional overhead.
        /// </summary>
        [SerializeField] private bool runAsynchronously = false;

        public bool RunAsynchronously => runAsynchronously;

        [SerializeField] private bool enableMultiThreading = false;

        [SerializeField] private bool enableObjectPooling = false;

        [SerializeField] private bool skipChecks = false;
            
        [SerializeField] private ScriptGraphProcessor graphProcessor = new ScriptGraphProcessor();
        
        /// <summary>
        /// Event that is triggered each time the script graph processor is done processing.
        /// </summary>
        public event Action<IReadOnlyDictionary<string, object>> OnProcessed;

        [SerializeField] private UnityEvent processed = new UnityEvent();
        
        /// <summary>
        /// Event that is triggered each time the script graph processor fails processing.
        /// </summary>
        public event Action OnError;

        /// <summary>
        /// Gets the graph processor.
        /// </summary>
        public ScriptGraphProcessor GraphProcessor => graphProcessor;
        
        private Dictionary<string, object> latestResult;
        
        private static readonly NewInstanceProvider NewInstanceProvider = new NewInstanceProvider();
        private static readonly NewScriptGraphInstanceProvider NewScriptGraphInstanceProvider = new NewScriptGraphInstanceProvider();
        
        private readonly List<string> paramsToRemove = new List<string>();
        private readonly HashSet<IScriptNode> failedCheckNodes = new HashSet<IScriptNode>();
        
        private MainThreadCommand invokeOnRunCommand;
        private MainThreadCommand InvokeOnRunCommand => invokeOnRunCommand ?? (
            invokeOnRunCommand = new MainThreadCommand(() => OnRun?.Invoke(this)));

        private MainThreadCommand invokeOnStopCommand;
        private MainThreadCommand InvokeOnStopCommand => invokeOnStopCommand ?? (
            invokeOnStopCommand = new MainThreadCommand(() => OnStop?.Invoke(this)));
        
        private MainThreadCommand invokeOnChecksFailedCommand;
        private MainThreadCommand InvokeOnChecksFailedCommand => invokeOnChecksFailedCommand ?? (
            invokeOnChecksFailedCommand = new MainThreadCommand(() => OnChecksFailed?.Invoke(this, failedCheckNodes)));

        public void SetInById(string id, object value)
        {
            var inName = graphProcessor.Graph.InputParameters.GetName(id);
            SetIn(inName, value);
        }
        
        /// <summary>
        /// Sets the value for the parameter associated with the given name.
        /// </summary>
        /// <param name="inName">The parameter's name.</param>
        /// <param name="value">The value.</param>
        public void SetIn(string inName, object value)
        {
            var inputParameters = graphProcessor.Graph.InputParameters;
            var id = inputParameters.GetId(inName);
            var inType = inputParameters.GetType(id);
            if (value == null && inType.IsValueType)
            {
                Debug.LogError($"Input parameter \"{inName}\" cannot be null: it's of type {inType} which is a value type.");
                return;
            } 
            
            if (value != null && inType != value.GetType())
            {
                Debug.LogWarning(
                    $"Input parameter \"{inName}\" should be of type {inType}, " +
                    $"but is of type {value.GetType()}. Conversion will be attempted, but this might fail or lead to unexpected results.");
            }
            
            paramsIn.Set(id, value);
        }

        /// <summary>
        /// Gets the current value for the parameter associated with the given name.
        /// </summary>
        /// <param name="inName">The parameters name.</param>
        /// <typeparam name="T">The parameter's type.</typeparam>
        /// <returns>The value.</returns>
        public T GetIn<T>(string inName)
        {
            var inputParameters = graphProcessor.Graph.InputParameters;
            if (!inputParameters.ContainsName(inName))
            {
                Debug.LogError($"Cannot get input parameter \"{inName}\": not defined.");
                return default;
            }
            
            var id = inputParameters.GetId(inName);
            
            if (!paramsIn.Contains(id)) 
            {
                return default;
            }
            
            return (T) paramsIn.Get(id);
        }

        /// <inheritdoc cref="GetIn{T}"/>
        public object GetIn(string inName)
        {
            var inputParameters = graphProcessor.Graph.InputParameters;
            if (!inputParameters.ContainsName(inName))
            {
                Debug.LogError($"Cannot get input parameter \"{inName}\": not defined.");
                return default;
            }
            
            var id = inputParameters.GetId(inName);
            
            return !paramsIn.Contains(id) 
                ? default 
                : paramsIn.Get(id);
        }
        
        private void Awake()
        {
            if (graphProcessor != null && runOnAwake)
            {
                if (runOnStart)
                {
                    Debug.LogWarning("Run On Awake and Run On Start shouldn't both be enabled, this will likely lead to weird behaviour and errors.");
                }
                Run(); 
            }
        }

        private void Start()
        {
            if (graphProcessor != null && runOnStart)
            {
                Run(); 
            }
        }

        public bool IsRunning { get; private set; }
        
        private static readonly ConcurrentDictionary<ScriptGraphGraph, object> graphLocks = new ConcurrentDictionary<ScriptGraphGraph, object>();

        /// <summary>
        /// Runs the script graph processor.
        /// </summary>
        private void RunProcessor()
        {
            graphLocks.TryAdd(graphProcessor.Graph, new object());
            
            lock (graphLocks[graphProcessor.Graph])
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("Graph runner is already running.");
                }

                IsRunning = true;

                MainThread.Execute(InvokeOnRunCommand);

                var inputParameters = graphProcessor.Graph.InputParameters;

                // First clean up the databag if necessary. Remove any entries for input parameters that no longer exist.
                paramsToRemove.Clear();
                foreach (var inParamId in paramsIn.Names)
                {
                    if (inputParameters.ContainsId(inParamId)) continue;

                    paramsToRemove.Add(inParamId);
                }

                foreach (var paramToRemove in paramsToRemove)
                {
                    paramsIn.Remove(paramToRemove);
                }

                var cancelProcessing = false;

                if (!skipChecks)
                {
                    // Check if all the parameters are assigned, if not, don't run the graph and output the appropriate errors instead.
                    foreach (var inParamId in inputParameters.OrderedIds)
                    {
                        var inParam = paramsIn.Contains(inParamId) ? paramsIn.Get(inParamId) : null;

                        if (inParam == null)
                        {
                            var paramType = inputParameters.GetType(inParamId);
                            if (paramType.IsValueType)
                            {
                                inParam = Activator.CreateInstance(paramType);
                            }

                            paramsIn.Set(inParamId, inParam);
                        }

                        if (inParam != null) continue;

                        cancelProcessing = true;
                        Debug.LogError(
                            $"Input parameter \"{inputParameters.GetName(inParamId)}\" hasn't been assigned a value.",
                            this);
                    }

                    failedCheckNodes.Clear();
                    // Check if all the ports that require a connection are indeed connected.
                    foreach (var consumerNode in graphProcessor.Graph.ConsumerNodes)
                    {
                        foreach (var inputPort in consumerNode.InPorts)
                        {
                            if (!inputPort.IsConnectionRequired || inputPort.IsConnected) continue;

                            Debug.LogError(
                                $"Input port \"{inputPort.Name}\" on node \"{consumerNode.GetType().Name}\" needs to be connected.");

                            failedCheckNodes.Add(consumerNode);
                            cancelProcessing = true;
                        }
                    }

                    if (failedCheckNodes.Count > 0)
                    {
                        MainThread.Execute(InvokeOnChecksFailedCommand);
                    }
                }

                if (cancelProcessing)
                {
                    Debug.LogError("Failed to run graph. Check error messages.");
                    IsRunning = false;
                    MainThread.Execute(InvokeOnStopCommand);
                    return;
                }

                // Assign the input parameter values to the processor.
                foreach (var inParamId in paramsIn.Names)
                {
                    var inParam = paramsIn.Get(inParamId);

                    graphProcessor.In(inParamId, () => inParam);
                }

                graphProcessor.IsMultiThreadingEnabled = enableMultiThreading && runAsynchronously;

                graphProcessor.InstanceProvider =
                    enableObjectPooling
                        ? (IInstanceProvider)PoolManagerSingleton.Instance
                        : NewInstanceProvider;

                graphProcessor.ScriptGraphInstanceProvider =
                    enableObjectPooling
                        ? (IScriptGraphInstanceProvider)ScriptGraphPoolManagerSingleton.Instance
                        : NewScriptGraphInstanceProvider;

                // Finally, run the graph processor and store the output results.
                try
                {
                    latestResult = graphProcessor.Process();
                }
                finally
                {
                    IsRunning = false;
                    MainThread.Execute(InvokeOnStopCommand);
                }
            }
        }

        /// <summary>
        /// Prepares all the input parameters that implement IPreparable.
        /// </summary>
        private void Prepare()
        {
            foreach (var inParamId in paramsIn.Names)
            {
                var inParam = paramsIn.Get(inParamId);
                if (inParam is IPreparable preparable)
                {
                    preparable.Prepare();
                }
            }
        }
        
        /// <summary>
        /// Runs the script graph processor. If run asynchronously is true, it will run its own thread (with the exception
        /// if Unity API stuff of course). It will run on synchronously on the main thread otherwise.
        /// </summary>
        public void Run()
        {
#if UNITY_WEBGL && UNITY_EDITOR
            graphProcessor.IsPlaying = Application.isPlaying;
            if (Application.isPlaying)
            {
                // Multi-threading is not supported on WebGL, so whenever we're in playmode, we want to emulate that
                // behaviour as much as possible.
                RunSync();
                return;
            }
            
            // In the editor, however, we want to respect the settings.
            if (runAsynchronously)
            {
                RunAsync();
            }
            else
            {
                RunSync();
            }
#elif UNITY_WEBGL
            // Multi-threading is not supported on WebGL
            RunSync();
            return;
#else
            if (runAsynchronously)
            {
                RunAsync();
            }
            else
            {
                RunSync();
            }
#endif
        }

        /// <summary>
        /// Runs the script graph processor synchronously.
        /// </summary>
        public void RunSync()
        {
            Prepare();
            RunProcessor();
            InvokeProcessed();
        }

        private void InvokeProcessed()
        {
            IsRunning = false;
            OnProcessed?.Invoke(latestResult);
            processed?.Invoke();
        }

        /// <summary>
        /// Runs the script graph processor asynchronously.
        /// </summary>
        public void RunAsync()
        {
            Prepare();
            
            // Run the processor as a task.
            var task = Task.Run(RunProcessor);

            // If the task fails, log the exception and invoke the OnError event on the main thread.
            task.ContinueWith(t =>
            {
                OnError?.Invoke();

                if (t.Exception != null) Debug.LogException(t.Exception);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
            
            // If the task completes normally, invoke the OnProcessed event on the main thread.
            task.ContinueWith(
                t => InvokeProcessed(), 
                CancellationToken.None, TaskContinuationOptions.NotOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Sets whether or not the RNG seed is randomized. 
        /// </summary>
        /// <param name="isSeedRandom">Whether the seed is random.</param>
        public void SetIsSeedRandom(bool isSeedRandom)
        {
            graphProcessor.IsSeedRandom = isSeedRandom;
        }

        /// <summary>
        /// Sets a static seed on the graph processor. 
        /// </summary>
        /// <param name="seed">The RNG seed.</param>
        public void SetStaticSeed(int seed)
        {
            SetIsSeedRandom(false);
            graphProcessor.Seed = seed;
        }
        
        /// <summary>
        /// Sets a static seed on the graph processor. 
        /// </summary>
        /// <param name="seed">The RNG seed.</param>
        public void SetStaticSeed(string seed)
        {
            SetIsSeedRandom(false);
            if (int.TryParse(seed, out var seedInt))
            {
                graphProcessor.Seed = seedInt;
                graphProcessor.SeedType = SeedType.Int;
            }
            else if (Guid.TryParse(seed, out var seedGuid))
            {
                graphProcessor.SeedGuid = seedGuid.ToString();
                graphProcessor.SeedType = SeedType.Guid;
            }
            else
            {
                Debug.LogError("This is not a valid seed: "+ seed);
            }
        }
    }
}