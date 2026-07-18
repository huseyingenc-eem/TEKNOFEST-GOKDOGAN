using System.Net;

namespace GOKDOGANIHA.Core.Services.Api;

public sealed class GameServerProtocolException : HttpRequestException
{
    public GameServerProtocolException(HttpStatusCode statusCode, string endpoint, string? responseBody)
        : base(BuildMessage(statusCode, endpoint, responseBody), null, statusCode)
    {
        Endpoint = endpoint;
        ResponseBody = responseBody;
    }

    public string Endpoint { get; }
    public string? ResponseBody { get; }

    private static string BuildMessage(HttpStatusCode statusCode, string endpoint, string? body)
    {
        var meaning = (int)statusCode switch
        {
            204 => "Gönderilen paket formatı yanlış.",
            400 => "İstek hatalı veya geçersiz.",
            401 => "Oturum açılması gerekiyor.",
            403 => "Bu işlem için yetki yok.",
            404 => "API adresi bulunamadı.",
            500 => "Yarışma sunucusunda iç hata oluştu.",
            _ => "Beklenmeyen yarışma sunucusu yanıtı."
        };
        return string.IsNullOrWhiteSpace(body)
            ? $"{endpoint}: HTTP {(int)statusCode} · {meaning}"
            : $"{endpoint}: HTTP {(int)statusCode} · {meaning} Sunucu: {body}";
    }
}
