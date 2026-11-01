using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Filmes")]
public class Filme
{
    [Key]
    public int IdFilme { get; set; }
    public string? TmdbId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Diretor { get; set; }
    public int? AnoLancamento { get; set; }
    public string? PosterUrl { get; set; }
}
