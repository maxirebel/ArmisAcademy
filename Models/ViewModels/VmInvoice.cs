using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmInvoiceJson
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<VmInvoice> Data { get; set; }
    }
    public class VmInvoice
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public long SaleRefrenceID { get; set; }
        public int Status { get; set; }
        public int Type { get; set; }
        public int Amount { get; set; }
        public int NewInventory { get; set; }
        public int BoonID { get; set; }
    }
    public class VmOnlineInvoce
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public int CourseID { get; set; }
        public int Pkg { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public long SaleRefrenceID { get; set; }
        public int Status { get; set; }
        public int Amount { get; set; }
        public int BoonID { get; set; }
        public List<InvoiceEvents> Evants { get; set; }
    }
    public class InvoiceEvents
    {
        public string id { get; set; }
        public string title { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string groupId { get; set; }
        public string display { get; set; }
    }
    public class VmTeacherInvoice
    {
        public int ID { get; set; }
        public int InvoiceID { get; set; }
        public int Status { get; set; }
        public string StartDate { get; set; }
        public string Date { get; set; }
        public int TotalPrice { get; set; }
        public int BasePrice { get; set; }
        public int TeacherPrice { get; set; }

        // مبلغ حق الزحمه سایت
        public int WagePrice { get; set; }
        //مبلغ جریمه
        public int AmountOfFines { get; set; }
        // مبلغ اضافه
        public int AmountOfAddition { get; set; }

        // مبلغ اضافه بابت لغو جلسه توسط هنرجو
        public int SiteProfitOfAdditional { get; set; }
        public int SiteProfit { get; set; }
        public int SiteTotalProfit { get; set; }

    }
}
