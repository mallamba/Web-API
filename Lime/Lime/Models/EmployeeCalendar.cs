using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lime.Models
{
    public class EmployeeCalendar
    {
        public List<Employee> TheEmployee { get; set; }

        public List<CalendarEntry> TheListOfEntries { get; set; }
    }
}