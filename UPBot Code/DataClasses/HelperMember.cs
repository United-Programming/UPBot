using System;
using System.ComponentModel.DataAnnotations;
/// <summary>
/// HelperMember entity
/// </summary>
public class HelperMember {
  [Key]
  public ulong Id { get; set; }
  [Required]
  [MaxLength(128)]
  public string Name { get; set; }
  [Required]
  public DateTime DateAdded { get; set; }
}