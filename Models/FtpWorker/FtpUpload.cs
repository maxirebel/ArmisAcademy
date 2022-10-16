using ArmisApp.Models.Domain.context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

public class Ftp
{
    private FtpParametr GetFtp(string TypeFtp)
    {
        DataContext db = new DataContext();
        var q = db.TblServers.Where(a => a.Type == TypeFtp).ToList();
        int RndServer = new Random().Next(0, q.Count() - 1);

        FtpParametr f = new FtpParametr() {
            FtpAddress = q[RndServer].Ip + q[RndServer].FtpPath,
            Password = q[RndServer].Password,
            UserName = q[RndServer].UserName,
            FtpID=q[RndServer].ID
        };

        return f;
    }

    private FtpParametr GetFtp(int FtpID)
    {
        DataContext db = new DataContext();
        var q = db.TblServers.Where(a => a.ID==FtpID).Single();

        FtpParametr f = new FtpParametr()
        {
            FtpAddress = q.Ip + q.FtpPath,
            Password = q.Password,
            UserName = q.UserName
        };

        return f;
    }
    public int Upload(string TypeFtp,string FileName, Stream MyFile)
    {
        try
        {
            var qP = GetFtp(TypeFtp);

            CheckIfFileExistsOnServer(qP);

            /* Create an FTP Request */
            FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(qP.FtpAddress + FileName);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(qP.UserName, qP.Password);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            /* Establish Return Communication with the FTP Server */
            Stream ftpStream = ftpRequest.GetRequestStream();
            /* Open a File Stream to Read the File for Upload */
            Stream localFileStream = MyFile;
            /* Buffer for the Downloaded Data */
            byte[] byteBuffer = new byte[2048];
            int bytesSent = localFileStream.Read(byteBuffer, 0, 2048);
            /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */

            while (bytesSent != 0)
            {
                ftpStream.Write(byteBuffer, 0, bytesSent);
                bytesSent = localFileStream.Read(byteBuffer, 0, 2048);
            }


            /* Resource Cleanup */
            localFileStream.Close();
            ftpStream.Close();
            ftpRequest = null;

            return qP.FtpID;
        }
        catch (Exception e)
        {
            string msg = e.ToString();
            return -1;
        }

    }
    public bool Remove(int ServerID, string FileName)
    {
        try
        {
            var qP = GetFtp(ServerID);

            /* Create an FTP Request */
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(qP.FtpAddress + FileName);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(qP.UserName, qP.Password);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            /* Establish Return Communication with the FTP Server */
            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Resource Cleanup */
            ftpResponse.Close();
            ftpRequest = null;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    private static void CheckIfFileExistsOnServer(FtpParametr file)
    {
        FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(file.FtpAddress);
        request.Credentials = new NetworkCredential(file.UserName, file.Password);
        request.Method = WebRequestMethods.Ftp.ListDirectory;
        try
        {
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Console.WriteLine("The file exists.");

        }
        catch (WebException ex)
        {
            FtpWebResponse response = (FtpWebResponse)ex.Response;
            if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                FtpWebRequest request2 = (FtpWebRequest)FtpWebRequest.Create(file.FtpAddress);
                request2.Credentials = new NetworkCredential(file.UserName, file.Password);
                request2.Method = WebRequestMethods.Ftp.MakeDirectory;
                FtpWebResponse response2 = (FtpWebResponse)request2.GetResponse();
                Stream Stream = response2.GetResponseStream();
                Stream.Close();
                response2.Close();
                request2 = null;
            }
        }
    }
}

public class FtpParametr
{
    public int FtpID { get; set; }
    public string FtpAddress { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}
