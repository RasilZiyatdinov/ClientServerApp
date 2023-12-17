using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using Server;

class Program
{
    static async Task Main(string[] args)
    {
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("config.json")); //чтение настроек серверов из файла config.json
        await Task.WhenAll(settings.Servers.Select(server => StartServerAsync(server))); //асинхронный запуск всех серверов
    }

    //запуск TcpListener для каждого сервера
    static async Task StartServerAsync(ServerSettings server)
    {
        TcpListener listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Parse(server.Address), server.Port);
            listener.Start();
            Console.WriteLine($"Сервер запущен {server}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, server);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }

    //обработка соединений с клиентами
    static async Task HandleClientAsync(TcpClient client, ServerSettings server)
    {
        try
        {
            using (var streamReader = new StreamReader(client.GetStream()))
            using (var streamWriter = new StreamWriter(client.GetStream()))
            {
                while (true)
                {
                    //отправка тестовых данных клиенту и чтение ответа
                    string testData = GenerateTestData();
                    await streamWriter.WriteLineAsync(testData);
                    await streamWriter.FlushAsync();

                    Console.WriteLine($"{server} отправил данные клиенту: {testData}");

                    string clientResponse = streamReader.ReadLine();
                    Console.WriteLine($"{server} получил ответ от клиента: {clientResponse}");

                    await Task.Delay(2000); 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при соединении с клиентом: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    //генерация тестовых данных для отправки
    static string GenerateTestData()
    {
        Random random = new Random();

        Dictionary<int, string> testData = new Dictionary<int, string>()
        {
            { 1, "#90#010102#27100322;100323#91" },
            { 2, "#90#010102#27100382;100323#91" },
            { 3, "#90#010102#27100322;100383#91" }
        };
        int randomKey = random.Next(1, 4);

        return testData[randomKey];
    }
}