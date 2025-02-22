using System;
using System.Collections.Generic;

namespace TaskScheduler.Models
{
    public class TaskItem
    {
        public Guid ID { get; set; }
        public string Text { get; set; } = string.Empty;
        // Used for styling/color.
        public string OriginalCategory { get; set; } = string.Empty;
        // Used for day assignment (if any).
        public string DayAssignment { get; set; } = string.Empty;
        public List<bool> MarkerStates { get; set; } = new List<bool>();
    }
}
