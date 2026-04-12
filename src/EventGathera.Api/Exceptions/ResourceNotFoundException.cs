namespace EventGathera.Api.Exceptions
{
    /// <summary>
    /// Тип исключения для ненайденного ресурса
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        /// <summary>
        /// ID ненайденного ресурса
        /// </summary>
        public int ResourceId { get; }

        public ResourceNotFoundException(string message) : base(message) { }

        public ResourceNotFoundException(string message, int id) : base(message) 
        {
            ResourceId = id;
        }
    }
}
