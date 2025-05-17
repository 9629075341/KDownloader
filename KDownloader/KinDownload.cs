/*
    БИБЛИОТЕКА СКАЧИВАНИЕ ВИДЕО С KINESCOPE.IO 
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
            4. quality (необязательно качество видео, по умолчанию Quality.best, например: Quality.best. 
                    Варианты: Quality.best/Quality.normal/Quality.low)

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
            Для третьего случая, если message содержит текст "PARTS" то в code находиться колличество 
            частей для скачивания, если message пустой, то в code находиться информация какая часть была скачана.
            
                
    Шаг 4.
            Запускаем загрузку видео с помощью метода Start()

                videoDownloader.Start();

    Шаг 5.
            Выполнить команду Dispose() для освобождения ресурсов и удаления временных файлов.
                
                videoDownloader.Dispose();


    Дополнительно можно использовать метод GetVideoResolutions() для получения доступных разрешений видеофайла.
    ( например: List<int> videoResolutions = videoDownloader.GetVideoResolutions(); )
____________________________________________________________________________________________________________
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

    // Освобождаем ресурсы и удаляем временные файлы
    videoDownloader.Dispose(); 


____________________________________________________________________________________________________________
    Временные файлы будут храниться в системной папке Temp
    После завершения работы временные файлы будут удалены.
 */



using KDownloader.Video;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml;


namespace KDownloader
{
    file static class Constants
    {
        public const string KINESCOPE_BASE_URL = "https://kinescope.io";
        public const string KINESCOPE_MASTER_PLAYLIST_URL = "https://kinescope.io/new-manifest/{video_id}/master.mpd";
        public const string KINESCOPE_CLEARKEY_LICENSE_URL = "https://license.kinescope.io/v1/vod/{video_id}/acquire/clearkey?token=";
        public const string MP3DECRYPT = "mp4decrypt.exe";
        public const string FFMPEG = "ffmpeg.exe";
    }

    public enum Quality
    {
        best = 0,
        normal = 1,
        low = 2,
    }

    public enum TypeMessage
    {
        Error = 0,
        Event = 1,
        Data = 2,
    }

    namespace Video
    {
        internal class KinescopeVideo : IDisposable
        {
            private readonly string? url;
            public readonly string? video_id;
            private readonly string? referer_url;

            private readonly HttpClient http;

            public KinescopeVideo(string? url = null, string? refererUrl = null)
            {
                this.url = url;
                referer_url = refererUrl;
                http = new HttpClient();
                try
                {
                    video_id = GetVideoID().Result;
                }
                catch (AggregateException ae) { throw ae.InnerException ?? ae; }
            }

            private async Task<string> GetVideoID()
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(referer_url)) request.Headers.Add("Referer", referer_url);

                HttpResponseMessage response = await http.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw new Exception("Видео не найдено");
                if (!responseText.Contains("id: \"")) throw new Exception("Доступ к видео запрещен. Возможно неверная ссылка.");
                string[] parts1 = responseText.Split(["id: \""], StringSplitOptions.None);
                string[] parts2 = parts1[1].Split('"');
                return parts2[0];
            }

            public string GetMpdMasterPlaylistUrl() => Constants.KINESCOPE_MASTER_PLAYLIST_URL.Replace("{video_id}", video_id);

            public string GetClearkeyLicenseUrl() => Constants.KINESCOPE_CLEARKEY_LICENSE_URL.Replace("{video_id}", video_id);

            public async Task<string> GetLicenseKey()
            {
                try
                {
                    string requestUrl = GetClearkeyLicenseUrl();
                    string cencKid = "some-cenc-kid";
                    byte[] kidBytes = Convert.FromHexString(cencKid);
                    string base64Kid = Convert.ToBase64String(kidBytes);
                    string cleanBase64Kid = base64Kid.Replace("=", "");
                    var jsonPayload = new
                    {
                        kids = new[] { cleanBase64Kid },
                        type = "temporary"
                    };
                    var content = new StringContent(JsonSerializer.Serialize(jsonPayload), Encoding.UTF8, "application/json");
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = content
                    };
                    request.Headers.Add("origin", Constants.KINESCOPE_BASE_URL);
                    var response = await this.http.SendAsync(request);
                    var responseText = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseText);
                    var root = doc.RootElement;
                    var key = root.GetProperty("keys")[0].GetProperty("k").GetString() + "==";
                    var keyBytes = Convert.FromBase64String(key);
                    return Convert.ToHexString(keyBytes).ToLower();
                }
                catch (KeyNotFoundException) { throw new Exception("Неподдерживаемый способ шифрования видеофайла"); }
                catch (JsonException) { throw new Exception("Неподдерживаемый способ шифрования видеофайла"); }
            }

            public void Dispose()
            {
                http?.Dispose();
            }
        }
    }

    namespace Download
    {
        public class MpegDash
        {
            public string cenc { get; set; } = string.Empty;
            public List<string> VideoSegments { get; set; } = [];
            public List<string> AudioSegments { get; set; } = [];
        }

        public class KidsRequest
        {
            public List<string> kids { get; set; } = [];
            public string type { get; set; } = string.Empty;
        }

        public class LicenseResponse
        {
            public List<KeyInfo> keys { get; set; } = [];
        }

        public class KeyInfo
        {
            public string kty { get; set; } = string.Empty;
            public string k { get; set; } = string.Empty;
            public string kid { get; set; } = string.Empty;
        }

        public class VideoDownloader : IDisposable
        {

            public delegate void BackMessage(TypeMessage typeMessage, string message = "", int code = 0);
            public event BackMessage? OnBackMessage;

            private readonly string inputURL = string.Empty;
            private readonly string outputFile = string.Empty;
            private readonly string referrer = string.Empty;
            private readonly Quality quality = Quality.best;

            private readonly List<int> video_resolutions = [];
            private int resolution = 720;

            private string licenseKey = string.Empty;

            private static string toolsPath = Directory.GetCurrentDirectory();
            private static readonly DirectoryInfo temp_path = new(Path.GetTempPath() + @"KVD");

            private KinescopeVideo? kinescope_video;
            private HttpClient? http;
            private readonly MpegDash mpd_master = new();

            public VideoDownloader(string inputURL, string outputFile, string referrer, Quality quality = Quality.best)
            {

                this.inputURL = inputURL;
                this.outputFile = outputFile;
                this.referrer = referrer;
                this.quality = quality;
                if (!File.Exists(Path.Combine(toolsPath, Constants.FFMPEG)))
                    throw new Exception($"Файл {Constants.FFMPEG} не найден в папке {toolsPath}.");
                if (!File.Exists(Path.Combine(toolsPath, Constants.MP3DECRYPT)))
                    throw new Exception($"Файл {Constants.MP3DECRYPT} не найден в папке {toolsPath}.");
                temp_path.Create();
            }

            private void GetMpdMaster()
            {
                try
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(kinescope_video!.GetMpdMasterPlaylistUrl());

                    XmlNode? videoInfo = xmldoc.DocumentElement?.FirstChild?.FirstChild;
                    XmlNode? audioInfo = xmldoc.DocumentElement?.FirstChild?.LastChild;

                    foreach (XmlNode item in videoInfo!.ChildNodes)
                    {
                        switch (item.Name)
                        {
                            case "Representation":
                                string height = item.Attributes?.GetNamedItem("height")?.Value ?? string.Empty;
                                if (height.Length > 0) video_resolutions.Add(int.Parse(height));
                                break;
                            case "ContentProtection":
                                string attr = item.Attributes?.GetNamedItem("cenc:default_KID")?.Value ?? string.Empty;
                                if (attr.Length > 0) mpd_master.cenc = attr;
                                break;
                        }
                    }
                    licenseKey = GetLicenseKey();
                    video_resolutions.Sort();
                    if (video_resolutions.Count > 0)
                    {
                        switch (quality)
                        {
                            case Quality.best:
                                resolution = video_resolutions.Last();
                                break;
                            case Quality.normal:
                                if (video_resolutions.Count > 1)
                                    resolution = video_resolutions[video_resolutions.Count - 2];
                                else
                                    resolution = video_resolutions.Last();
                                break;
                            case Quality.low:
                                resolution = video_resolutions.First();
                                break;
                            default:
                                resolution = video_resolutions.Last();
                                break;
                        }
                    }


                    var BaseUrl = videoInfo?.ChildNodes
                        .Cast<XmlNode>()
                        .FirstOrDefault(x => x.Name == "Representation" && x.Attributes?["height"]?.Value == resolution.ToString())
                        ?.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Name == "BaseURL")?.InnerText ?? string.Empty;

                    XmlNodeList? SegmetList = videoInfo?.ChildNodes
                        .Cast<XmlNode>().FirstOrDefault(x => x.Name == "Representation" && x.Attributes?["height"]?.Value == resolution.ToString())
                        ?.ChildNodes
                        .Cast<XmlNode>().FirstOrDefault(x => x.Name == "SegmentList")
                        ?.ChildNodes;

                    string old = BaseUrl;

                    foreach (XmlNode item in SegmetList!)
                    {
                        string segmentLink = $"{BaseUrl}{item.Attributes?["media"]?.Value}";
                        if (old != segmentLink)
                        {
                            mpd_master.VideoSegments.Add(segmentLink);
                            old = segmentLink;
                        }
                    }

                    if (mpd_master.VideoSegments.Count == 0) mpd_master.VideoSegments.Add(BaseUrl);

                    BaseUrl = audioInfo?.ChildNodes
                        .Cast<XmlNode>()
                        .FirstOrDefault(x => x.Name == "Representation")
                        ?.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Name == "BaseURL")?.InnerText ?? string.Empty;

                    SegmetList = audioInfo?.ChildNodes
                        .Cast<XmlNode>().FirstOrDefault(x => x.Name == "Representation")
                        ?.ChildNodes
                        .Cast<XmlNode>().FirstOrDefault(x => x.Name == "SegmentList")
                        ?.ChildNodes;

                    old = BaseUrl;

                    foreach (XmlNode item in SegmetList!)
                    {
                        string segmentLink = $"{BaseUrl}{item.Attributes?["media"]?.Value}";
                        if (old != segmentLink)
                        {
                            mpd_master.AudioSegments.Add(segmentLink);
                            old = segmentLink;
                        }
                    }
                    if (mpd_master.AudioSegments.Count == 0) mpd_master.AudioSegments.Add(BaseUrl);
                }
                catch (Exception ex)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка при получении и парсинге MPD: {ex.Message}");
                    throw new Exception($"An error occurred fetching or parsing MPD master: {ex.Message}", ex);
                }
            }

            public static void SetToolsPath(string path)
            {
                if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
                if (!Directory.Exists(path)) throw new Exception($"Папка {path} не найдена.");
                toolsPath = path;
            }

            public List<int> GetVideoResolutions()
            {
                return video_resolutions;
            }

            public void Start()
            {
                OnBackMessage?.Invoke(TypeMessage.Event, code: 1);      // Получение информации о видеофайле
                kinescope_video = new(inputURL, this.referrer);
                http = new() { Timeout = TimeSpan.FromSeconds(30) };
                GetMpdMaster();

                string videoTempPath = Path.Combine(temp_path.FullName, $"{kinescope_video.video_id}_video.mp4{(licenseKey != null ? ".enc" : "")}");
                string audioTempPath = Path.Combine(temp_path.FullName, $"{kinescope_video.video_id}_audio.mp4{(licenseKey != null ? ".enc" : "")}");
                string decryptedVideoTempPath = Path.Combine(temp_path.FullName, $"{kinescope_video.video_id}_video.mp4");
                string decryptedAudioTempPath = Path.Combine(temp_path.FullName, $"{kinescope_video.video_id}_audio.mp4");

                int i = 0;

                // Download video segments
                OnBackMessage?.Invoke(TypeMessage.Data, "PARTS", mpd_master.VideoSegments.Count);
                OnBackMessage?.Invoke(TypeMessage.Event, code: 2);      // Загрузка видеофайла
                if (mpd_master.VideoSegments.Count > 0)
                {
                    using FileStream f = new(videoTempPath, FileMode.Create, FileAccess.Write);
                    foreach (string segment_url in mpd_master.VideoSegments)
                    {
                        FetchSegment(segment_url, f);
                        i++;
                        OnBackMessage?.Invoke(TypeMessage.Data, code: i);
                    }
                }

                i = 0;

                // Download audio segments
                OnBackMessage?.Invoke(TypeMessage.Data, "PARTS", mpd_master.AudioSegments.Count);
                OnBackMessage?.Invoke(TypeMessage.Event, code: 3);      // Загрузка аудиофайла
                if (mpd_master.AudioSegments.Count > 0)
                {
                    using FileStream f = new(audioTempPath, FileMode.Create, FileAccess.Write);
                    foreach (string segment_url in mpd_master.AudioSegments)
                    {
                        FetchSegment(segment_url, f);
                        i++;
                        OnBackMessage?.Invoke(TypeMessage.Data, code: i);
                    }
                }

                // Расшифровать если найден ключ
                if (licenseKey != null)
                {
                    OnBackMessage?.Invoke(TypeMessage.Event, code: 4);      // Рашифровка видеофайла
                    DecryptMedia(videoTempPath, decryptedVideoTempPath);

                    OnBackMessage?.Invoke(TypeMessage.Event, code: 5);      // Рашифровка аудиофайла
                    DecryptMedia(audioTempPath, decryptedAudioTempPath);
                }
                else
                {
                    // Если файлы не зашифрованы то просто объединим их
                    decryptedVideoTempPath = videoTempPath;
                    decryptedAudioTempPath = audioTempPath;
                }

                // Склейка видео и аудио
                OnBackMessage?.Invoke(TypeMessage.Event, code: 6);              // Объединение видео и аудиофалов
                string finalFilepath = Path.ChangeExtension(outputFile, ".mp4");
                string finalDirectory = Path.GetDirectoryName(finalFilepath)!;

                if (!string.IsNullOrEmpty(finalDirectory) && !Directory.Exists(finalDirectory))
                {
                    Directory.CreateDirectory(finalDirectory);
                }

                bool videoExists = File.Exists(decryptedVideoTempPath);
                bool audioExists = File.Exists(decryptedAudioTempPath);

                if (videoExists && audioExists)
                {
                    MergeTracks(decryptedVideoTempPath, decryptedAudioTempPath, finalFilepath);
                }
                else
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "Ошибка: видео или аудиофайл не были успешно загружены/расшифрованы.Невозможно объединить.");
                    throw new Exception("Невозможно объединить: видео и аудиофайл не найдены.");
                }

                OnBackMessage?.Invoke(TypeMessage.Event, code: 7);  // Завершение работы

                try { if (File.Exists(decryptedVideoTempPath)) File.Delete(decryptedVideoTempPath); } catch { }
                try { if (File.Exists(decryptedAudioTempPath)) File.Delete(decryptedAudioTempPath); } catch { }
            }

            private void MergeTracks(string source_video_filepath, string source_audio_filepath, string target_filepath)
            {
                try
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = $"{toolsPath}\\{Constants.FFMPEG}",
                        Arguments = $"-i \"{source_video_filepath}\" -i \"{source_audio_filepath}\" -c copy \"{target_filepath}\" -y -loglevel error",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using Process process = new() { StartInfo = startInfo };
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string stderr = process.StandardError.ReadToEnd();
                        OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка склейки видео и аудио файлов. Код выхода: {process.ExitCode}. Ошибка: {stderr}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка склейки видео и аудио файлов. Ошибка: {ex.Message}");
                }
            }

            private void DecryptMedia(string source_filepath, string target_filepath)
            {
                try
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = $"{toolsPath}\\{Constants.MP3DECRYPT}",
                        Arguments = $"--key 1:{licenseKey} \"{source_filepath}\" \"{target_filepath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using Process process = new() { StartInfo = startInfo };
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string stderr = process.StandardError.ReadToEnd();
                        OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка расшифровки файла. Код выхода: {process.ExitCode}. Ошибка: {stderr}");
                    }
                }
                catch (Exception ex)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка расшифровки файла. Ошибка: {ex.Message}");
                }
                try { File.Delete(source_filepath); } catch { }
            }

            private void FetchSegment(string segment_url, FileStream fileStream)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        HttpResponseMessage response = http!.GetAsync(segment_url, HttpCompletionOption.ResponseHeadersRead).Result;
                        response.EnsureSuccessStatusCode();
                        using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                        {
                            responseStream.CopyTo(fileStream);
                        }
                        return;
                    }
                    catch (HttpRequestException)
                    {
                        OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка чтения сегмента {segment_url}. (Попытка {i + 1}/5).");
                    }
                    catch (Exception)
                    {
                        OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка чтения сегмента {segment_url}. (Попытка {i + 1}/5).");
                    }
                }
                OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка чтения сегмента {segment_url} после 5 попыток.");

            }

            // Метод для преобразования строки в шестнадцатеричном формате в массив байтов
            private static byte[] HexStringToByteArray(string hex)
            {
                int numberChars = hex.Length;
                byte[] bytes = new byte[numberChars / 2];
                for (int i = 0; i < numberChars; i += 2) bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }

            private string GetLicenseKey()
            {
                try
                {
                    HttpResponseMessage response = http!.PostAsync(
                        requestUri: kinescope_video?.GetClearkeyLicenseUrl(),
                        content: new StringContent(
                            JsonSerializer.Serialize
                                (
                                    new KidsRequest
                                    {
                                        kids = [Convert.ToBase64String(HexStringToByteArray(mpd_master.cenc.Replace("-", ""))).Replace("=", "")],
                                        type = "temporary"
                                    }
                                ),
                                Encoding.UTF8,
                                "application/json"
                        )
                     ).Result;
                    response.EnsureSuccessStatusCode();
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    LicenseResponse licenseResponse = JsonSerializer.Deserialize<LicenseResponse>(responseBody)!;

                    if (licenseResponse?.keys == null || licenseResponse.keys.Count == 0)
                    {
                        OnBackMessage?.Invoke(TypeMessage.Error, "Невозможно получить ключ лицензии");
                        return string.Empty;
                    }
                    string base64Key = licenseResponse!.keys[0].k;
                    while (base64Key.Length % 4 != 0)
                    {
                        base64Key += "=";
                    }
                    byte[] keyBytes = Convert.FromBase64String(base64Key);
                    return BitConverter.ToString(keyBytes).Replace("-", "").ToLower();

                }
                catch (HttpRequestException)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "Ошибка получения ключа по HTTP");
                    return string.Empty;
                }
                catch (JsonException)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "Неверный формат ключа");
                    return string.Empty;
                }
                catch (ArgumentOutOfRangeException)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "К сожалению, в настоящее время поддерживается только тип шифрования ClearKey, а не тот, что в этом видео");
                    return string.Empty;
                }
                catch (NullReferenceException)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "К сожалению, в настоящее время поддерживается только тип шифрования ClearKey, а не тот, что в этом видео");
                    return string.Empty;
                }
                catch (Exception)
                {
                    OnBackMessage?.Invoke(TypeMessage.Error, "Произошла непредвиденная ошибка при получении лицензионного ключа:  {ex.Message}");
                    return string.Empty;
                }
            }

            public void Dispose()
            {
                kinescope_video?.Dispose();
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (http != null)
                    {
                        http.Dispose();
                        http = null;
                    }
                    if (temp_path != null && temp_path.Exists)
                    {
                        try
                        {
                            temp_path.Delete(recursive: true);
                        }
                        catch (IOException ex)
                        {
                            OnBackMessage?.Invoke(TypeMessage.Error, $"Ошибка при удалении временной папки {temp_path.FullName}: {ex.Message}");
                        }
                    }
                }
            }

            ~VideoDownloader()
            {
                Dispose(false);
            }
        }
    }
}


