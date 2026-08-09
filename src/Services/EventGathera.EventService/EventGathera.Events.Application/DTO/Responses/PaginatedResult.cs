namespace EventGathera.Events.Application.DTO.Responses
{
    /// <summary>
    /// Пагинированный список
    /// </summary>
    /// <typeparam name="T">Тип элементов в списке</typeparam>
    public class PaginatedResult<T>
    {
        public int TotalItems { get; set; }

        public IEnumerable<T> Items { get; set; }

        public int CurrrentPage { get; set; }

        public int ItemsOnCurrrentPage { get; set; }
    }
}
