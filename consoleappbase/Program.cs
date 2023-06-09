using System;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml;

namespace CurrencyQuotes
{
    class Program
    {
        static System.Timers.Timer timer;
        static string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\user\Desktop\docs\study\new\consoleappbase\Database1.mdf;Integrated Security=True";

        static void Main(string[] args)
        {
            System.Text.EncodingProvider ppp = System.Text.CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(ppp);

            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            timer.Start();

            UpdateCurrencyData();

           
        }

        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateCurrencyData();
        }

        static void UpdateCurrencyData()
        {
            try
            {
                string currentDate = DateTime.Today.ToString("dd.MM.yyyy");
                string url = $"http://www.cbr.ru/scripts/XML_daily.asp?date_req={currentDate}";

                WebClient webClient = new WebClient();
                webClient.Encoding = Encoding.GetEncoding("Windows-1251");

                string xmlString = webClient.DownloadString(url);

                XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(xmlString));
                reader.XmlResolver = null;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(reader);

                XmlNodeList valuteNodes = xmlDoc.GetElementsByTagName("Valute");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand clearCommand = new SqlCommand("TRUNCATE TABLE CurrencyQuotes", connection);
                    clearCommand.ExecuteNonQuery();

                    foreach (XmlNode valuteNode in valuteNodes)
                    {
                        string currencyCode = valuteNode.SelectSingleNode("CharCode").InnerText;
                        string currencyName = valuteNode.SelectSingleNode("Name").InnerText;
                        string currencyValue = valuteNode.SelectSingleNode("Value").InnerText;
                        DateTime requestDate = DateTime.Today;

                        SqlCommand insertCommand = new SqlCommand("INSERT INTO CurrencyQuotes (CurrencyCode, CurrencyName, CurrencyValue, RequestDate) VALUES (@CurrencyCode, @CurrencyName, @CurrencyValue, @RequestDate)", connection);
                        insertCommand.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                        insertCommand.Parameters.AddWithValue("@CurrencyName", currencyName);
                        insertCommand.Parameters.AddWithValue("@CurrencyValue", currencyValue);
                        insertCommand.Parameters.AddWithValue("@RequestDate", requestDate);
                        insertCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("Котировки валют успешно загружены в базу данных.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка: " + ex.Message);
            }
        }
    }
}
