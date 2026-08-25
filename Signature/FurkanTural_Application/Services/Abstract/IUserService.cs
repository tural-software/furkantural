using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Kullanıcı CRUD'una ek olarak kimlik ve profil işlemleri. SeedAdminAsync yalnızca tablo tamamen boşken çalışır, tek bir kullanıcı bile varsa 409 döner: ilk kurulum içindir, admin eklemek için değil. SearchAsync en az iki karakter ister, sonucu kırpar ve iki yönden herhangi biri engellenmiş kullanıcıyı listeden düşürür. UpdateAvatarAsync'e adres değil dosya adı verilir. UpdateLastSeenAsync tek istisnadır: <see cref="Wrappers.Result"/> zarfı kullanmaz, kullanıcı bulunamasa bile hata üretmeden o anki UTC değerini döndürür — her istekte çağrıldığı için sessiz kalması istenir.<para>DeactivateMyAccountAsync tek yönlüdür ve yalnızca kullanıcının kendi hesabında çalışır: kapatır, açmaz. Geri açmak yalnızca posta doğrulamasıyla olur (bkz. <see cref="IAccountActivationService"/>), çünkü hesabı kapatan kişinin kendisi olduğunu gösteren şey oturum değil posta kutusudur. Oturumun yanında parola da istenir. Okuma küresel süzgeçten geçer, dolayısıyla zaten kapalı ya da silinmiş bir hesap bulunamaz ve ikinci bir kapatma sessizce başarılı görünmez.</para><para>ToggleActiveAsync ile karıştırılmamalı: o admin ucudur, iki yöne de çalışır ve parola sormaz.</para></summary>
public interface IUserService : IService<UserDto, CreateUserDto, UpdateUserDto>
{
    Task<Result<UserDto>> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> SeedAdminAsync(string? username, string? password, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserSearchResultDto>>> SearchAsync(string query, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAvatarAsync(int userId, string fileName, int? updatedBy, CancellationToken cancellationToken = default);
    Task<DateTime> UpdateLastSeenAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> AcceptAgreementAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateMyAccountAsync(int userId, string? password, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminUserDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
