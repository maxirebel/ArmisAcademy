using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmFinancial
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public long SaleRefrenceID { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }

        public int Amount { get; set; }
        public int BoonID { get; set; }
        public int TeacherProfit { get; set; }
        // سود خالص
        public int NetProfit { get; set; }
        // سود معرف
        public int Benefits { get; set; }
        public int Discount { get; set; }

    }
}
