using KDownloader;
using KDownloader.Download;


//VideoDownloader.SetToolsPath(@"D:\");

VideoDownloader videoDownloader = new(
    @"https://kinescope.io/a6239f75-a36a-4cf6-90c7-df314a99ac27",
    @"d:\SuperVideo\Камушки.mp4",
    @"https://my.hudozhnik.online//",
    Quality.low);


videoDownloader.OnBackMessage += (typeMessage, message, code) =>
{
    if (typeMessage == TypeMessage.Error)
        Console.WriteLine(message);

    if (typeMessage == TypeMessage.Event)
    {
        switch (code)
        {
            case 1:
                Console.WriteLine("Получение информации о видеофайле");
                break;
            case 2:
                Console.WriteLine("Загрузка видеофайла");
                break;
            case 3:
                Console.WriteLine("Загрузка аудиофайла");
                break;
            case 4:
                Console.WriteLine("Рашифровка видеофайла");
                break;
            case 5:
                Console.WriteLine("Рашифровка аудиофайла");
                break;
            case 6:
                Console.WriteLine("Объединение видео и аудиофалов");
                break;
            case 7:
                Console.WriteLine("Завершение работы");
                break;
        }
    }

    if (typeMessage == TypeMessage.Data)
    {
        if (message == "PARTS")
            Console.WriteLine("Количество частей: " + code);
        else
        {
            Console.WriteLine("Часть: " + code);
        }
    }
};

videoDownloader.Start();
videoDownloader.Dispose();

