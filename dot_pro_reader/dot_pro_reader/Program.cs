using System;
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
        public double longtitude;

        public Coords(double longtitude, double latitude)
        {
            this.longtitude = longtitude;
            this.latitude = latitude;
        }
    }

    class FileProReader
    {
        private byte FFh1;
        private char[] IS3name = new char[13];
        private UInt32 IS3id;
        private UInt32 coilNumber;
        private UInt16 startYear;
        private UInt16 startDay;
        private UInt32 startMili;

        private ProjectionType projectionType;
        private UInt16 stringCount;
        private UInt16 pixelsInString;
        private double latitude;
        private double longitude;
        private double sizeLatitude;
        private double sizeLongtitude;
        private double stepLatitude;
        private double stepLongtitude;

        private UInt16[] pixelsBright;

        public static double MercatorLat(double lat)
        {
            lat = lat * Math.Atan2(1, 1) / 90;
            var result = Math.Log(Math.Tan(0.5 * lat + Math.Atan2(1, 1)));
            result = 90 * result / Math.Atan2(1, 1);
            return result;
        }

        public static double UnmercatorLat(double mercatorLatitude)
        {
            mercatorLatitude = mercatorLatitude * Math.Atan2(1, 1) / 90;
            var result = 2.0 * (Math.Atan(Math.Exp(mercatorLatitude)) - Math.Atan2(1, 1));
            result = 90 * result / Math.Atan2(1, 1);
            return result;
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

                //102
                reader.ReadBytes(410);
                pixelsBright = new UInt16[stringCount * pixelsInString];
                for (var i = 0; i < stringCount * pixelsInString; ++i)
                {
                    pixelsBright[i] = reader.ReadUInt16();
                }
            }
        }

        public Coords GetCoordsFromIndexes(int x, int y)
        {
            y = stringCount - y - 1;
            var resultLongtitude = longitude + x * sizeLongtitude / (pixelsInString - 1);
            var minLatitude = projectionType == ProjectionType.Mercator ? MercatorLat(latitude) : latitude;
            var maxLatitude = projectionType == ProjectionType.Mercator ? MercatorLat(latitude + sizeLatitude) : latitude + sizeLatitude;
            var resultLatitide = minLatitude + y * (maxLatitude - minLatitude) / (pixelsInString - 1);
            resultLatitide = projectionType == ProjectionType.Equirectangular ? UnmercatorLat(resultLatitide) : resultLatitide;
            return new Coords(resultLongtitude, resultLatitide);
        }

        public (int x, int y) GetIndexesFromCoords(Coords coords)
        {
            var column = (coords.longtitude - longitude) / (sizeLongtitude / (pixelsInString - 1));
            var x = Convert.ToInt32(Math.Floor(column + 0.5));
            if (projectionType == ProjectionType.Mercator)
            {
                coords.latitude = MercatorLat(coords.latitude);
            }
            var minLatitude = latitude;
            if (projectionType == ProjectionType.Mercator)
            {
                minLatitude = MercatorLat(minLatitude);
            }
            var maxLatitude = latitude + sizeLatitude;
            if (projectionType == ProjectionType.Mercator)
            {
                maxLatitude = MercatorLat(maxLatitude);
            }
            var denominator = (maxLatitude - minLatitude) / (stringCount - 1);
            var line = (coords.latitude - minLatitude) / denominator;
            var y = Convert.ToInt32(Math.Floor(line + 0.5));
            y = stringCount - y - 1;
            return (x, y);
        }

        public int GetBrightOfPixel(int x, int y)
        {
            return pixelsBright[(x + (pixelsInString * (stringCount - y - 1)))];
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var fileProReader = new FileProReader();
            fileProReader.Read("20040916_081954_NOAA_15.m.pro");
            fileProReader.PrintPasportData();

            Console.Write("Введите индекс x: ");
            int.TryParse(Console.ReadLine(), out int x);
            Console.Write("Введите индекс y: ");
            int.TryParse(Console.ReadLine(), out int y);

            var coords = fileProReader.GetCoordsFromIndexes(x, y);
            Console.WriteLine($"Долгота: {coords.longtitude}, Широта: {coords.latitude}");
            Console.WriteLine($"Яркость: {fileProReader.GetBrightOfPixel(x, y)}");

            Console.Write("Введите долготу: ");
            double.TryParse(Console.ReadLine(), out double longtitude);
            Console.Write("Введите широту: ");
            double.TryParse(Console.ReadLine(), out double latitude);

            var indexes = fileProReader.GetIndexesFromCoords(new Coords(longtitude, latitude));
            Console.WriteLine($"x: {indexes.x}, y: {indexes.y}");
        }
    }
}
