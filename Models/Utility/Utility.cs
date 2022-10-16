using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArmisApp.Models.Utility
{
    public class Utility
    {
        public string RemoveHtmlTag(string HtmlCode)
        {
            Regex reg = new Regex("<(.*?)>");
            var maches = reg.Matches(HtmlCode);

            foreach (Match item in maches)
            {
                HtmlCode = HtmlCode.Replace(item.Value, "");
            }

            return HtmlCode;
        }

        public string GetSafeHtml(string Html)
        {
            Regex reg = new Regex(@"<[\s]*script(.*?)>(.*?)<(.*?)script>", RegexOptions.Multiline);
            var q = reg.Matches(Html);

            foreach (Match item in q)
            {
                Html = Html.Replace(item.Value, "");
            }

            return Html;
        }
    }
}
