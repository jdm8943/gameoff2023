namespace InsaneScatterbrain.ScriptGraph
{
    public static class PoolManagerSingleton
    {
        private static PoolManager instance;

        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Initialize();
                }

                return instance;
            }
        }

        private static void Initialize()
        {
            instance = new PoolManager();
            PoolManagerInitializer.Initialize(instance);
        }
    }
}