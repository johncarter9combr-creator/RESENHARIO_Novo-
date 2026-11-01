using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Series")]
public class Serie
{
    [Key]
    public int IdSerie { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Sinopse { get; set; }
    public string? ImagemUrl { get; set; }
    public string? DataEstreia { get; set; }
    public string? FonteId { get; set; }
}
