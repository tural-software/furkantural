namespace FurkanTural_Application.DTOs.Common;

/// <summary>Toplu işlem türleri. Aktiflik için toggle yoktur: karışık bir seçimde "tersine çevir" belirsiz olurdu, bu yüzden yön açıkça söylenir.</summary>
public enum BulkAction
{
    Delete,
    Deactivate,
    Activate,
    Restore
}
