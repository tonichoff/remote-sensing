using System;
using System.IO;

namespace dot_pro_reader
{
    class Program
    {
        static double MercatorLat(double lat) {
            lat = lat * Math.Atan2(1, 1) / 90;
		    double mLat = Math.Log(Math.Tan(lat + Math.Atan2(1, 1)));
            mLat = 90 * mLat / Math.Atan2(1, 1);
		    return mLat;
	    }

        static double UnmercatorLat(double mlat)
        {
            mlat = mlat * Math.Atan2(1, 1) / 90;
            double lat = 2.0 * (Math.Atan(Math.Exp(mlat)) - Math.Atan2(1, 1));
            lat = 90 * lat / Math.Atan2(1, 1);
            return lat;
        }

        static void Main(string[] args)
        {
            var fileName = "20040916_081954_NOAA_15.m.pro";
            using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                var FFh1 = reader.ReadByte();
                var IS3name = new char[13];
                for (int i = 0; i < 13; ++i)
                {
                    IS3name[i] = reader.ReadChar();
                }
                var IS3id = reader.ReadUInt32();
                var coilNumber = reader.ReadUInt32();
                var startYear = reader.ReadUInt16();
                var startDay = reader.ReadUInt16();
                var startMili = reader.ReadUInt32();

                Console.WriteLine($"Тип формата FFh1: {FFh1}");
                Console.WriteLine($"Название ИС3: {new string(IS3name)}");
                Console.WriteLine($"Идентификатор ИС3: {IS3id}");
                Console.WriteLine($"Номер витка: {coilNumber}");
                Console.WriteLine($"Год: {startYear}");
                Console.WriteLine($"День: {startDay}");
                Console.WriteLine($"Мили: {startMili}");

                reader.ReadBytes(42);

                var typeProjection = reader.ReadUInt16();
                var stringCount = reader.ReadUInt16();
                var pixelsInString = reader.ReadUInt16();
                var latitude = reader.ReadSingle();
                var longitude = reader.ReadSingle();
                var sizeLatitude = reader.ReadSingle();
                var sizeLongtitude = reader.ReadSingle();
                var stepLatitude = reader.ReadSingle();
                var stepLongtitude = reader.ReadSingle();

                string nameProjection = "";
                if (typeProjection == 1)
                {
                    nameProjection = "Меркаторская";
                }
                else if (typeProjection == 2)
                {
                    nameProjection = "Равнопромежуточная";
                }
                else
                {
                    nameProjection = "unknown";
                }
                Console.WriteLine($"Тип проекции: {nameProjection}");
                Console.WriteLine($"Количество строк: {stringCount}");
                Console.WriteLine($"Количество пискелей в строке: {pixelsInString}");
                Console.WriteLine($"Широта: {latitude}");
                Console.WriteLine($"Долгота: {longitude}");
                Console.WriteLine($"Размер по широте: {sizeLatitude}");
                Console.WriteLine($"Размер по долготе: {sizeLongtitude}");
                Console.WriteLine($"Шаг по широте: {stepLatitude}");
                Console.WriteLine($"Шаг по долготе: {stepLongtitude}");

                //Console.Write("Введите индекс x: ");
                //int.TryParse(Console.ReadLine(), out int x);
                //Console.Write("Введите индекс y: ");
                //int.TryParse(Console.ReadLine(), out int y);
                //
                //y = stringCount - y - 1;
                //var resLon = longitude + x * sizeLongtitude / (pixelsInString - 1);
                //double min_lat = (typeProjection == 1) ? MercatorLat(latitude) : latitude;
                //double max_lat = (typeProjection == 1) ? MercatorLat(latitude + sizeLatitude) : latitude + sizeLatitude;
                //double resLat = min_lat + y * (max_lat - min_lat) / (pixelsInString - 1);
                //resLat = (typeProjection == 2) ? UnmercatorLat(resLat) : resLat;
                //Console.WriteLine($"Широта: {resLat}, Долгота: {resLon}");

                Console.Write("Введите широту: ");
                double.TryParse(Console.ReadLine(), out double finLat);
                Console.Write("Введите долготоу: ");
                double.TryParse(Console.ReadLine(), out double finLong);

                double column = (finLong - longitude) / (sizeLongtitude / (pixelsInString - 1));
                int resCol = Convert.ToInt32(Math.Floor(column + 0.5));

                if (typeProjection == 1) finLat = MercatorLat(finLat);
                double min_lat_a = latitude;
                if (typeProjection == 1) min_lat_a = MercatorLat(min_lat_a);
                double max_lat_a = latitude + sizeLatitude;
                if (typeProjection == 1) max_lat_a = MercatorLat(max_lat_a);
                double res = (max_lat_a - min_lat_a) / (stringCount - 1);
                double line = (finLat - min_lat_a) / res;
                int resLine = Convert.ToInt32(Math.Floor(line + 0.5));
                resLine = stringCount - resLine - 1;
                Console.WriteLine($"x: {resCol}, y: {resLine}");
            }
        }
    }
}
