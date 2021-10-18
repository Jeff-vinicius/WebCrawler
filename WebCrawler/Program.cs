using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using Newtonsoft.Json;
using Npgsql;
using System.Net;
using System.Text;
using System.Configuration;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {            

            StartCrowler();

            Console.ReadKey();
        }

        private static async Task StartCrowler()
        {
            try
            {
                WebCrawlerModel webCrawlerDb = new WebCrawlerModel();

                string urlHome = "https://proxyservers.pro/proxy/list/order/updated/order_dir/asc/page/#";

                IList<string> urlPages = new List<string>();

                HttpClient httpClient = new HttpClient();
                string html = await httpClient.GetStringAsync(urlHome);
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                GetAllUrlPage(urlPages, htmlDocument);

                DateTime startDate = DateTime.Now;
                Console.WriteLine($"INICIANDO EXECUÇÃO EM {startDate:dd/MM/yyy HH:mm:ss}");

                IList<WebCrawlerModel> finalList = new List<WebCrawlerModel>();

                if (urlPages.Count > 0)
                {
                    webCrawlerDb = await SetAllData(webCrawlerDb, urlPages, html, htmlDocument, finalList);

                    int totalLines = finalList.Count;
                    int totalPages = urlPages.Count;

                    DateTime endDate = DateTime.Now;
                    Console.WriteLine($"FINALIZANDO EXECUÇÃO EM {endDate:dd/MM/yyy HH:mm:ss}");

                    string path = @"c:\WebCrawlerFiles";

                    if (!Directory.Exists(path))
                    {
                        DirectoryInfo directory = Directory.CreateDirectory(path);
                    }

                    CreateHtmlFiles(urlPages, path);
                    Console.WriteLine("ARQUIVOS HTML GERADOS");

                    CreateJsonFile(finalList, path);
                    Console.WriteLine("ARQUIVO JSON GERADO");

                    NpgsqlConnection connection;
                    NpgsqlCommand comand;
                    InsertDataBase(startDate, totalLines, totalPages, endDate, out connection, out comand);
                    Console.WriteLine("FINALIZADO INSERÇÃO DOS DADOS NO BANCO");
                }
                else
                {
                    Console.WriteLine("NÁO HÁ DADOS A SEREM CARREGADOS");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("=== DEU ERRO NA EXECUÇÃO ===");

                StringBuilder sb = new StringBuilder();
                sb.Append("[MESSAGE]: ");
                sb.AppendLine(ex.Message);
                sb.Append("[INNER EXCEPTION]: ");
                sb.AppendLine(ex.InnerException == null ? string.Empty : ex.InnerException.Message);
                sb.Append("[STACK TRACE]: ");
                sb.AppendLine(ex.StackTrace);

                Console.WriteLine(sb.ToString());
            }
            finally
            {
                Console.WriteLine("PROCESSO FINALIZADO");
            }
        }

        private static async Task<WebCrawlerModel> SetAllData(WebCrawlerModel webCrawlerDb, IList<string> urlPages, string html, HtmlDocument htmlDocument, IList<WebCrawlerModel> finalList)
        {
            foreach (var url in urlPages)
            {
                HttpClient httpClientPage = new HttpClient();
                string htmlPage = await httpClientPage.GetStringAsync(url);
                HtmlDocument htmlDocumentPage = new HtmlDocument();
                htmlDocumentPage.LoadHtml(html);

                List<List<string>> table = GetAllDataTable(htmlDocumentPage);

                foreach (var item in table)
                {
                    webCrawlerDb = new WebCrawlerModel
                    {
                        IpAdress = item[1].Replace("\n", "").Replace("  ", ""),
                        Port = item[2],
                        Country = item[3].Replace("\n", "").Replace("  ", ""),
                        Protocol = item[6]
                    };
                    finalList.Add(webCrawlerDb);
                }
            }

            return webCrawlerDb;
        }

        private static void InsertDataBase(DateTime startDate, int totalLines, int totalPages, DateTime endDate, out NpgsqlConnection connection, out NpgsqlCommand comand)
        {
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "SELECT version()";
            comand = new NpgsqlCommand(sql, connection);

            comand.CommandText = $"INSERT INTO tb_Web_Crawler_Infos(Qtd_Paginas, Qtd_Linhas, Dt_Termino_Exec, Dt_Inicio_Exec) VALUES('{totalPages}','{totalLines}','{endDate}','{startDate}')";
            comand.ExecuteNonQuery();
        }

        private static void CreateJsonFile(IList<WebCrawlerModel> finalList, string path)
        {
            string jsonResult = JsonConvert.SerializeObject(finalList, Formatting.Indented);

            string pathJson = $"{path}\\resultadoExtracao.json";

            if (!File.Exists(pathJson))
            {
                using var tw = new StreamWriter(pathJson, true);
                tw.WriteLine(jsonResult.ToString());
                tw.Close();
            }
            else
            {
                File.Delete(pathJson);
                using var tw = new StreamWriter(pathJson, true);
                tw.WriteLine(jsonResult.ToString());
                tw.Close();
            }
        }

        private static void CreateHtmlFiles(IList<string> urlPages, string path)
        {
            using var client = new WebClient();
            foreach (var htmlFile in urlPages)
            {
                string nameFile = htmlFile.Replace("https://proxyservers.pro/proxy/list/order/updated/order_dir/asc/page/", "");

                client.DownloadFile(htmlFile, $"{path}\\page{nameFile}.html");
            }
        }

        private static void GetAllUrlPage(IList<string> urlPages, HtmlDocument htmlDocument)
        {
            foreach (HtmlNode linkPage in htmlDocument.DocumentNode.SelectNodes("//a[@class='page-link']"))
            {
                string hrefValue = linkPage.GetAttributeValue("href", string.Empty).Replace("/proxy/list/order/updated/order_dir/asc/page/", "");

                if (hrefValue.Contains("#"))
                {
                    urlPages.Add("https://proxyservers.pro/proxy/list/order/updated/order_dir/asc/page/1");
                }
                else
                {
                    urlPages.Add($"https://proxyservers.pro/proxy/list/order/updated/order_dir/asc/page/{hrefValue}");
                }
            }
        }

        private static List<List<string>> GetAllDataTable(HtmlDocument htmlDocumentPage)
        {
            return htmlDocumentPage.DocumentNode.SelectSingleNode("//table[@class='table table-hover']")
                               .Descendants("tr")
                               .Skip(1)
                               .Where(tr => tr.Elements("td").Count() > 1)
                               .Select(tr => tr.Elements("td").Select(td => td.InnerText).ToList())
                               .ToList();
        }

    }
}
