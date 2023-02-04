using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using System.Xml;

namespace WSA_Installer.Models.Files
{
    class extractWSA
    {
        public async static Task<object> ExtractWSA(string PackagePath, string TargetPath, string arch)
        {
            TargetPath += "\\";

            string[] LanguagePacks = { };

            string[] ScalePacks = { };

            string tmp_path = Environment.GetEnvironmentVariable("temp") + "\\WSA安装器临时数据\\";

            Directory.CreateDirectory(tmp_path);

            Directory.CreateDirectory(tmp_path + "language\\");

            Directory.CreateDirectory(tmp_path + "scale\\");

            Directory.CreateDirectory(tmp_path + "language_msix\\");

            Directory.CreateDirectory(TargetPath + "Images");

            try
            {
                bool result = false;

                using (var zip = ZipFile.OpenRead(PackagePath))
                {
                    if (zip != null)
                    {
                        

                        //zip.ExtractToDirectory(System.IO.Path.GetTempPath() + "WSA安装器临时数据");
                        //var f_list = Directory.GetFiles(System.IO.Path.GetTempPath() + "WSA安装器临时数据");
                        foreach (var f in zip.Entries)
                        {
                            if (f.Name.ToLower().Contains("language"))
                            {
                                string SaveName = f.Name.Split("-", 2)[1].Split(".")[0];

                                f.ExtractToFile(tmp_path + "language_msix\\" + f.Name);

                                LanguagePacks.Append(SaveName);
                            }
                            else if (f.Name.ToLower().Contains("scale"))
                            {
                                f.ExtractToFile(tmp_path + "scale\\" + f.Name);

                                using (var img = ZipFile.OpenRead(tmp_path + "scale\\" + f.Name))
                                {
                                    foreach(var img2 in img.Entries){
                                        if (img2.FullName.Substring(0,7) == "Images/")
                                        {
                                            img2.ExtractToFile(TargetPath + "Images\\" + img2.Name);
                                        }
                                    }
                                }
                            }
                            else if (f.Name.ToLower().Contains(arch))
                            {
                                f.ExtractToFile(tmp_path + f.Name);
                                using (var zip_ = ZipFile.OpenRead(tmp_path + f.Name))
                                {
                                    if (zip_ != null)
                                    {
                                        //zip_.ExtractToDirectory(TargetPath);


                                        // Debug.Start
                                        zip_.GetEntry("AppxManifest.xml").ExtractToFile(TargetPath + "AppxManifest.xml");
                                        zip_.GetEntry("resources.pri").ExtractToFile(TargetPath + "resources.pri");
                                        zip_.GetEntry("AppxSignature.p7x").ExtractToFile(TargetPath + "AppxSignature.p7x");
                                        // Debug.End


                                        File.Move(TargetPath + "resources.pri", tmp_path + "language\\" + "en-us.pri");
                                    }
                                }
                            }
                        }
                        foreach (var f in Directory.GetFiles(tmp_path + "language_msix\\"))
                        {
                            using (var l_zip = ZipFile.OpenRead(f))
                            {
                                foreach (var l in l_zip.Entries)
                                {
                                    if (l.Name == "resources.pri")
                                    {
                                        string SaveName = new FileInfo(f).Name.Split("-", 2)[1].Split(".")[0];

                                        l.ExtractToFile(tmp_path + "language\\" + $"{SaveName}.pri");
                                    }
                                }
                            }
                        }

                        File.Delete(TargetPath + "AppxSignature.p7x");

                        File.Create(TargetPath + "resources.pri");

                        result = await Merge_Language_Resource(tmp_path + "language", arch, TargetPath);

                        var xml = new XmlDocument();

                        xml.Load(TargetPath + "AppxManifest.xml");

                        var ResourceManifest = xml.DocumentElement.GetElementsByTagName("Resources")[0];

                        foreach (var lang in LanguagePacks)
                        {
                            var tmp_node = xml.CreateElement("Resource", xml.DocumentElement.NamespaceURI);

                            tmp_node.SetAttribute("language", lang);

                            ResourceManifest.AppendChild(tmp_node);
                        }

                        xml.Save(TargetPath + "AppxManifest.xml");
                    }
                }

                Directory.Delete(tmp_path, true);

                return result;
            }catch(Exception ex)
            {
                Directory.Delete(tmp_path, true);

                return ex;
            }
        }

        private static async Task<bool> Merge_Language_Resource(string path, string arch, string TargetPath)
        {
            string cf = '"' + AppDomain.CurrentDomain.BaseDirectory + "\\xml\\priconfig.xml\"";

            string of = $@"""{TargetPath}resources.pri""";

            path = @$"""{path}""" ;

            //string str = @$"new /pr """"""{path}"""""" /in MicrosoftCorporationII.WindowsSubsystemForAndroid /cf """"""{cf}"""""" /of """"""{of}"""""" /o";

            // $" new /pr " + path + " /in MicrosoftCorporationII.WindowsSubsystemForAndroid /cf " + cf + " /of " + of + " /o"

            var proinfo = new ProcessStartInfo(
                AppDomain.CurrentDomain.BaseDirectory + $"Tools\\{arch}\\makepri.exe");

            proinfo.Verb = "runas";

            //proinfo.CreateNoWindow = false;

            proinfo.ArgumentList.Add("new /pr ");

            proinfo.ArgumentList.Add($"\"{path}\"");

            proinfo.ArgumentList.Add("/in MicrosoftCorporationII.WindowsSubsystemForAndroid");

            proinfo.ArgumentList.Add("/cf");

            proinfo.ArgumentList.Add(cf);

            proinfo.ArgumentList.Add("/of");

            proinfo.ArgumentList.Add(of);
            proinfo.ArgumentList.Add("/o");

            var makepri = Process.Start(proinfo);

            await makepri.WaitForExitAsync();

            if (makepri.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
