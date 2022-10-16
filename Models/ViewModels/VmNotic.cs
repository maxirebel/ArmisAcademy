using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmNotic
    {
        public int Notic { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }

        public PaymentNotic Payment { get; set; }
    }
    public class PaymentNotic
    {
        public string UserImage { get; set; }
        public string Price { get; set; }
        public int Type { get; set; }
        public string ToUserFullName { get; set; }
    }
}
