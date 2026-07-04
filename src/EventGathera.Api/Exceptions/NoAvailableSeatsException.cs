namespace EventGathera.Presentation.Exceptions
{
    /// <summary>
    /// Исключение отсутствия свободных мест на событии
    /// </summary>
    public class NoAvailableSeatsException : Exception
    {
        /// <summary>
        /// ID события
        /// </summary>
        public Guid EventId { get; }
        public NoAvailableSeatsException(string message): base(message) { }

        public NoAvailableSeatsException(string message, Guid id) : base(message) 
        {
            EventId = id;
        }
    }
}
