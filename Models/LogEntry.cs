using System;

namespace LogManager.Models
{
    /// <summary>
    /// Представляет запись лога с информацией о времени, уровне и сообщении.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Время создания записи лога.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Уровень логирования (например, INFO, WARN, ERROR).
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Сообщение лога.
        /// </summary>
        public string Message { get; set; }
    }
}