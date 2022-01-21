using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class CommandGroupLink
{
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Required]
    public bool Enabled { get; set; } = true;

    public virtual Channel Channel { get; set; } = null!;
    
    public virtual CommandGroup CommandGroup { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandGroupLink>(entity =>
        {
            entity.Property(e => e.Enabled).HasDefaultValue(true);
        });
    }
}
