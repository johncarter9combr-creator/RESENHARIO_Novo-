using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Estantes")]
public class Estante
{
    [Key]
    public int IdEstante { get; set; }
    public int IdUsuario { get; set; }
    public int? IdLivros { get; set; }
    public int? IdFilme { get; set; }
    public int? IdSerie { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataAtualizacao { get; set; }
}
