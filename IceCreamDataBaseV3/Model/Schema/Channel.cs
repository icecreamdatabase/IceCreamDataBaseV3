using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Channel
{
    [Required]
    public int BotUserId { get; set; }
    
    [Required]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(25)]
    public string ChannelName { get; set; } = null!;

    [Required]
    public bool Enabled { get; set; } = true;

    [Required]
    public int MaxIrcMessageLength { get; set; } = 450;

    public virtual List<CommandGroupLink> CommandGroupLinks { get; set; } = null!;
    
    public virtual List<UserNoticeResponse> UserNoticeResponses { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(nameof(BotUserId), nameof(RoomId));
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.MaxIrcMessageLength).HasDefaultValue(450);
        });
    }
}
