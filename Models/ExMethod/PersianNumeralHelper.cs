using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmisApp.Models.ExMethod
{
    public static class PersianNumeralHelper
    {
        public static string ConvertNumerals(this string input)
        {
            return input.Replace('0', '\u06f0')
                        .Replace('1', '\u06f1')
                        .Replace('2', '\u06f2')
                        .Replace('3', '\u06f3')
                        .Replace('4', '\u06f4')
                        .Replace('5', '\u06f5')
                        .Replace('6', '\u06f6')
                        .Replace('7', '\u06f7')
                        .Replace('8', '\u06f8')
                        .Replace('9', '\u06f9');
            //if (new string[] { "fa-ir" }
            //      .Contains(Thread.CurrentThread.CurrentCulture.Name))
            //{
            //    return input.Replace('0', '\u06f0')
            //            .Replace('1', '\u06f1')
            //            .Replace('2', '\u06f2')
            //            .Replace('3', '\u06f3')
            //            .Replace('4', '\u06f4')
            //            .Replace('5', '\u06f5')
            //            .Replace('6', '\u06f6')
            //            .Replace('7', '\u06f7')
            //            .Replace('8', '\u06f8')
            //            .Replace('9', '\u06f9');
            //}
            //else return input;
        }
        public static string ConvertToStandardNumeral(this string input)
        {
            return input.Replace('\u06f0', '0')
                        .Replace('\u06f1', '1')
                        .Replace('\u06f2', '2')
                        .Replace('\u06f3', '3')
                        .Replace('\u06f4', '4')
                        .Replace('\u06f5', '5')
                        .Replace('\u06f6', '6')
                        .Replace('\u06f7', '7')
                        .Replace('\u06f8', '8')
                        .Replace('\u06f9', '9');
        }
    }
}
