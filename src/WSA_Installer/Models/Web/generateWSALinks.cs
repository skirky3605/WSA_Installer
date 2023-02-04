// This file edited from MagiskOnWSALocal. 
// MagiskOnWSALocal is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// MagiskOnWSALocal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License
// along with MagiskOnWSALocal.  If not, see <https://www.gnu.org/licenses/>.
// Copyright(C) 2022 LSPosed Contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace WSA_Installer.Models.Web
{
    class generateWSALinks
    {
        public async static Task<string[]> generateWSALink(string arch,string ReleaseType)
        {
            string url = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx";

            string user = "";

            string cat_id = "858014f3-3934-4abe-8078-4aa193e74ca8";

            string post_data = string.Format(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\GetCookie.xml"), user);

            // url列表
            string[] UrlList = {string.Empty,string.Empty,string.Empty,string.Empty };

            var content = new StringContent(post_data, Encoding.UTF8, "application/soap+xml");

            using (var client = new HttpClient())
            {
                var _rep = await client.PostAsync(url, new StringContent(post_data, Encoding.UTF8, "application/soap+xml"));

                XmlDocument doc = new XmlDocument();

                doc.LoadXml(await _rep.Content.ReadAsStringAsync());

                _rep.Dispose();

                String cookie = doc.GetElementsByTagName("EncryptedData")[0].FirstChild.Value.ToString();

                post_data = string.Format(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\WUIDRequest.xml"), user, cookie, cat_id, "X64");

                XmlDocument doc1 = new XmlDocument();

                var _rep1 = await client.PostAsync(url, new StringContent(post_data, Encoding.UTF8, "application/soap+xml"));

                doc1.LoadXml(HttpUtility.HtmlDecode(await _rep1.Content.ReadAsStringAsync()));

                var filenames = new Dictionary<int, string>();

                foreach (XmlNode node in doc1.GetElementsByTagName("Files"))
                {
                    filenames[int.Parse((node.ParentNode.ParentNode as XmlElement).GetElementsByTagName("ID")[0].FirstChild.Value)] =
                        $"{node.FirstChild.Attributes["InstallerSpecificIdentifier"].Value}_{node.FirstChild.Attributes["FileName"].Value}";
                }

                var identities = new List<object[]> { };

                foreach (XmlNode node in doc1.GetElementsByTagName("SecuredFragment"))
                {
                    var filename = filenames[int.Parse((node.ParentNode.ParentNode.ParentNode as XmlElement).GetElementsByTagName("ID")[0].FirstChild.Value)];

                    var update_identity = node.ParentNode.ParentNode.FirstChild;

                    if (filename.Contains("MicrosoftCorporationII.WindowsSubsystemForAndroid"))
                    {
                        UrlList[0] = await send_req(update_identity.Attributes["UpdateID"].Value, update_identity.Attributes["RevisionNumber"].Value, filename, File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\FE3FileUrl.xml"));
                    }else if (filename.Contains("Microsoft.UI.Xaml") && filename.Contains(arch))
                    {
                        UrlList[1] = await send_req(update_identity.Attributes["UpdateID"].Value, update_identity.Attributes["RevisionNumber"].Value, filename, File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\FE3FileUrl.xml"));
                    }else if (filename.Contains("VCLibs") && filename.Contains(arch))
                    {
                        if (filename.Contains("UWPDesktop")){
                            UrlList[2] = await send_req(update_identity.Attributes["UpdateID"].Value, update_identity.Attributes["RevisionNumber"].Value, filename, File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\FE3FileUrl.xml"));
                        }
                        else
                        {
                            UrlList[3] = await send_req(update_identity.Attributes["UpdateID"].Value, update_identity.Attributes["RevisionNumber"].Value, filename, File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\xml\\FE3FileUrl.xml"));
                        }
                    }
                }

                async Task<string> send_req(string i, string v, string out_file, string fe3)
                {
                    string post_data = string.Format(fe3, user, i, v, ReleaseType);

                    var rep_ = await client.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured", new StringContent(post_data, Encoding.UTF8, "application/soap+xml"));

                    var doc2 = new XmlDocument();

                    doc2.LoadXml(await rep_.Content.ReadAsStringAsync());

                    foreach (XmlElement l in doc2.GetElementsByTagName("FileLocation"))
                    {
                        string url_ = l.GetElementsByTagName("Url")[0].FirstChild.Value;

                        if (url_.Length != 99)
                        {
                            return url_;
                        }
                    }
                    return string.Empty;
                }
                return UrlList;
            }
        }
    }
}
