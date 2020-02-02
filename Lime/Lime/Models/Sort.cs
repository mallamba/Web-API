using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lime.Models
{
    public class Sort
    {



		/// <summary>
		/// A list is passed on to be sorted.
		/// A sorted list is returned
		/// </summary>
		/// <param name="list">A List of string to be sorted</param>
		public static List<CalendarEntry> ReturnSortedList(List<string> list)
        {
			list.Sort();
			Dictionary<DateTime, DateTime> dict = new Dictionary<DateTime, DateTime>();
			Dictionary<DateTime, DateTime> temp = new Dictionary<DateTime, DateTime>();
			string[] array = new string[2];
			string dateString, fr, to = "";
			DateTime frDate, toDate; ;
			List<CalendarEntry> entryList = new List<CalendarEntry>();
			CalendarEntry calEntry;

			foreach (string line in list)
			{
				array = line.Split('M');
				fr = array[0] + "M ";
				to = array[1] + "M";
				frDate = DateTime.Parse(fr, System.Globalization.CultureInfo.InvariantCulture);
				frDate = frDate.AddHours ( TimeDef() );
				toDate = DateTime.Parse(to, System.Globalization.CultureInfo.InvariantCulture);
				toDate = toDate.AddHours( TimeDef() );
				dict.Add(frDate, toDate);

				
			}
			List<DateTime> keys = dict.Keys.ToList<DateTime>();
			keys.Sort();
			foreach (DateTime d in keys)
			{
				calEntry = new CalendarEntry();
				calEntry.BeginMeeting = d;
				calEntry.EndMeeting = dict[d];
				entryList.Add(calEntry);
			}
			return entryList;
		}



		/// <summary>
		/// The register time is set to be UTC.
		/// The deffirence between UTC and current time is calculated and returned
		/// </summary>
		private static double TimeDef()
		{
			return (double)(DateTime.Now.Hour - DateTime.UtcNow.Hour);
		}


	}
}