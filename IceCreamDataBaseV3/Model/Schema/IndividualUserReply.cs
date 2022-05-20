using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.DataAnnotations;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class IndividualUserReply
{
    [Required]
    public int BotUserId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [ForeignKey($"{nameof(BotUserId)},{nameof(RoomId)}")]
    public virtual Channel Channel { get; set; } = null!;

    [Required]
    public int TriggerUserId { get; set; }

    [Required]
    public bool Enabled { get; set; } = true;

    [Required]
    [MaxLength(255)]
    [MySqlCharset("utf8mb4")]
    public string TriggerPhrase { get; set; } = null!;

    [Required]
    [MySqlCharset("utf8mb4")]
    public string Response { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndividualUserReply>(entity =>
        {
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.HasKey(nameof(BotUserId), nameof(RoomId), nameof(TriggerUserId), nameof(TriggerPhrase));
        });
    }
}
