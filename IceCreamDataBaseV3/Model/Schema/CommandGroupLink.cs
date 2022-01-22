﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public int CommandGroupId { get; set; }

    [Required]
    public int BotUserId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;
    
    [ForeignKey(nameof(CommandGroupId))]
    public virtual CommandGroup CommandGroup { get; set; } = null!;

    [ForeignKey($"{nameof(BotUserId)},{nameof(RoomId)}")]
    public virtual Channel Channel { get; set; } = null!;

    protected internal static void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandGroupLink>(entity =>
        {
        });
    }
}
