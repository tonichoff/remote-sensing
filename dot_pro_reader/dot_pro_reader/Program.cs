using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dot_pro_reader
{
    enum ProjectionType
    {
        Mercator,
        Equirectangular
    }

    struct Coords
    {
        public double latitude;
        public double longitude;
        public bool isRadian;

        public const double EarthRadius = 6371.21;

        public Coords(double longtitude, double latitude, bool isRadian = false)
        {
            this.longitude = longtitude;
            this.latitude = latitude;
            this.isRadian = isRadian;
        }

        public void ToRadian()
        {
            if (isRadian)
            {
                return;
            }
            longitude = DegreeToRadian(longitude);
            latitude = DegreeToRadian(latitude);
            isRadian = !isRadian;
        }

        public void ToDegree()
        {
            if (!isRadian)
            {
                return;
            }
            longitude = RadianToDegree(longitude);
            latitude = RadianToDegree(latitude);
            isRadian = !isRadian;
        }

        public static double DegreeToRadian(double x)
        {
             return x * Math.PI / 180;
        }

        public static double RadianToDegree(double x)
        {
            return x / Math.PI * 180;
        }
    }

    class FileProReader
    {
        public struct PointOfSlice
        {
            public int index;
            public int x;
            public int y;
            public Coords coords;
            public int bright;
            public double temperature;
            public double distance;

            public PointOfSlice(int index, int x, int y, Coords coords, int bright, double temperature, double distance)
            {
                this.index = index;
                this.x = x;
                this.y = y;
                this.coords = coords;
                this.bright = bright;
                this.temperature = temperature;
                this.distance = distance;
            }
        }

        private byte FFh1;
        private char[] IS3name = new char[13];
        private uint IS3id;
        private uint coilNumber;
        private ushort startYear;
        private ushort startDay;
        private uint startMili;

        private ProjectionType projectionType;
        private ushort stringCount;
        private ushort pixelsInString;
        private double latitude;
        private double longitude;
        private double sizeLatitude;
        private double sizeLongtitude;
        private double stepLatitude;
        private double stepLongtitude;
        private double coefficientA;
        private double coefficientB;

        private ushort[] pixelsBright;

        public static double MercatorLat(double lat)
        {
            var at = Math.Atan2(1, 1);
            lat *= at / 90;
            return 90 * Math.Log(Math.Tan(0.5 * lat + at)) / at;
        }

        public static double UnmercatorLat(double mercatorLatitude)
        {
            var at = Math.Atan2(1, 1);
            mercatorLatitude *= at / 90;
            return 90 * 2.0 * (Math.Atan(Math.Exp(mercatorLatitude)) - at) / at;
        }

        public double GetDistance(Coords coords1, Coords coords2)
        {
            //var coords1 = GetCoordsFromIndexes(x1, y1);
            //var coords2 = GetCoordsFromIndexes(x2, y2);
            coords1.ToRadian();
            coords2.ToRadian();
            var d = Math.Acos(
                Math.Sin(coords1.latitude) * Math.Sin(coords2.latitude) +
                Math.Cos(coords1.latitude) * Math.Cos(coords2.latitude) * Math.Cos(coords1.longitude - coords2.longitude)
            );
            return d * Coords.EarthRadius;
        }

        public void PrintPasportData()
        { 
            string projectionName = "";
            switch (projectionType)
            {
                case ProjectionType.Equirectangular:
                    projectionName = "Меркаторская";
                    break;
                case ProjectionType.Mercator:
                    projectionName = "Равнопромежуточная";
                    break;
            }

            Console.WriteLine($"Тип формата FFh1: {FFh1}");
            Console.WriteLine($"Название ИС3: {new string(IS3name)}");
            Console.WriteLine($"Идентификатор ИС3: {IS3id}");
            Console.WriteLine($"Номер витка: {coilNumber}");
            Console.WriteLine($"Год: {startYear}");
            Console.WriteLine($"День: {startDay}");
            Console.WriteLine($"Мили: {startMili}");
            Console.WriteLine($"Тип проекции: {projectionName}");
            Console.WriteLine($"Количество строк: {stringCount}");
            Console.WriteLine($"Количество пискелей в строке: {pixelsInString}");
            Console.WriteLine($"Широта: {latitude}");
            Console.WriteLine($"Долгота: {longitude}");
            Console.WriteLine($"Размер по широте: {sizeLatitude}");
            Console.WriteLine($"Размер по долготе: {sizeLongtitude}");
            Console.WriteLine($"Шаг по широте: {stepLatitude}");
            Console.WriteLine($"Шаг по долготе: {stepLongtitude}");
        }

        public void Read(string fileName)
        {
            using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                FFh1 = reader.ReadByte();
                var IS3name = new char[13];
                for (int i = 0; i < 13; ++i)
                {
                    IS3name[i] = reader.ReadChar();
                }
                IS3id = reader.ReadUInt32();
                coilNumber = reader.ReadUInt32();
                startYear = reader.ReadUInt16();
                startDay = reader.ReadUInt16();
                startMili = reader.ReadUInt32();

                reader.ReadBytes(42);
                //72
                var typeProjection = reader.ReadUInt16();
                if (typeProjection == 1)
                {
                    projectionType = ProjectionType.Mercator;
                }
                else if (typeProjection == 2)
                {
                    projectionType = ProjectionType.Equirectangular;
                }
                else
                {
                    throw new InvalidDataException("Неизвестный тип проекции");
                }
                stringCount = reader.ReadUInt16();
                pixelsInString = reader.ReadUInt16();
                latitude = reader.ReadSingle();
                longitude = reader.ReadSingle();
                sizeLatitude = reader.ReadSingle();
                sizeLongtitude = reader.ReadSingle();
                stepLatitude = reader.ReadSingle();
                stepLongtitude = reader.ReadSingle();
                coefficientA = reader.ReadDouble();
                coefficientB = reader.ReadDouble();

                //102
                reader.ReadBytes(394);
                pixelsBright = new ushort[stringCount * pixelsInString];
                for (var i = 0; i < stringCount * pixelsInString; ++i)
                {
                    pixelsBright[i] = reader.ReadUInt16();
                }
            }
        }

        public Coords GetCoordsFromIndexes(int x, int y)
        {
            var resultLongtitude = longitude + x * sizeLongtitude / (pixelsInString - 1);
            var width = (pixelsInString / sizeLongtitude * 360) / (2 * Math.PI);
            var mapOffset = width * Math.Log(Math.Tan((Math.PI / 4) + (Coords.DegreeToRadian(latitude) / 2)));
            var a = (stringCount + mapOffset - y - 0.52) / width;
            var resultLatitude = 180 / Math.PI * (2 * Math.Atan(Math.Exp(a)) - Math.PI / 2);
            return new Coords(resultLongtitude, resultLatitude);
        }

        public (int x, int y) GetIndexesFromCoords(Coords coords)
        {
            var column = (coords.longitude - longitude) / (sizeLongtitude / (pixelsInString - 1));
            var x = Convert.ToInt32(Math.Floor(column + 0.5));
            var width = (pixelsInString / sizeLongtitude * 360) / (2 * Math.PI);
            var mapOffset = width * Math.Log(Math.Tan((Math.PI / 4) + (Coords.DegreeToRadian(latitude) / 2)));
            var log = Math.Log(Math.Tan((Math.PI / 4) + (Coords.DegreeToRadian(coords.latitude) / 2)));
            var estimated = stringCount - ((width * log) - mapOffset) - 0.5;
            var y = Convert.ToInt32(Math.Floor(estimated));
            return (x, y);
        }

        public List<PointOfSlice> CreateSlice(Coords coords1, Coords coords2)
        {
            (var x1, var y1) = GetIndexesFromCoords(coords1);
            (var x2, var y2) = GetIndexesFromCoords(coords2);
            var n = Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
            var deltaLongitude = (coords2.longitude - coords1.longitude) / n;
            var deltaLatitude = (coords2.latitude - coords1.latitude) / n;
            var result = new List<PointOfSlice>();
            var pointer = new Coords(coords1.longitude, coords1.latitude);
            for (var i = 0; i < n + 1; ++i)
            {
                (var x, var y) = GetIndexesFromCoords(pointer);
                var bright = GetBrightOfPixel(x, y);
                var coords = new Coords(pointer.longitude, pointer.latitude);
                result.Add(new PointOfSlice(
                    i + 1, x, y, coords, bright, BrightToCelsius(bright), GetDistance(coords, coords1)
                ));
                pointer.longitude += deltaLongitude;
                pointer.latitude += deltaLatitude;
            }
            return result;
        }

        public void ExportSliceToVec(List<PointOfSlice> slice, string fileName)
        {
            if (slice.Count == 0)
            {
                return;
            }
            using (var file = new StreamWriter(fileName))
            {
                foreach (var point in slice)
                {
                    var coordString = $"{ point.coords.longitude }: { point.coords.latitude}";
                    coordString = coordString.Replace(",", ".").Replace(":", ",");
                    file.WriteLine($"TYPE = POINT COLOR = 10 GEO = ({coordString}) WIDTH = 3 IS_WIDTH_ADAPTIVE = FALSE");
                }
            }
        }

        public int GetBrightOfPixel(int x, int y)
        {
            return pixelsBright[x + (pixelsInString * (stringCount - y - 1))];
        }

        public double BrightToCelsius(int bright)
        {
            return coefficientA * bright + coefficientB;
        }
    }

    class Program
    {
        static void PrintHelp()
        {
            Console.WriteLine("Введите '-cti' чтобы преобразовать координаты в индексы пикселя");
            Console.WriteLine("Введите '-itc' чтобы преобразовать индексы пикселя в координаты");
            Console.WriteLine("Введите '-d' чтобы найти дистанцию между точками, задаными координатами");
            Console.WriteLine("Введите '-s' чтобы создать слайс и экспортировать его в .vec файл");
        }

        static void RunCommand(string command, FileProReader fileProReader)
        {
            switch (command)
            {
                case "-h":
                case "--help":
                    PrintHelp();
                    break;
                case "-itc":
                    Console.Write("Введите индекс x: ");
                    int.TryParse(Console.ReadLine(), out int x);
                    Console.Write("Введите индекс y: ");
                    int.TryParse(Console.ReadLine(), out int y);
                    var coords = fileProReader.GetCoordsFromIndexes(x, y);
                    Console.WriteLine($"Долгота: {coords.longitude}, Широта: {coords.latitude}");
                    Console.WriteLine($"Яркость: {fileProReader.GetBrightOfPixel(x, y)}");
                    break;
                case "-cti":
                    Console.Write("Введите долготу: ");
                    double.TryParse(Console.ReadLine(), out double longtitude);
                    Console.Write("Введите широту: ");
                    double.TryParse(Console.ReadLine(), out double latitude);
                    var indexes = fileProReader.GetIndexesFromCoords(new Coords(longtitude, latitude));
                    Console.WriteLine($"x: {indexes.x}, y: {indexes.y}");
                    Console.WriteLine($"Яркость: {fileProReader.GetBrightOfPixel(indexes.x, indexes.y)}");
                    break;
                case "-d":
                    Console.Write("Введите первую долготу: ");
                    double.TryParse(Console.ReadLine(), out double lg1);
                    Console.Write("Введите первую широту: ");
                    double.TryParse(Console.ReadLine(), out double lt1);
                    Console.Write("Введите вторую долготу: ");
                    double.TryParse(Console.ReadLine(), out double lg2);
                    Console.Write("Введите вторую широту: ");
                    double.TryParse(Console.ReadLine(), out double lt2);
                    Console.WriteLine($"Расстояние {fileProReader.GetDistance(new Coords(lg1, lt1), new Coords(lg2, lt2))}");
                    break;
                case "-s":
                    Console.Write("Введите первую долготу: ");
                    double.TryParse(Console.ReadLine(), out double lon1);
                    Console.Write("Введите первую широту: ");
                    double.TryParse(Console.ReadLine(), out double lat1);
                    Console.Write("Введите вторую долготу: ");
                    double.TryParse(Console.ReadLine(), out double lon2);
                    Console.Write("Введите вторую широту: ");
                    double.TryParse(Console.ReadLine(), out double lat2);
                    var slice = fileProReader.CreateSlice(new Coords(lon1, lat1), new Coords(lon2, lat2));
                    Console.Write("Введите название файла: ");
                    var fileName = Console.ReadLine();
                    fileProReader.ExportSliceToVec(slice, fileName);
                    break;
                default:
                    Console.WriteLine("Неивзестный ключ.");
                    PrintHelp();
                    break;
            }
        }

        static FileProReader TryInititalizeFileReader(string filePath)
        {
            var fileProReader = new FileProReader();
            try
            {
                fileProReader.Read(filePath);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Не удалось найти или прочесть файл.");
                Console.WriteLine("Сообщение об ошибке: " + ex.Message);
                return null;
            }
            return fileProReader;
        }

        static void Main(string[] args)
        {
            FileProReader fileProReader;
            switch (args.Length)
            {
                case 0:
                    Console.WriteLine("Укажите путь к файлу. Также можно сразу указать необхоимый ключ: ");
                    PrintHelp();
                    break;
                case 1:
                    fileProReader = TryInititalizeFileReader(args[0]);
                    if (fileProReader == null)
                    {
                        return;
                    }
                    PrintHelp();
                    RunCommand(Console.ReadLine(), fileProReader);
                    break;
                case 2:
                    fileProReader = TryInititalizeFileReader(args[0]);
                    if (fileProReader == null)
                    {
                        return;
                    }
                    RunCommand(args[1], fileProReader);
                    break;
            }
        }
    }
}
