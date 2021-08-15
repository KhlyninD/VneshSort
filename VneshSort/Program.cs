using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VneshSort
{
    // Сортировка большого файла будет основана на методе внешней сортировки
    class Program
    {
        // Задаем максимальный размер файлов в байтах
        const int maxLenghtSplitFile = 1024 * 1024 * 50;
        // Задаем максимальный размер кучи в байтах
        const int maxMуmoryGC = maxLenghtSplitFile * 5;

        static void Main(string[] args)
        {
            // Генерируем файл и возвращаем путь до директории с файлом
            var pathDB = GeneratotBigFile();
            
            Sort(pathDB);
        }

        private static void Sort(string pathDB)
        {
            // Если начальный файл маленький, то вызываем обычную сортировку, в противном внешнюю
            if (new FileInfo($"{pathDB}//BigFile.txt").Length < 1024*1024)
            {
                string[] splitString = File.ReadAllLines($"{pathDB}//BigFile.txt");
                Array.Sort(splitString);
                File.WriteAllLines($"{pathDB}/SortBigFile.txt", splitString);
            }
            else
            {
                // Делим файл на файлы поменьше и возвращаем путь до директории с файлами
                var pathDS = SplitFile(pathDB);
                // Сортируем маленькие файлы
                Program.SortSplitFile(pathDS);
                // Объединяем маленькие отсортированные файлы
                MergeSortSplitFile(pathDS, pathDB);
            }
        }

        private static void MergeSortSplitFile(string pathDS, string pathDB)
        {
            // Путь к файлу
            var pathFile = $"{pathDB}/SortBigFile.txt";
            // Удаление старого файла, если есть 
            File.Delete(pathFile);
            // Массив путей к маленьким файлам
            var pathSplitFile = Directory.GetFiles(pathDS);
            // Количество маленьких файлов
            var lenghtPathS = pathSplitFile.Length;
            // Массив очереди равный по длине количеству маленьких файлов
            var queueSplitFiles = new Queue<string> [lenghtPathS];
            // Максимальный размер очереди
            var maxCountQueue = maxLenghtSplitFile / lenghtPathS + 2;

            //если файл один, то его просто копируем и переименовываем
            if (lenghtPathS < 2)
            {
                File.Move(pathSplitFile[0], pathFile);
                return;
            }

            // Открываем потоки на чтение файлов и помещаем их в массив потоков
            StreamReader[] splitFile = new StreamReader[lenghtPathS];
            for (int i = 0; i < lenghtPathS; i++)
                splitFile[i] = new StreamReader(pathSplitFile[i]);
            
            // Записываем в массив очереди часть данных из файлов
            for (var i = 0; i < lenghtPathS; i++)
            {
                queueSplitFiles[i] = QueueSplitFile(splitFile[i], maxCountQueue);
            }

            // Создаем отсортированный файл и в цикле его заполняем
            StreamWriter sortFile = new StreamWriter(pathFile);
            while (true)
            {
                // находим минимальный элемент и индекс среди первых
                var min = "";
                var minIndex = -1;
                for (var i = 0; i < lenghtPathS; i++)
                {
                    if (queueSplitFiles[i] != null)
                    {
                        if (queueSplitFiles[i].Count == 0)
                            queueSplitFiles[i] = QueueSplitFile(splitFile[i], maxCountQueue);
                        if (queueSplitFiles[i].Count == 0)
                            continue;

                        if (minIndex < 0 || String.CompareOrdinal(min, queueSplitFiles[i].Peek()) > 0)
                        {
                            min = queueSplitFiles[i].Peek();
                            minIndex = i;
                        }
                    }
                }
                if(minIndex == -1)
                    break;

                // Записываем в файл и изымаем элемент из очереди 
                sortFile.WriteLine(queueSplitFiles[minIndex].Dequeue());
            }
            sortFile.Close();

            // закрываем файлы для чтения и удаляем лишнее
            for (int i = 0; i < lenghtPathS; i++)
            {
                splitFile[i].Close();
                File.Delete(pathSplitFile[i]);
            }
            Directory.Delete(pathDS, true);
            Console.WriteLine("Сортировка завершена");
        }

        // Добавляем в очередь из одного маленького файла часть строк
        private static Queue<string> QueueSplitFile(StreamReader splitFile, int maxLenghtQueue)
        {
            var queueSplitFile = new Queue<string>();

            string line;
            var lengthLine = 0;
            while (maxLenghtQueue >= lengthLine && (line = splitFile.ReadLine()) != null )
            {
                lengthLine += line.Length * sizeof(Char);
                queueSplitFile.Enqueue(line);
            }
            return queueSplitFile;
        }

        private static void SortSplitFile(string pathDS)
        {
            // Проходимся по всем маленьким файлам
            foreach (var pathS in Directory.GetFiles(pathDS))
            {
                // Записываем все строки из файла в массив 
                string[] splitString = File.ReadAllLines(pathS);

                // Сортируем строки в массиве   
                Array.Sort(splitString);

                string newpath = pathS.Replace("Split", "Sorted");
                // Перезаписываем  файл с новыми данными и удаляем лишнее
                File.WriteAllLines(newpath, splitString);
                File.Delete(pathS);
                splitString = null;
                // Вызываем сборщик мусора, если превышает установленное значение в куче
                if (GC.GetTotalMemory(false) > maxMуmoryGC)
                    GC.Collect();
            }
            Console.WriteLine("Маленкие файлы отсортированны");
        }

        private static string SplitFile(string pathDB)
        {
            // Нумерация разделённых файлов
            int splitFileNomber = 0;
            // Путь к директории c маленькими файлами
            var pathDS = $"{pathDB}/FileTemp";

            // Создание директории
            Directory.CreateDirectory(pathDS);
            // Создаем файл поменьше и начинаем запись в него
            StreamWriter splitFile = new StreamWriter($"{pathDS}/SplitFile_{splitFileNomber}.txt");
            // Открываем большой файл для чтения
            using (StreamReader bigFile = new StreamReader($"{pathDB}/BigFile.txt"))
            {
                // Проходимся по всем строкам большого файла
                string line;
                while ((line = bigFile.ReadLine()) != null)
                {
                    // Прогресс выполнения операции
                    Console.Write("Деление файла на меньшие файлы: {0:f2}%   \r", 
                        100.0 * bigFile.BaseStream.Position/ (bigFile.BaseStream.Length));

                    // Копируем строку из большого файла в маленький
                    splitFile.WriteLine(line);

                    // Создаем новый маленький файл, если длинна предыдущего маленького файл
                    // превысило установленное значение
                    if (splitFile.BaseStream.Length > maxLenghtSplitFile && bigFile.Peek() >= 0)
                    {
                        splitFile.Close();
                        splitFileNomber++;
                        splitFile = new StreamWriter($"{pathDS}/SplitFile_{splitFileNomber}.txt");
                    }
                }

            }
            splitFile.Close();
            Console.WriteLine();
            return pathDS;
        }

        private static string GeneratotBigFile()
        {
            // Путь к директории
            var pathD = "File";
            // Путь к файлу
            var pathF = $"{pathD}/BigFile.txt";
            // Массив символов, используемых в генерации строк
            char[] letters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

            // Получение и проверка начальных данных
            Console.WriteLine("Необходимо сгенерировать файл для сортировки.\nВведите количество строк:");
            int countString = TrueInt(Console.ReadLine());

            Console.WriteLine("Введите максимальную длину строки:");
            int maxLenString = TrueInt(Console.ReadLine());


            // Генератор случайных чисел
            Random rand = new Random();

            // Создание директории
            Directory.CreateDirectory(pathD);

            // Удаление старого файла, если есть 
            File.Delete(pathF);

            // Открываем до запись в файл
            using (StreamWriter sw = new StreamWriter(pathF, true, System.Text.Encoding.Default))
            {
                // Цикл для строк
                for (var i = 0; i < countString; i++)
                {
                    // Прогресс выполнения генерации
                    Console.Write("Генерация файла: {0:f2}%   \r", 100.0 * i / (countString-1));
                    
                    // Случайная длинна строки
                    int lenString = rand.Next(1, maxLenString+1);

                    // Создаем пустое слово
                    StringBuilder word = new StringBuilder("");

                    // Цикл для строки
                    for (int j = 1; j <= lenString; j++)
                    {
                        // Случайный символ
                        int letterNum = rand.Next(0, letters.Length - 1);

                        // Добавляем символ в строку
                        word.Append(letters[letterNum]);
                    }

                    // Запись в файл
                    sw.WriteLine(word);
                }
            }
            Console.WriteLine();
            return pathD;
        }

        // Проверка ввода данных
        private static int TrueInt(string v)
        {
            int chislo;
            while (!(int.TryParse(v, out chislo) && chislo > 0))
            {
                Console.WriteLine("Неправильный ввод. Повторите ввод: ");
                v = Console.ReadLine();
            }
            return chislo;
        }
    }
}
