using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Business.Helpers;

/// <summary>Yönetici süzgeçlerini EF'in SQL'e çevirebileceği yüklemlere dönüştürür. Yüklemler Expression.Invoke ile değil parametre değiştirerek birleştirilir; Invoke sağlayıcı tarafından çevrilemez ve sorguyu belleğe düşürürdü. Common yalnızca ortak omurgayı kurar (aktiflik, silinmişlik, oluşturulma aralığı); metin araması modülün kendi sütunlarını bildiği yerde, servisinde eklenir. Hiçbir süzgeç verilmediğinde null döner ve depo bütün tabloyu sayfalar.</summary>
public static class AdminFilters
{
    public static Expression<Func<T, bool>>? Common<T>(AdminListQuery query) where T : BaseEntity
    {
        Expression<Func<T, bool>>? predicate = null;

        if (query.IsActive is { } active)
            predicate = predicate.AndAlso(x => x.IsActive == active);

        if (query.IsDeleted is { } deleted)
            predicate = predicate.AndAlso(x => x.IsDeleted == deleted);

        if (query.DateFrom is { } from)
            predicate = predicate.AndAlso(x => x.CreatedAt >= from);

        if (query.DateToExclusive is { } to)
            predicate = predicate.AndAlso(x => x.CreatedAt < to);

        return predicate;
    }

    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>>? left, Expression<Func<T, bool>> right)
        => left is null ? right : Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>>? left, Expression<Func<T, bool>> right)
        => left is null ? right : Combine(left, right, Expression.OrElse);

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
