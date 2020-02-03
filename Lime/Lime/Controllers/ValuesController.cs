using Lime.App_Start;
using Lime.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace Lime.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private readonly string path = "" + Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory) + @"\freebusy.txt";
        private System.IO.StreamReader file;
        private string[] stringArray;
        private string earliestDate, latestDate, hours, extracted = "";
        private DateTime eDate, t, lDate, tempStart, tempEnd;
        private CalendarEntry tempCalendarEntry;
        private List<CalendarEntry> finalList;
        private List<Employee> finalEmployeeList;
        private List<string> errorIdList;
        private int length, errors, eHour, lHour;
        private bool moreIDs = false;



        /// <summary>
        /// HTTP Request GET-method by passing on parameters in the URI. 
        /// After api/meetings/ add the parameters with a '/' between each parameter.
        /// Format for ID is an in digits
        /// Format for EARLIEST, LATEST is text according to: "MM,DD,YYYYxHHqMMqSSxAM"
        /// The ',', 'x', 'q' are left as they are.
        /// Format for HOURS is text according to: "HH-HH"
        /// The HH are both for hour digits and '-' is kept as it is.
        /// The last part is ofr the IDs which are written with a '-' in between.
        /// </summary>
        /// <param name="length">The LENGTH of the requested meeting</param>
        /// <param name="earliest">The EARLIEST requested meeting</param>
        /// <param name="latest">The LATEST requested meeting</param>
        /// <param name="hours">The office HOURS for meetings</param>
        /// <param name="ids">The ID/IDs of the employee/employees.</param>
        /// https://localhost:44316/api/values/meetings/60/01,20,2015x08q00q00xAM/01,22,2015x08q00q00xPM/08-17/57646786307395936680161735716561753784
        [HttpGet]
        [ArrayInput("ids")]
        [Route("meetings/{length}/{earliest}/{latest}/{hours}/{ids?}")]
        public HttpResponseMessage Get(int length, string earliest, string latest, string hours, [FromUri]string[] ids = null)
        {
            return ReturnResluts(length, earliest, latest, hours, ids);
        }



        /// <summary>
        /// HTTP Request POST-method with parameters from body in list of strings.
        /// All parameters are sent in one array of strings:
        /// ["Length","MM,DD,YYYYxHHqMMqSSxAM", "MM,DD,YYYYxHHqMMqSSxAM", "HH-HH", "ID", "ID"]
        /// </summary>
        /// <param name="paramsList">A list of strings with parameters</param>
        /// https://localhost:44316/api/values/meetings/
        /// ["60","01,20,2015x08q00q00xAM", "01,22,2015x08q00q00xPM", "08-17", "57646786307395936680161735716561753784", "156281747655501356358519480949344976171"]
        [HttpPost]
        [Route("meetings/")]
        public HttpResponseMessage Meetings([FromBody] List<string> paramsList)
        {
            if (paramsList != null)
            {
                int length; string earliest; string latest; string hours; string[] ids;
                if (paramsList.Count >= 5)
                {
                    ids = new string[paramsList.Count - 4];
                    length = IsLengthOk(paramsList.ElementAt(0));
                    earliest = paramsList.ElementAt(1);
                    latest = paramsList.ElementAt(2);
                    hours = paramsList.ElementAt(3);
                    for (int i = 4; i < paramsList.Count; i++)
                    {
                        ids[i - 4] = paramsList.ElementAt(i);
                    }
                    return ReturnResluts(length, earliest, latest, hours, ids);
                }
                else
                    return Request.CreateResponse(HttpStatusCode.NoContent, "NOT ENOUGH PARAMETERS");
            }
            else
                return Request.CreateResponse(HttpStatusCode.NoContent, "NO PARAMETERS RECEIVED");
        }



        /// <summary>
        /// Return of an HttpResponseMessage to be sent back to client in accordance with several conditions.
        /// Parameters are passed on from previous GET and POST methods.
        /// </summary>
        /// <param name="length">The LENGTH of the requested meeting</param>
        /// <param name="earliest">The EARLIEST requested meeting</param>
        /// <param name="latest">The LATEST requested meeting</param>
        /// <param name="hours">The office HOURS for meetings</param>
        /// <param name="ids">The ID/IDs of the employee/employees.</param>
        /// <param name="id">The ID of the employee.</param>
        private HttpResponseMessage ReturnResluts(int length, string earliest, string latest, string hours, [FromUri]string[] ids = null)
        {
            //System.Diagnostics.Debug.WriteLine("ids: " + ids.Length);
            earliest = uriStringChars(earliest); latest = uriStringChars(latest);
            if (ids.Length == 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "No ID provieded");
            else if (IsListNotReady(ids))
            {
                errorIdList.Insert(0, "IDs not found or not valid");
                HttpResponseMessage resp = new HttpResponseMessage
                {
                    Content = new StringContent(JsonConvert.SerializeObject(errorIdList), System.Text.Encoding.UTF8, "application/json")
                };
                return resp;
            }
            else if (!IsLengthOk(length))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Length error: " + length);
            else if (!IsDateOk(earliest))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Earliest date request error: " + earliest);
            else if (!IsDateOk(latest))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Latest date request error: " + latest);
            else if (!IsHoursOk(hours))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Office hours error: " + hours);
            else
            {
                eDate = DateTime.Parse(earliest, System.Globalization.CultureInfo.InvariantCulture);
                lDate = DateTime.Parse(latest, System.Globalization.CultureInfo.InvariantCulture);
                if (lDate < eDate.AddMinutes(length))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Earliest: " + eDate + "; Length: " + length + "; Latest: " + latest);
                else
                {
                    EmployeeCalendar e = new EmployeeCalendar();
                    finalEmployeeList = new List<Employee>();
                    this.length = length;
                    this.hours = hours;
                    finalList = new List<CalendarEntry>();
                    ids = ids.Distinct().ToArray();
                    for (int i = 0; i < ids.Length; i++)
                    {
                        e = ReturnEmployeeCalendar(ids[i]);
                        if (i == 1)
                            moreIDs = true;
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, e);
                }
            }
        }



        /// <summary>
        /// Looks up some free meeting times by ID, LENGTH of requested meeting,
        /// EARLIEST and LATEST requested meeting, Office HOURS.
        /// Returns an object of EmployeeCalendar with an id, name and a list of calendarentries of meeting suggestions.
        /// </summary>
        /// <param name="id">The ID of the employee.</param>
        private EmployeeCalendar ReturnEmployeeCalendar(string id)
        {
            EmployeeCalendar employeeCalendar = new EmployeeCalendar();
            Employee employee = new Employee();
            List<string> stringList = new List<string>();
            employee.Id = id;
            string line;

            file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                tempCalendarEntry = new CalendarEntry();
                if (!line.Equals("") && !line.Equals(" "))
                {
                    extracted = line.Substring(0, line.IndexOf(';'));
                    if (extracted.Equals(id))
                    {
                        extracted = line.Substring(extracted.Length + 1);
                        if (!extracted.Any(char.IsDigit))
                            employee.Name = extracted;
                        else
                        {
                            stringArray = extracted.Split(';');
                            extracted = stringArray[0] + stringArray[1];
                            tempStart = DateTime.Parse(stringArray[0], System.Globalization.CultureInfo.InvariantCulture);
                            tempEnd = DateTime.Parse(stringArray[1], System.Globalization.CultureInfo.InvariantCulture);
                            if (tempStart >= eDate && tempEnd <= lDate)
                                stringList.Add(extracted);
                        }
                    }
                }
            }
            file.Close();

            if (employee.Name == null)
                employee.Name = "ID NOT FOUND IN REGISTER";
            finalEmployeeList.Add(employee);
            employeeCalendar.TheEmployee = finalEmployeeList;

            employeeCalendar.TheListOfEntries = Sort.ReturnSortedList(stringList);
            finalList = IntervalList(employeeCalendar.TheListOfEntries, eDate, lDate);
            employeeCalendar.TheListOfEntries = finalList;

            return employeeCalendar;
        }



        /// <summary>
        /// The purpose of this method is to find avialable times for one employee in one period,
        /// or to find common avilable times between several employees.
        /// </summary>
        /// <param name="list">A List of CalendarEntries alreday booked meetings</param>
        /// <param name="eDate">DateTime representing the earliest date and time for meetings.</param>
        /// <param name="lDate">DateTime representing the earliest date and time for meetings.</param>
        private List<CalendarEntry> IntervalList(List<CalendarEntry> list, DateTime eDate, DateTime lDate)
        {
            List<CalendarEntry> newList = new List<CalendarEntry>();
            while (lDate.Hour > lHour)
                lDate = lDate.AddMinutes(-30);

            CalendarEntry c;
            //just a necessasity to get it rolling
            if (list.Count == 0)
                list.Add(new CalendarEntry { BeginMeeting = lDate, EndMeeting = lDate.AddMinutes(length) });
            for (int i = 0; i < list.Count; i++)
            {
                c = list.ElementAt(i);
                while (eDate.Hour > eHour)
                    eDate = eDate.AddMinutes(30);
                while (eDate.Hour < eHour)
                    eDate = eDate.AddMinutes(30);

                t = eDate.AddMinutes(length);
                //eHours & lHours are opening hours

                while (t <= c.BeginMeeting && t <= new DateTime(t.Year, t.Month, t.Day, lHour, 00, 00) && eDate.Hour >= eHour)
                {
                    CalendarEntry newEntry = new CalendarEntry { BeginMeeting = eDate, EndMeeting = t };
                    while (t.Hour >= lHour)
                        t = t.AddMinutes(30);
                    while (t.Hour < eHour)
                        t = t.AddMinutes(60);

                    if (moreIDs)
                    {
                        if (CalendarEntry.Contains(newEntry, finalList))
                            newList.Add(newEntry);
                    }
                    else
                        newList.Add(newEntry);

                    t = t.AddMinutes(length);
                    eDate = t.AddMinutes(-length);

                    if (t > c.BeginMeeting)
                    {
                        eDate = c.EndMeeting.Value;
                        t = eDate.AddMinutes(length);
                        if (i != (list.Count - 1))
                        {
                            while (t < list.ElementAt(i + 1).BeginMeeting && t <= new DateTime(t.Year, t.Month, t.Day, lHour, 00, 00))
                            {
                                newEntry = new CalendarEntry { BeginMeeting = eDate, EndMeeting = t };
                                //If more than one ID, then find common free times and put them in one list
                                if (moreIDs)
                                {
                                    if (CalendarEntry.Contains(newEntry, finalList))
                                        newList.Add(newEntry);
                                }
                                else
                                    newList.Add(newEntry);
                                eDate = t;
                                t = eDate.AddMinutes(length);
                            }
                        }
                        //for the last busy time to check if there is time before office closing hours
                        else if (i == (list.Count - 1))
                        {
                            while (t <= new DateTime(t.Year, t.Month, t.Day, lHour, 00, 00))
                            {
                                newEntry = new CalendarEntry { BeginMeeting = eDate, EndMeeting = t };
                                //If more than one ID, then find common free times and put them in one list
                                if (moreIDs)
                                {
                                    if (CalendarEntry.Contains(newEntry, finalList))
                                        newList.Add(newEntry);
                                }
                                else
                                    newList.Add(newEntry);

                                eDate = t;
                                t = eDate.AddMinutes(length);
                            }
                        }
                    }
                }
            }
            return newList;
        }



        /// <summary>
        /// Checking if entered IDs in the array are valid.
        /// Returns true when all the IDs are invalid,
        /// otherwise a number of errors is saved  and invalid IDs are added to an errorlist 
        /// </summary>
        /// <param name="arr">Array of string representing IDs</param>
        private bool IsListNotReady(string[] arr)
        {
            errors = 0;
            errorIdList = new List<string>();
            //If id not found in register then return code for NOTFOUND to stop
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (!IsIdOk(arr[i]))
                    {
                        errorIdList.Add(arr[i]);
                        errors++;
                    }
                }
            }
            return (errors == arr.Length);
        }



        /// <summary>
        /// This method is used to reforamt the date string by replacing characters with others
        /// </summary>
        /// <param name="str">The string that needs to be re-formatted</param>
        private string uriStringChars(string str)
        {
            str = str.Replace(",", "/");
            str = str.Replace("x", " ");
            str = str.Replace("q", ":");
            return str;
        }



        /// <summary>
        /// This method is used to check if register contains an ID
        /// </summary>
        /// <param name="id">ID string to be checked</param>
        private bool IsIdOk(string id)
        {
            string extractedID;
            bool value = false;
            file = new System.IO.StreamReader(path);
            while ((extractedID = file.ReadLine()) != null)
            {
                if (!extractedID.Equals("") && !extractedID.Equals(" "))
                {
                    extractedID = extractedID.Substring(0, extractedID.IndexOf(';'));
                    if (value = (extractedID.Equals(id)))
                        break;
                }
            }
            file.Close();
            return value;
        }



        /// <summary>
        /// This method is used to control if the length of meeting is in half hour or whole hoour
        /// </summary>
        /// <param name="l">int representing the length of a meeting</param>
        private bool IsLengthOk(int l)
        {
            return (l % 30 == 0);
        }
        private int IsLengthOk(string str)
        {
            int len = 1;
            try
            {
                len = Int32.Parse(str);
            }
            catch (Exception e)
            {
                len = 1;
            }

            if (len >= 30)
                return len;
            else
                return 1;
        }



        /// <summary>
        /// This method is used to return a value if a string can represent a date or not.
        /// </summary>
        /// <param name="str">The string that needs to be checked</param>
        private bool IsDateOk(string str)
        {
            try
            {
                DateTime.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }



        /// <summary>
        /// This method is used to split out office hours from a string
        /// </summary>
        /// <param name="str">The string that needs to be split out</param>
        private bool IsHoursOk(string str)
        {
            try
            {
                string[] hoursArray = str.Split('-');
                eHour = Int32.Parse(hoursArray[0]);
                lHour = Int32.Parse(hoursArray[1]);
                if (lHour > eHour)
                    return true;
            }
            catch (Exception e)
            {
                return false;
            }
            return false;
        }
    }
}
