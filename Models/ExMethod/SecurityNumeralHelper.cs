using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmisApp.Models.ExMethod
{
    public static class SecurityNumeralHelper
    {
        public static string ConvertScurity(this string input)
        {
            return input.Replace('0', 'A')
                        .Replace('1', 'Z')
                        .Replace('2', 'B')
                        .Replace('3', 'D')
                        .Replace('4', 'S')
                        .Replace('5', 'G')
                        .Replace('6', 'T')
                        .Replace('7', 'L')
                        .Replace('8', 'F')
                        .Replace('9', 'N')
                        .Replace('.','=');
        }
        public static string ConvertNumeral(this string input)
        {
            return input.Replace('A', '0')
                        .Replace('Z', '1')
                        .Replace('B', '2')
                        .Replace('D', '3')
                        .Replace('S', '4')
                        .Replace('G', '5')
                        .Replace('T', '6')
                        .Replace('L', '7')
                        .Replace('F', '8')
                        .Replace('N', '9')
                        .Replace('=', '.');
        }
    }
}
