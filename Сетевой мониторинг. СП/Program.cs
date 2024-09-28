
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            // Инициализация модулей
            PacketCapture packetCapture = new PacketCapture();
            PacketFilter packetFilter = new PacketFilter();
            Statistics statistics = new Statistics();
            CommandLineInterface commandLineInterface = new CommandLineInterface(packetFilter, statistics);

            // Запуск перехвата пакетов
            packetCapture.StartCapture();

            // Обработка пакетов
            while (true)
            {
                // Получение пакета
                Packet packet = packetCapture.GetPacket();

                // Фильтрация пакета
                if (packetFilter.FilterPacket(packet))
                {
                    // Вывод информации о пакете
                    Console.WriteLine($"IP-адрес отправителя: {packet.SourceIP}");
                    Console.WriteLine($"IP-адрес получателя: {packet.DestinationIP}");
                    Console.WriteLine($"Порт: {packet.Port}");
                    Console.WriteLine($"Тип протокола: {packet.Protocol}");
                    Console.WriteLine($"Размер пакета: {packet.Size}");

                    // Обновление статистики
                    statistics.UpdateStatistics(packet);
                }
            }
        }
    }

    public class PacketCapture
    {
        public Socket _socket;

        public PacketCapture()
        {
            // Инициализация сокета
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
        }

        public void StartCapture()
        {
            // Запуск перехвата пакетов
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
                _socket.Bind(localEndPoint);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting packet capture: " + ex.Message);
            }
        }

        public Packet GetPacket()
        {
            // Получение пакета
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _socket.Receive(buffer);
                return new Packet(buffer, bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving packet: " + ex.Message);
                throw;
            }
        }
    }

    public class PacketFilter
    {
        public string FilterIP { get; set; }
        public int FilterPort { get; set; }
        public string FilterProtocol { get; set; }

        public PacketFilter()
        {
            // Инициализация критериев фильтрации
            FilterIP = "";
            FilterPort = 0;
            FilterProtocol = "";
        }

        public bool FilterPacket(Packet packet)
        {
            // Применение критериев фильтрации
            if (FilterIP != "" && packet.SourceIP != FilterIP) return false;
            if (FilterPort != 0 && packet.Port != FilterPort) return false;
            if (FilterProtocol != "" && packet.Protocol != FilterProtocol) return false;
            return true;
        }
    }

    public class Statistics
    {
        public int _tcpCount;
        public int _udpCount;
        public int _icmpCount;
        public long _totalSize;

        public Statistics()
        {
            // Инициализация статистики
            _tcpCount = 0;
            _udpCount = 0;
            _icmpCount = 0;
            _totalSize = 0;
        }

        public void UpdateStatistics(Packet packet)
        {
            // Обновление статистики
            switch (packet.Protocol)
            {
                case "TCP":
                    _tcpCount++;
                    break;
                case "UDP":
                    _udpCount++;
                    break;
                case "ICMP":
                    _icmpCount++;
                    break;
            }
            _totalSize += packet.Size;
        }

        public void PrintStatistics()
        {
            // Вывод статистики
            Console.WriteLine($"Количество TCP-пакетов: {_tcpCount}");
            Console.WriteLine($"Количество UDP-пакетов: {_udpCount}");
            Console.WriteLine($"Количество ICMP-пакетов: {_icmpCount}");
            Console.WriteLine($"Общий объем переданных данных: {_totalSize}");
        }
    }

    public class CommandLineInterface
    {
        public PacketFilter _packetFilter;
        public Statistics _statistics;

        public CommandLineInterface(PacketFilter packetFilter, Statistics statistics)
        {
            _packetFilter = packetFilter;
            _statistics = statistics;
        }

        public void Run()
        {
            // Запуск интерфейса командной строки
            while (true)
            {
                Console.Write("Введите команду: ");
                string command = Console.ReadLine();

                // Обработка команд
                switch (command)
                {
                    case "filter":
                        // Установка критериев фильтрации
                        Console.Write("Введите IP-адрес: ");
                        string filterIP = Console.ReadLine();
                        Console.Write("Введите порт: ");
                        int filterPort = int.Parse(Console.ReadLine());
                        Console.Write("Введите тип протокола: ");
                        string filterProtocol = Console.ReadLine();

                        // Установка критериев фильтрации в модуле PacketFilter
                        _packetFilter.FilterIP = filterIP;
                        _packetFilter.FilterPort = filterPort;
                        _packetFilter.FilterProtocol = filterProtocol;

                        break;
                    case "stats":
                        // Вывод статистики
                        _statistics.PrintStatistics();

                        break;
                    case "exit":
                        // Выход из программы
                        return;
                }
            }
        }
    }

    public class Packet
    {
        public string SourceIP { get; set; }
        public string DestinationIP { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public int Size { get; set; }

        public Packet(byte[] buffer, int bytesRead)
        {
            // Анализ содержимого пакета
            int offset = 0;
            SourceIP = BitConverter.ToString(buffer, offset, 4).Replace("-", ".");
            offset += 4;
            DestinationIP = BitConverter.ToString(buffer, offset, 4).Replace("-", ".");
            offset += 4;
            Port = BitConverter.ToInt16(buffer, offset);
            offset += 2;
            Protocol = BitConverter.ToString(buffer, offset, 1);
            Size = bytesRead;
        }
    }
}