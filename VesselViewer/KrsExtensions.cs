namespace VesselViewer
{
    internal static class KrsExtensions
    {
        public static T Module<T>(this Part part) where T : PartModule
        {
            return (T)part.Modules[typeof(T).Name];
        }
    }
}