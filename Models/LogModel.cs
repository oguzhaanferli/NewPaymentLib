using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Business.v2.Context;
using Lib.DataModel.v2.Entities.LOG;
using Lib.Extension.v2;

namespace Lib.Payment.Models
{
    public class LogModel
    {
        public int RezID { get; set; }
        public string Message { get; set; }
        public object Request { get; set; }
        public object Response { get; set; }
        public string CustomerName { get; set; }

        public static void Log(int RezID, string Message, object Request, object Response, string CustomerName)
        {
            try
            {
                ContextBase cb = new ContextBase(CustomerName);
                TLib_Log bLog = new TLib_Log();
                bLog.btRequest = ConvertObjectToByte(Request);
                bLog.btResponse = ConvertObjectToByte(Response);
                bLog.stType = Message + " | " + CustomerName;
                bLog.inResID = RezID;
                bLog.stSessionKey = "";
                bLog.dtTimeZone = DateTime.Now;
                cb.Add(bLog);
            }
            catch (Exception)
            {

            }
        }
        public static byte[] ConvertObjectToByte(object data)
        {
            byte[] compressed;

            using (var outStream = new MemoryStream())
            {
                using (var tinyStream = new GZipStream(outStream, CompressionMode.Compress))
                {
                    var dataStr = "";
                    if (data == null) data = "";
                    if (data.GetType() == typeof(String)) dataStr = (string)data;
                    else if (data.GetType() == typeof(Exception)) dataStr = (data as Exception).Message;
                    else dataStr = data.ToSeriliazeJSONString();

                    byte[] byteArray = Encoding.UTF8.GetBytes(dataStr);
                    MemoryStream stream = new MemoryStream(byteArray);
                    var d = stream.ToArray();
                    tinyStream.Write(d, 0, d.Length);
                }
                compressed = outStream.ToArray();
            }
            return compressed;
        }
    }
}
