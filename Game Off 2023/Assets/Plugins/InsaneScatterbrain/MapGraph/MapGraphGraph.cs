using System.Threading;
using InsaneScatterbrain.Dependencies;
using InsaneScatterbrain.ScriptGraph;
using UnityEngine;

namespace InsaneScatterbrain.MapGraph
{
    /// <inheritdoc cref="ScriptGraphGraph"/>
    public class MapGraphGraph : ScriptGraphGraph
    {
        [SerializeField, HideInInspector] private NamedColorSet namedColorSet = default;

        /// <summary>
        /// Gets the named color set for this map graph.
        /// </summary>
        public NamedColorSet NamedColorSet
        {
            get => namedColorSet;
            set => namedColorSet = value;
        }

        public override void RegisterDependencies(DependencyContainer container)
        {
            base.RegisterDependencies(container);
            container.Register(() => namedColorSet);

            var instanceProvider = container.Get<IInstanceProvider>();
            
            var areaExtractor = new ThreadLocal<AreaExtractor>(() => new AreaExtractor(instanceProvider.Get<Area>));
            container.Register(() => areaExtractor.Value);
        }
    }
}