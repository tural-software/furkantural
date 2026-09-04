namespace FurkanTural_Application.DTOs.Common;

/// <summary>Açılır liste sözlüğünün tek satırı: kimlik ve gösterilecek etiket. Yönetici sözlüğüdür; pasif kayıt da gelir, silinmiş gelmez. Sayfa değil sözlük olduğu için take verilmezse tümü döner; ağırlığı satır başına iki alandır.</summary>
public sealed record AdminOptionDto(int Id, string Label);
