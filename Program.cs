using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LoadBalancer
{
    // класс для запуска приложения
    class Program
    {
        //ip-адрес
        string ipAddress;
        //порт
        int port;
        //количество запросов
        int valreq;
        //содержимое запроса
        string contrequest;
        //сообщение клиента
        string mesclient = "";
        //сообщение сервера
        string messerver = "";
        //номер сервера
        int numserver;

        static void Main(string[] args)
        {
            //запуск серверов
            StartServer("127.0.0.1", 8000);
            StartServer("127.0.0.1", 8001);
            StartServer("127.0.0.1", 8002);
            //отправка запроса клиентом к серверу
            int valreq = SendClientRequest("127.0.0.1", 8000);
            int zapros = 2;
            switch (zapros)
            {
                case 0:
                    Console.WriteLine("Ошибка отправки запроса! Диагностика...");
                    break;
                case 1:
                    Console.WriteLine("Запрос получен");
                    break;
                case 2:
                    Console.WriteLine("Количество запросов: ", valreq);
                    break;
            }
            //Отправка запроса на балансировщик
            zapros = 1;
            switch (zapros)
            {
                case 0:
                    Console.WriteLine("Ошибка на этапе балансировки! Диагностика...");
                    break;
                case 1:
                    Console.WriteLine("Балансировка нагрузки");
                    BalanceRequest(mesclient);
                    break;
            }
            //Отправка ответа сервера клиенту
            zapros = 1;
            switch (zapros)
            {
                case 0:
                    Console.WriteLine("Ошибка обработки запроса! Диагностика...");
                    break;
                case 1:
                    Console.WriteLine("Возвращение результата клиенту");
                    SendResponse(messerver);
                    break;
            }
        }

        public static void StartServer(string ipAddress, int port)
        {
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(ip, port);
            TcpListener listener = new TcpListener(endPoint);

            try
            {
                listener.Start();
                Console.WriteLine($"Сервер запущен на {ip}:{port}");
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine($"Новое подключение от {client.Client.RemoteEndPoint}");
                    // обработка запросов от клиента
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

        public static int SendClientRequest(string ipAddress, int port)
        {
            // Создаем новый TCP клиент и подключаемся к серверу
            TcpClient client = new TcpClient(ipAddress, port);

            // Отправляем запрос серверу
            byte[] data = Encoding.ASCII.GetBytes("Client request");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);

            // Читаем ответ от сервера
            data = new byte[256];
            int bytes = stream.Read(data, 0, data.Length);
            string responseData = Encoding.ASCII.GetString(data, 0, bytes);

            // Получаем количество запросов от сервера
            int requestCount = int.Parse(responseData);

            // Закрываем соединение
            stream.Close();
            client.Close();

            // Возвращаем количество запросов
            return requestCount;
        }

        public static void BalanceRequest(TcpClient client)
        {
            Console.WriteLine("New client connected!");

            // Выбор сервера
            int selectedServer = RoundRobin();

            // Подключение к выбранному серверу
            TcpClient server = serverList[selectedServer];

            // Отправка запроса на сервер
            NetworkStream serverStream = server.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = client.GetStream().Read(buffer, 0, client.ReceiveBufferSize);
            serverStream.Write(buffer, 0, bytesRead);
            serverStream.Flush();

            // Увеличение счетчика запросов от клиента к выбранному серверу
            clientRequestCounts[selectedServer]++;

            // Отправка запроса серверу о количестве запросов от клиента
            byte[] countBuffer = BitConverter.GetBytes(clientRequestCounts[selectedServer]);
            serverStream.Write(countBuffer, 0, countBuffer.Length);
            serverStream.Flush();
        }
        public static void SendResponse(HttpListenerContext context, string responseString)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            System.IO.Stream output = context.Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
