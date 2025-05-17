#   БИБЛИОТЕКА СКАЧИВАНИЕ ВИДЕО С KINESCOPE.IO 
    _____________________________________________________________________________________________________________
    KinescopeDownloader - это порт библиотеки на Python kinescope-dl (https://github.com/anijackich/kinescope-dl) 
    библиотека для загрузки видео с Kinescope.io, которая использует MPD (MPEG-DASH) для потоковой передачи видео 
    и аудио. Она поддерживает расшифровку видеофайлов, зашифрованных с помощью ClearKey.
    
    Для Работы библиотеки необходимы следующие утилиты:
        1. mp4decrypt.exe - утилита для расшифровки видеофайлов, зашифрованных с помощью ClearKey.
                            https://www.bento4.com/documentation/mp4decrypt/
        2. ffmpeg.exe - утилита для объединения видео и аудиофайлов в один файл.
                            https://ffmpeg.org/download.html
 

                                                                                                CopyRight © 2025
    _____________________________________________________________________________________________________________
    
    ИНСТРУКЦИЯ ПО ИСПОЛЬЗОВАНИЮ:

    Шаг 1.      
            Задаем путь 'path' к файлам ffmpeg.exe и mp4decrypt.exe

                VideoDownloader.SetToolsPath(path);         

    Шаг 2.
            Создаем объект VideoDownloader с параметрами:
            1. input_url (ссылка на видео, например: https://kinescope.io/a6239f75-a36a-4cf6-90c7-df314a99ac27)
            2. output_file name (имя выходного файла, например: d:\SuperVideo\1.2_Камушки.mp4)
            3. referrer (рефер ссылка на сайт где встроено видео, например: https://my.hudozhnik)
            4. quality (необязательно качество видео, по умолчанию Quality.best, 
                        например: Quality.best. Варианты: Quality.best/Quality.normal/Quality.low)

                VideoDownloader videoDownloader = new(inputURL, outputFile, referrer, quality);

    Шаг 3.
            Подписываемся на событие OnBackMessage, чтобы получать сообщения о процессе загрузки видео и аудио файлов.
                
                videoDownloader.OnBackMessage += (typeMessage, message, code) =>
                {
                    ...
                }
            
            События бывают 3 типов (typeMessage):
            1. TypeMessage.Error - ошибка, например: "Ошибка при получении и парсинге MPD: {ex.Message}" 
            2. TypeMessage.Event - событие (текущая активность), например: 4
            3. TypeMessage.Data - прогресс загрузки, например: 3 

            Для первого случая сообщение находиться в message (code всегда равно 0)
            Для второго (message всегда пустой) случая в code находиться код события:
                1 - Получение информации о видеофайле
                2 - Загрузка видеофайла
                3 - Загрузка аудиофайла
                4 - Рашифровка видеофайла
                5 - Рашифровка аудиофайла
                6 - Объединение видео и аудиофалов
                7 - Завершение работы
            Для третьего случая, если message содержит текст "PARTS" то в code находиться колличество частей 
            для скачивания, если message пустой, то в code находиться информация какая часть была скачана.
            
                
    Шаг 4.
            Запускаем загрузку видео с помощью метода Start()

                videoDownloader.Start();

    Дополнительно можно использовать метод GetVideoResolutions() для получения доступных разрешений видеофайла.
    ( например: List<int> videoResolutions = videoDownloader.GetVideoResolutions(); )
    _____________________________________________________________________________________________________________
    
    Пример использования:

    // Подключаем необходимые пространства имен
    using KDownloader;
    using KDownloader.Download;

    // Устанавливаем путь к ffmpeg.exe и mp4decrypt.exe
    VideoDownloader.SetToolsPath(@"D:\Utils");

    // Создаем объект VideoDownloader с параметрами:
    VideoDownloader videoDownloader = new(
          @"https://kinescope.io/a6239f75-a36a-4cf6-90c7-df314a99ac27", 
          @"d:\SuperVideo\Камушки.mp4", 
          @"https://my.hudozhnik.online//", 
          Quality.low);

    // Подписываемся на событие OnBackMessage, чтобы получать сообщения о процессе загрузки видео и аудио файлов.
    videoDownloader.OnBackMessage += (typeMessage, message, code) =>  
    { 
        switch (typeMessage)
        {
            case TypeMessage.Error:
                Console.WriteLine(message);
                break;
            case TypeMessage.Event:
                Console.WriteLine($"Идет операция: {code}");
                break;
            case TypeMessage.Data:
                if (message == "PARTS") 
                {
                    Console.WriteLine($"Всего частей для загрузки: {code}");
                } else {
                    Console.WriteLine($"Загружаеться: {code} часть");
                }
                break;
        }            
    }

    // Запускаем загрузку видео
    videoDownloader.Start();


    _____________________________________________________________________________________________________________
    Временные файлы будут храниться в системной папке Temp
    После завершения работы временные файлы будут удалены.
