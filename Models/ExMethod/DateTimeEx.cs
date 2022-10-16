using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ExMethod
{
    public static class DataTimeEx
    {
        public static DateTime ToShamsi(this DateTime Dt)
        {
            try
            {
                PersianCalendar pDate = new PersianCalendar();

                int Day = pDate.GetDayOfMonth(Dt);
                int Month = pDate.GetMonth(Dt);
                int Year = pDate.GetYear(Dt);

                int Hour = pDate.GetHour(Dt);
                int Minute = pDate.GetMinute(Dt);
                int Second = pDate.GetSecond(Dt);

                // رخ دادن خطا در 31 شهریور
                return new DateTime(Year, Month, Day, Hour, Minute, Second);
            }
            catch (Exception)
            {
                return Dt;
            }
            
        }
    }
}
