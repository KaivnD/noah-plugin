using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Noah.Utils
{
    public class CheckUpdateEventArgs : EventArgs
    {
        public string Version;
        public string Error;
    }
    public class AutoUpdater
    {
        public string Feed;
        public string CurrentVersion;

        public string LatestFileName { get; private set; }
        public string LatestFileHash { get; private set; }
        public string LatestVersion { get; private set; }

        public delegate void AutoUpdateHandler(string version);
        public event EventHandler OnUpdateAva;

        public AutoUpdater()
        {
        }

        public void CheckForUpdate()
        {
            try
            {
                var client = new RestClient(Feed);
                var request = new RestRequest(Method.GET);

                IRestResponse response = client.Execute(request);

                XmlDocument document = new XmlDocument();
                document.LoadXml(response.Content);
                LatestFileName = document.SelectSingleNode("/root/file").InnerText;
                LatestFileHash = document.SelectSingleNode("/root/hash").InnerText;
                LatestVersion = document.SelectSingleNode("/root/version").InnerText;

                var current = new Version(CurrentVersion);
                var latest = new Version(LatestVersion);

                if (current.CompareTo(latest) < 0)
                {
                    OnUpdateAva(this, new CheckUpdateEventArgs { Version = LatestVersion });
                }

            } catch(Exception ex)
            {
                OnUpdateAva(this, new CheckUpdateEventArgs { Error = ex.Message });
            }
        }

        private string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        internal string DownloadUpdate()
        {
            if (LatestFileName == null) return null;

            string downloadUrl = $@"https://ncfz.oss-cn-shanghai.aliyuncs.com/Noah/Plugin/Rhino/{LatestFileName}";
            var client = new RestClient(downloadUrl);
            var request = new RestRequest(Method.GET);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string rhi = Path.Combine(Path.GetTempPath(), LatestFileName);
                client.DownloadData(request).SaveAs(rhi);
                string hash = GetMD5HashFromFile(rhi);
                if (string.IsNullOrWhiteSpace(hash)) return "Hash 计算失败";

                if (!Equals(LatestFileHash.ToUpper(), hash.ToUpper())) return "下载的安装包Hash值与源文件Hash值不对等，您下载的这个文件已经被修改过了，请勿安装";

                return rhi;
            }

            return "update failed";
        }
    }
}
