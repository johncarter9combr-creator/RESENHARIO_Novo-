using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Resenhario.Api.Controllers;

[ApiController]
[Route("api/CatalogoExterno")]
public class CatalogoExternoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CatalogoExternoController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public record FilmeExterno(string TmdbId, string Titulo, string? Diretor, int? AnoLancamento, string? PosterUrl);
    public record LivroExterno(string Id, string Titulo, string Autor, string? Sinopse, string? Isbn, string? CapaUrl, string? AnoPublicacao);
    public record SerieExterna(string FonteId, string Titulo, string? Sinopse, string? ImagemUrl, string? DataEstreia);

    [HttpGet("livros")]
    [HttpGet("~/api/Livro/buscar")]
    public async Task<IActionResult> BuscarLivros([FromQuery] string? termo, [FromQuery] string? q, CancellationToken cancellationToken)
    {
        termo = string.IsNullOrWhiteSpace(q) ? termo : q;
        if (string.IsNullOrWhiteSpace(termo)) return BadRequest("Informe o título ou autor do livro.");
        if (termo.Trim().Length > 150) return BadRequest("A pesquisa deve ter no máximo 150 caracteres.");

        try
        {
            var url = "https://www.googleapis.com/books/v1/volumes?maxResults=12&printType=books&q=" + Uri.EscapeDataString(termo.Trim());
            var apiKey = _configuration["GoogleBooks:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey)) url += "&key=" + Uri.EscapeDataString(apiKey);
            var client = _httpClientFactory.CreateClient("CatalogoExterno");
            using var response = await client.GetAsync(url, cancellationToken);
           if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    return StatusCode(StatusCodes.Status429TooManyRequests, new
    {
        mensagem = "O Google Books está temporariamente limitando as pesquisas. Aguarde alguns instantes e tente novamente."
    });
}

if (!response.IsSuccessStatusCode)
{
    return StatusCode(StatusCodes.Status502BadGateway, new
    {
        mensagem = $"O Google Books respondeu com HTTP {(int)response.StatusCode}."
    });
}

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return Ok(Array.Empty<LivroExterno>());

            var livros = new List<LivroExterno>();
            foreach (var item in items.EnumerateArray().Take(12))
            {
                var id = Texto(item, "id");
                if (string.IsNullOrWhiteSpace(id) || !item.TryGetProperty("volumeInfo", out var info)) continue;
                var titulo = Texto(info, "title") ?? "Título não informado";
                var autores = new List<string>();
                if (info.TryGetProperty("authors", out var authors) && authors.ValueKind == JsonValueKind.Array)
                    autores.AddRange(authors.EnumerateArray().Where(a => a.ValueKind == JsonValueKind.String).Select(a => a.GetString()!).Where(a => !string.IsNullOrWhiteSpace(a)));
                string? isbn = null;
                if (info.TryGetProperty("industryIdentifiers", out var identifiers) && identifiers.ValueKind == JsonValueKind.Array)
                {
                    foreach (var identifier in identifiers.EnumerateArray())
                    {
                        var tipo = Texto(identifier, "type");
                        var valor = Texto(identifier, "identifier");
                        if (tipo == "ISBN_13" && !string.IsNullOrWhiteSpace(valor)) { isbn = valor; break; }
                        if (tipo == "ISBN_10" && isbn is null) isbn = valor;
                    }
                }
                string? capa = null;
                if (info.TryGetProperty("imageLinks", out var imageLinks))
                    capa = Texto(imageLinks, "thumbnail") ?? Texto(imageLinks, "smallThumbnail");
                if (!string.IsNullOrWhiteSpace(capa)) capa = capa.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                livros.Add(new LivroExterno(id, titulo, autores.Count == 0 ? "Autor não informado" : string.Join(", ", autores),
                    Texto(info, "description"), isbn, capa, Texto(info, "publishedDate")));
            }
            return Ok(livros);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499, new { mensagem = "A pesquisa foi cancelada." });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { mensagem = "O Google Books demorou demais para responder." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "Não foi possível conectar ao Google Books. Tente novamente mais tarde." });
        }
        catch (JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "O Google Books retornou uma resposta inválida." });
        }
    }

    [HttpGet("filmes")]
    [HttpGet("~/api/Filme/buscar")]
    public async Task<IActionResult> BuscarFilmes([FromQuery] string? termo, [FromQuery] string? q, CancellationToken cancellationToken)
    {
        termo = string.IsNullOrWhiteSpace(q) ? termo : q;
        if (string.IsNullOrWhiteSpace(termo)) return BadRequest("Informe o título do filme.");
        if (termo.Trim().Length > 150) return BadRequest("A pesquisa deve ter no máximo 150 caracteres.");

        var token = _configuration["Tmdb:ReadAccessToken"];
        if (string.IsNullOrWhiteSpace(token))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensagem = "A integração TMDB ainda não foi configurada. Adicione o token de leitura em Tmdb:ReadAccessToken usando variável de ambiente; não coloque o token no JavaScript."
            });

        var client = _httpClientFactory.CreateClient("CatalogoExterno");

        try
        {
            var url = "https://api.themoviedb.org/3/search/movie?include_adult=false&language=pt-BR&page=1&query=" +
                      Uri.EscapeDataString(termo.Trim());
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = $"O TMDB respondeu com HTTP {(int)response.StatusCode}. Verifique se o token de leitura é válido e está ativo." });

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return Ok(Array.Empty<FilmeExterno>());

            var saida = new List<FilmeExterno>();
            foreach (var item in results.EnumerateArray().Take(12))
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                var titulo = Texto(item, "title") ?? Texto(item, "original_title") ?? "Filme sem título";
                var data = Texto(item, "release_date");
                int? ano = null;
                if (!string.IsNullOrWhiteSpace(data) && data.Length >= 4 && int.TryParse(data[..4], out var anoLido))
                    ano = anoLido;
                var posterPath = Texto(item, "poster_path");
                var poster = string.IsNullOrWhiteSpace(posterPath) ? null : "https://image.tmdb.org/t/p/w500" + posterPath;
                var diretor = await BuscarDiretor(client, token, id, cancellationToken);
                saida.Add(new FilmeExterno(id.ToString(), titulo, diretor, ano, poster));
            }
            return Ok(saida);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Problem("A pesquisa foi cancelada.", statusCode: 499);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { mensagem = "O TMDB demorou demais para responder." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "Não foi possível conectar ao TMDB. Tente novamente mais tarde." });
        }
        catch (JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "O TMDB retornou uma resposta inválida." });
        }
    }


    [HttpGet("series")]
    [HttpGet("~/api/Serie/buscar")]
    public async Task<IActionResult> BuscarSeries([FromQuery] string? termo, [FromQuery] string? q, CancellationToken cancellationToken)
    {
        termo = string.IsNullOrWhiteSpace(q) ? termo : q;
        if (string.IsNullOrWhiteSpace(termo)) return BadRequest("Informe o nome da série.");
        if (termo.Trim().Length > 150) return BadRequest("A pesquisa deve ter no máximo 150 caracteres.");
        try
        {
            var client = _httpClientFactory.CreateClient("CatalogoExterno");
            var url = "https://api.tvmaze.com/search/shows?q=" + Uri.EscapeDataString(termo.Trim());
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = $"O TVmaze respondeu com HTTP {(int)response.StatusCode}." });
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var saida = new List<SerieExterna>();
            foreach (var item in json.RootElement.EnumerateArray().Take(12))
            {
                if (!item.TryGetProperty("show", out var show)) continue;
                var id = show.TryGetProperty("id", out var idElement) ? idElement.ToString() : null;
                if (string.IsNullOrWhiteSpace(id)) continue;
                var titulo = Texto(show, "name") ?? "Série";
                var sinopseHtml = Texto(show, "summary") ?? "";
                var sinopse = System.Text.RegularExpressions.Regex.Replace(sinopseHtml, "<[^>]*>", " ");
                sinopse = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(sinopse, "\\s+", " ")).Trim();
                string? imagem = null;
                if (show.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.Object)
                    imagem = Texto(image, "medium") ?? Texto(image, "original");
                saida.Add(new SerieExterna("tvmaze:" + id, titulo, sinopse, imagem, Texto(show, "premiered")));
            }
            return Ok(saida);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return StatusCode(499, new { mensagem = "A pesquisa foi cancelada." }); }
        catch (OperationCanceledException) { return StatusCode(StatusCodes.Status504GatewayTimeout, new { mensagem = "O TVmaze demorou demais para responder." }); }
        catch (HttpRequestException) { return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "Não foi possível conectar ao TVmaze." }); }
        catch (JsonException) { return StatusCode(StatusCodes.Status502BadGateway, new { mensagem = "O TVmaze retornou uma resposta inválida." }); }
    }

    private static async Task<string?> BuscarDiretor(HttpClient client, string token, int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.themoviedb.org/3/movie/{id}/credits?language=pt-BR");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("crew", out var crew) || crew.ValueKind != JsonValueKind.Array) return null;
            foreach (var person in crew.EnumerateArray())
                if (string.Equals(Texto(person, "job"), "Director", StringComparison.OrdinalIgnoreCase))
                    return Texto(person, "name");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // O filme continua disponível mesmo se os créditos não puderem ser consultados.
        }
        return null;
    }

    private static string? Texto(JsonElement elemento, string propriedade) =>
        elemento.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
