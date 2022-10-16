using ArmisApp.Models.Repository;
using RestSharp;

namespace ArmisApp.Services
{
    public class SmsSender
    {
        //retval :
        // Invalid User Pass=0,
        // Successfull = 1,
        // No Credit = 2,
        // DailyLimit = 3,
        // SendLimit = 4,
        // Invalid Number = 5
        // System IS Disable = 6
        // Bad Words= 7
        // Pardis Minimum Receivers=8
        // Number Is Public=9
        public string SendSms2(string Text,string ToNumber)
        {
            //long[] rec = null;
            //byte[] status = null;
            WebService.Send sms = new WebService.Send();

            ToolsRepository RepTools = new ToolsRepository();
            var settings = RepTools.Settings();

            string retval = sms.SendSimpleSMS2(settings.SmsUserName, settings.SmsPassword, ToNumber, settings.SmsNumber, Text, false);
            //int retval = 0;
            return retval;
        }
        public string SendSms(string Text, string ToNumber)
        {
            ToolsRepository RepTools = new ToolsRepository();
            var settings = RepTools.Settings();

            var client = new RestClient("http://rest.payamak-panel.com/api/SendSMS/SendSMS");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddHeader("postman-token", "fcddb5f4-dc58-c7d5-4bf9-9748710f8789");
            request.AddHeader("cache-control", "no-cache");
            request.AddParameter("application/x-www-form-urlencoded", "username="+ settings.SmsUserName +"&password="+ settings.SmsPassword + "&to="+ToNumber +
                "&from="+ settings.SmsNumber + "&text="+ Text + "&isflash=false", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return response.StatusCode.ToString();
        }
        public int SendMultiSms(string Text,string[] ToNumber)
        {
            long[] rec = null;
            byte[] status = null;
            WebService.Send sms = new WebService.Send();

            ToolsRepository RepTools = new ToolsRepository();
            var settings = RepTools.Settings();

            int retval = sms.SendSms(settings.SmsUserName, settings.SmsPassword, ToNumber, settings.SmsNumber, Text, false,"",ref rec,ref status);
            //int retval = 0;
            return retval;
        }
    }
}
