using System;
using InsaneScatterbrain.Dependencies;

namespace InsaneScatterbrain.ScriptGraph
{
    /// <summary>
    /// Interface each script graph node should inherit from.
    /// </summary>
    public interface IScriptNode : INode
    {
#if UNITY_EDITOR
        string Note { get; set; }
#endif

        string Id { get; }

        event Action<IScriptNode, IDependencyContainer> OnPrepareDependencies;
        void Initialize();
        void LoadDependencies(DependencyContainer container);
    }
}