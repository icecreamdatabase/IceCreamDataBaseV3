using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.DataAnnotations;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Command
{
    [Key]
    [Required]
    public int Id { get; set; }
    
    [Required]
    public int CommandGroupId { get; set; }

    [Required]
    public bool Enabled { get; set; } = true;

    [Required]
    public bool IsRegex { get; set; } = false;

    [Required]
    public bool ShouldReply { get; set; } = false;

    [Required]
    [MaxLength(255)]
    [MySQLCharset("utf8mb4")]
    public string TriggerPhrase { get; set; } = null!;

    [Required]
    [MySQLCharset("utf8mb4")]
    public string Response { get; set; } = null!;

    [Required]
    public int CooldownSeconds { get; set; } = 5;
    
    [Required]
    public int TimesUsed { get; set; } = 0;

    [Required]
    public bool TriggerNormal { get; set; } = true;

    [Required]
    public bool TriggerSubs { get; set; } = true;

    [Required]
    public bool TriggerVips { get; set; } = true;

    [Required]
    public bool TriggerMods { get; set; } = true;

    [Required]
    public bool TriggerBroadcaster { get; set; } = true;

    [Required]
    public bool TriggerBotAdmin { get; set; } = true;

    [Required]
    public bool TriggerBotOwner { get; set; } = true;

    [ForeignKey(nameof(CommandGroupId))]
    public virtual CommandGroup CommandGroup { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Command>(entity =>
        {
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.IsRegex).HasDefaultValue(false);
            entity.Property(e => e.ShouldReply).HasDefaultValue(false);
            entity.Property(e => e.CooldownSeconds).HasDefaultValue(5);
            entity.Property(e => e.TimesUsed).HasDefaultValue(0);
            entity.Property(e => e.TriggerNormal).HasDefaultValue(true);
            entity.Property(e => e.TriggerSubs).HasDefaultValue(true);
            entity.Property(e => e.TriggerVips).HasDefaultValue(true);
            entity.Property(e => e.TriggerMods).HasDefaultValue(true);
            entity.Property(e => e.TriggerBroadcaster).HasDefaultValue(true);
            entity.Property(e => e.TriggerBotAdmin).HasDefaultValue(true);
            entity.Property(e => e.TriggerBotOwner).HasDefaultValue(true);
        });
    }
}
