using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace EasyHarvest
{
    public class HttpClient
    {
        /// <summary>
        /// Get请求
        /// </summary>
        /// <param name="url">目标地址</param>
        /// <returns></returns>
        public static ModSetting setting;
        public static string ApiGet(string url)
        {
            url = "http://101.43.149.170:4651/farm/" + url;
            string reslut = string.Empty;
            try
            {
                HttpWebRequest wbRequest = (HttpWebRequest)WebRequest.Create(url);
                wbRequest.Proxy = null;
                wbRequest.Method = "GET";
                wbRequest.Timeout = 200;
                HttpWebResponse wbResponse = (HttpWebResponse)wbRequest.GetResponse();
                using (Stream responseStream = wbResponse.GetResponseStream())
                {
                    using (StreamReader sReader = new StreamReader(responseStream))
                    {
                        reslut = sReader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return string.Empty;
            }
            return reslut;
        }

        /// <summary>
        /// Post请求
        /// </summary>
        /// <param name="url">目标地址</param>
        /// <param name="sendData">消息体json</param>
        /// <returns></returns>
        public static string ApiPost(string url, string sendData)
        {
            url = "http://101.43.149.170:4651/farm/" + url;
            string reslut = string.Empty;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(sendData);

                HttpWebRequest wbRequest = (HttpWebRequest)WebRequest.Create(url);
                wbRequest.Proxy = null;
                wbRequest.Method = "POST";
                wbRequest.ContentType = "application/json";
                wbRequest.ContentLength = data.Length;
                wbRequest.Timeout = 200;

                using (Stream wStream = wbRequest.GetRequestStream())
                {
                    wStream.Write(data, 0, data.Length);
                }

                HttpWebResponse wbResponse = (HttpWebResponse)wbRequest.GetResponse();
                using (Stream responseStream = wbResponse.GetResponseStream())
                {
                    using (StreamReader sReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        reslut = sReader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return string.Empty;
            }
            return reslut;
        }
    }
}