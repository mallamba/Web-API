using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lime.Models
{
    public  class CalendarEntry
    {
        public DateTime BeginMeeting { get; set; }

        public DateTime EndMeeting { get; set; }

        public static bool Contains(CalendarEntry c, List<CalendarEntry> l)
        {
            bool value = false;
            foreach (CalendarEntry ce in l)
            {
                if (ce.BeginMeeting == c.BeginMeeting)
                    if (ce.EndMeeting == c.EndMeeting)
                        value = true;
            }
            return value;
        }
    }
}