using System;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Xml;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        Console.WriteLine("Введите команду:");
        string command = Console.ReadLine();

        string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\user\Desktop\docs\study\new\consoleappbase\Database1.mdf;Integrated Security=True";
        string dateFromDatabase = GetDateFromDatabase(connectionString);

        if (command == "--currencies")
        {
            try
            {
                string url = "http://www.cbr.ru/scripts/XML_daily.asp";
                WebClient client = new WebClient();
                client.Encoding = Encoding.GetEncoding("Windows-1251");

                string response = client.DownloadString(url);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(response);

                XmlNodeList valuteNodes = xmlDocument.SelectNodes("//Valute");

                Console.WriteLine("Список доступных валют:");

                foreach (XmlNode valuteNode in valuteNodes)
                {
                    string valuteCode = valuteNode.SelectSingleNode("CharCode").InnerText;
                    string valuteName = valuteNode.SelectSingleNode("Name").InnerText;

                    Console.WriteLine($"{valuteCode} - {valuteName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при получении данных: " + ex.Message);
            }
        }
        else if (command.StartsWith("--quota"))
        {
            try
            {
                string[] arguments = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (arguments.Length < 2)
                {
                    Console.WriteLine("Неверная команда.");
                    return;
                }

                string currencyCode = arguments[1];
                string date = arguments.Length >= 4 && arguments[2] == "--date" ? arguments[3] : dateFromDatabase;

                string url = $"http://www.cbr.ru/scripts/XML_daily.asp?date_req={date}";
                WebClient client = new WebClient();
                client.Encoding = Encoding.GetEncoding("Windows-1251");

                string response = client.DownloadString(url);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(response);

                XmlNode valuteNode = xmlDocument.SelectSingleNode($"//Valute[CharCode='{currencyCode.ToUpper()}']");

                if (valuteNode != null)
                {
                    string valuteName = valuteNode.SelectSingleNode("Name").InnerText;
                    string valuteRate = valuteNode.SelectSingleNode("Value").InnerText;

                    Console.WriteLine($"Курс {currencyCode}: {valuteRate} RUB");
                }
                else
                {
                    Console.WriteLine("Нет данных о заданной валюте.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при получении данных: " + ex.Message);
            }
        }
        else if (command.StartsWith("--exchange"))
        {
            try
            {
                string[] arguments = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (arguments.Length < 4)
                {
                    Console.WriteLine("Недостаточно аргументов. Используйте --exchange (сумма) --from (валюта1) --to (валюта2)");
                    return;
                }

                decimal amount = decimal.Parse(arguments[1]);
                string fromCurrency = arguments[3].ToUpper();
                string toCurrency = arguments.Length >= 6 ? arguments[5].ToUpper() : "";

                if (toCurrency == "")
                {
                    Console.WriteLine("Не указана валюта для конвертации.");
                    return;
                }

                string date = arguments.Length >= 8 && arguments[6] == "--date" ? arguments[7] : dateFromDatabase;

                string url = $"http://www.cbr.ru/scripts/XML_daily.asp?date_req={date}";
                WebClient client = new WebClient();
                client.Encoding = Encoding.GetEncoding("Windows-1251");

                string response = client.DownloadString(url);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);

                decimal rateFrom = GetCurrencyRate(xmlDoc, fromCurrency);
                decimal rateTo = GetCurrencyRate(xmlDoc, toCurrency);

                if (rateFrom == 0 || rateTo == 0)
                {
                    Console.WriteLine("Некорректные коды валют.");
                    return;
                }

                decimal result = amount * rateFrom / rateTo;

                Console.WriteLine($"{amount} {fromCurrency} = {result} {toCurrency}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при получении данных: " + ex.Message);
            }
        }
        else
        {
            Console.WriteLine("Неизвестная команда.");
        }
    }

    static string GetDateFromDatabase(string connectionString)
    {
        string query = "SELECT TOP 1 RequestDate FROM CurrencyData ORDER BY RequestDate DESC";

        using (SqlConnection connection = new SqlConnection(connectionString))
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            connection.Open();
            object result = command.ExecuteScalar();

            if (result != null)
            {
                return Convert.ToDateTime(result).ToString("dd.MM.yyyy");
            }
        }

        return DateTime.Today.ToString("dd.MM.yyyy");
    }

    static decimal GetCurrencyRate(XmlDocument xmlDoc, string currencyCode)
    {
        XmlNode currencyNode = xmlDoc.SelectSingleNode($"//Valute[CharCode='{currencyCode}']/Value");

        if (currencyNode != null && decimal.TryParse(currencyNode.InnerText, out decimal rate))
        {
            return rate;
        }

        return 0;
    }
}
