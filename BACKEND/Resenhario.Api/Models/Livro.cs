
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Livros")]
public class Livro
{
    [Key]
    public int IdLivros { get; set; }

    public string? GoogleBId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string? Sinopse { get; set; }
    public string? Isbn { get; set; }
    public string? CapaUrl { get; set; }
}