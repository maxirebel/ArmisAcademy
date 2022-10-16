using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Utility
{
    public class TimeUtility
    {
        /// <summary>
        /// Ex: 10 روز پیش
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public string GetTimeName(DateTime dt)
        {
            TimeSpan t = (DateTime.Now - dt);

            if (t.Days >= 365)
            {
                return ((int)(t.Days / 365)) + " سال پیش ";
            }

            if (t.Days >= 30)
            {
                return ((int)(t.Days / 30)) + " ماه پیش ";
            }

            if (t.Days < 30 & t.Days > 0)
            {
                return ((int)(t.Days)) + " روز پیش ";
            }

            if (t.Hours > 0)
            {
                return ((int)(t.Hours)) + " ساعت پیش ";
            }

            if (t.Minutes > 0)
            {
                return ((int)(t.Minutes)) + " دقیقه پیش ";
            }


            return t.Seconds + " ثانیه پیش ";

        }

        public string GetDateName(DateTime dt)
        {
            PersianCalendar pDate = new PersianCalendar();

            int Year = pDate.GetYear(dt);
            int Month = pDate.GetMonth(dt);
            int Day = pDate.GetDayOfMonth(dt);
            int DayOfWeek = (int)dt.DayOfWeek;

            string[] MonthName = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
            string[] DayOfWeekName = { "یکشنبه", "دوشنبه", "سه شنبه", "چهارشنبه", "پنج شنبه", "جمعه", "شنبه" };

            return DayOfWeekName[DayOfWeek] + " " + Day + " " + MonthName[Month - 1] + " " + Year;

        }
        public string GetMonthName(DateTime dt)
        {
            int Month = dt.Month;
            string[] MonthName = { "دی", "بهمن", "اسفند", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر" };
            return MonthName[Month - 1];
        }
        public string GetYearName(DateTime dt)
        {
            PersianCalendar pDate = new PersianCalendar();
            int Year = pDate.GetYear(dt);
            return Year.ToString();
        }
        public string GetDayName(DateTime dt)
        {
            int Day = dt.Day;
            return Day.ToString();
        }
        public string GetHourseTime(int minute=0,int second=0)
        {
            float Ftime =0;
            float primSec = 0;

            string newMinu = "";
            string newSec = "";
            string HourseTime = "";

            if (second > 60)
            {
                primSec =((float)second / 60.0f);
                string[] t = primSec.ToString("00.00").Split(new char[] { '.' });
                if (Convert.ToInt32(t[0]) > 0)
                {
                    minute += Convert.ToInt32(t[0]);
                    newSec = t[1];
                }
                else
                {
                    newSec = second.ToString();
                }
            }
            if (minute > 60)
            {
                Ftime =((float)minute / 60.0f);
                string[] t = Ftime.ToString("00.00").Split(new char[] { '.' });
                if (Convert.ToInt32(t[1]) > 60)
                {
                    newMinu = (Convert.ToInt32(t[1]) / 60).ToString("00");
                }
                else
                {
                    newMinu = (Convert.ToInt32(t[1])).ToString("00");
                }
                HourseTime = t[0] + " ساعت و " + newMinu + " دقیقه و"+newSec+" ثانیه ";
            }
            else
            {
                HourseTime = minute.ToString() + " دقیقه";
            }
            return HourseTime;
        }
    }
}
