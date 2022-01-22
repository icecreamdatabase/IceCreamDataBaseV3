using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;

namespace IceCreamDataBaseV3.Model.Schema;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class UserNoticeResponse
{
    [Required]
    public int BotUserId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [ForeignKey($"{nameof(BotUserId)},{nameof(RoomId)}")]
    public virtual Channel Channel { get; set; } = null!;

    [Required]
    public UserNoticeMessageId MessageId { get; set; }

    [Required]
    public string Response { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNoticeResponse>(entity =>
        {
            entity.HasKey(nameof(BotUserId), nameof(RoomId), nameof(MessageId));
            entity.Property(e => e.MessageId).HasConversion(
                e => e.ToString(),
                s => Enum.Parse<UserNoticeMessageId>(s, true)
            );
        });
    }
}
