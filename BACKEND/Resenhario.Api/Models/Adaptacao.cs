
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resenhario.Api.Models;

[Table("Adaptacoes")]
public class Adaptacao
{
    [Key]
    public int IdAdaptacoes { get; set; }

    public int IdLivro { get; set; }

    public int IdFilme { get; set; }

    public DateTime DataVinculo { get; set; }
}