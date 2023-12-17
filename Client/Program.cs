using System.Net.Sockets;
using Newtonsoft.Json;
using Client;
using System.Text.RegularExpressions;

class Program
{
    static string dataPattern = @"#90#\d{6}#\d{2}(\d{6}|\d{8});(\d{6}|\d{8})#91"; //шаблон для проверки корректности данных

    static async Task Main(string[] args)
    {
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("config.json")); //чтение настроек клиента из файла config.json
        await StartClientAsync(settings); //асинхронный запуск клиента
    }

    //инициализация и управление клиентскими соединениями
    static async Task StartClientAsync(Settings clientSettings)
    {
        var clients = new List<TcpClient>();
        var streamReaders = new List<StreamReader>();
        var streamWriters = new List<StreamWriter>();

        try
        {
            //инициализация соединений с серверами
            foreach (var serverSettings in clientSettings.Servers)
            {
                var client = new TcpClient(serverSettings.Address, serverSettings.Port);
                clients.Add(client);

                var streamReader = new StreamReader(client.GetStream());
                var streamWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };

                streamReaders.Add(streamReader);
                streamWriters.Add(streamWriter);
            }

            //отправка данных и получение ответов от серверов
            while (true)
            {
                var responses = await ReceiveFromAllServersAsync(streamReaders, clientSettings);

                if (responses != null)
                {
                    //обработка полученных данных и отправка результата обратно на сервера
                    foreach (var streamWriter in streamWriters)
                    {
                        await streamWriter.WriteLineAsync(ProcessData(responses));
                    }
                }
                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в клиенте: {ex.Message}");
        }
        finally
        {
            //закрытие всех соединений при завершении работы
            foreach (var client in clients)
            {
                client.Close();
            }
        }
    }

    //метод для обработки полученных данных и формирования результата
    public static string ProcessData(List<string> dataList)
    {
        List<string> group1 = new List<string>();
        List<string> group2 = new List<string>();

        foreach (var data in dataList)
        {
            //проверка соответствия данных шаблону
            Match match = Regex.Match(data, dataPattern);

            if (match.Success)
            {
                group1.Add(match.Groups[1].Value);
                group2.Add(match.Groups[2].Value);
            }
        }

        var first1 = group1.First();
        var first2 = group2.First();

        //формирование результата с учетом совпадения всех элементов в группах
        string result1 = group1.All(x => x == first1) ? first1 : "NoRead";
        string result2 = group2.All(x => x == first2) ? first2 : "NoRead";

        return "#90#010102#27" + result1 + ";" + result2 + "#91";      
    }

    //метод для одновременного чтения данных от всех серверов
    static async Task<List<string>> ReceiveFromAllServersAsync(List<StreamReader> streamReaders, Settings clientSettings)
    {
        var responses = new List<string>();
        var tasks = new List<Task<string>>();

        for (int i = 0; i < clientSettings.Servers.Count; i++)
        {
            var message = streamReaders[i].ReadLineAsync();
            Console.WriteLine($"Получено сообщение {message.Result} от {clientSettings.Servers[i]}");
            tasks.Add(message);
        }

        await Task.WhenAll(tasks);

        for (int i = 0; i < clientSettings.Servers.Count; i++)
        {
            string response = tasks[i].Result;
            if (string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Сервер {clientSettings.Servers[i]} закрыл соединение");
                return null;
            }
            responses.Add(response);
        }

        Console.WriteLine("Получены сообщения от всех серверов\n");
        return responses;
    }
}

