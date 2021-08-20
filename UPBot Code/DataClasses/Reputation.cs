using System;
using System.ComponentModel.DataAnnotations;

public class Reputation {
  [Key]
  [Required]
  public ulong User { get; set; }
  [Required]
  public ushort Rep { get; set; }
  [Required]
  public ushort Fun { get; set; }
  [Required]
  public ushort Tnk { get; set; }
  [Required]
  public DateTime DateAdded { get; set; }
}


