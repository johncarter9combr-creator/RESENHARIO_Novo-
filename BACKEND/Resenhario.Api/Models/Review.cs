
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    public int IdReviews { get; set; }

    public int IdUsuario { get; set; }
    public int? IdLivro { get; set; }
    public int? IdAdaptacao { get; set; }
    public int? NotaLivro { get; set; }
    public string? Comentario { get; set; }
    public bool AssistiuFilme { get; set; }
    public int? NotaFidelidade { get; set; }
    public string? Voto { get; set; }
    public DateTime DataCriacao { get; set; }
}