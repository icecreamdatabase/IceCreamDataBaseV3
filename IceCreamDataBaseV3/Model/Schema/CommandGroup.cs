using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class CommandGroup
{
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public bool Enabled { get; set; } = true;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;
    
    public virtual List<CommandGroupLink> CommandGroupLinks { get; set; } = null!;
    
    public virtual List<Command> Commands { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandGroup>(entity =>
        {
            entity.Property(e => e.Enabled).HasDefaultValue(true);
        });
    }
}
