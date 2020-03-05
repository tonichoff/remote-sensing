using System;
using System.IO;

namespace dot_pro_reader
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = "20040916_081954_NOAA_15.m.pro ";
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
            }
        }
    }
}
