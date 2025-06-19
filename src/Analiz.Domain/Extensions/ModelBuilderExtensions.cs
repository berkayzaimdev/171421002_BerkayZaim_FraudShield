using System.Linq.Expressions;
using System.Reflection;
using FraudShield.TransactionAnalysis.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Analiz.Domain.Extensions;

public static class ModelBuilderExtensions
{
    public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityType)
    {
        var methodToCall = typeof(ModelBuilderExtensions)
            .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(entityType.ClrType);

        var filter = methodToCall.Invoke(null, Array.Empty<object>());
        entityType.SetQueryFilter((LambdaExpression)filter);
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : class, ISoftDelete
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }
}