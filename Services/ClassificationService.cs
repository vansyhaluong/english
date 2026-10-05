using System.Data;
using English.Data;
using English.Interfaces;
using English.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class ClassificationService(ApplicationDbContext context) : IClassificationService
{
    public async Task<IReadOnlyList<ClassificationSummary>> GetAllAsync(ClassificationKind kind, CancellationToken cancellationToken)
    {
        return kind switch
        {
            ClassificationKind.Topic => await context.Topics.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Select(x => new ClassificationSummary(x.Id, x.Name, x.Description)).ToArrayAsync(cancellationToken),
            ClassificationKind.GrammarGroup => await context.GrammarGroups.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Select(x => new ClassificationSummary(x.Id, x.Name, x.Description)).ToArrayAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public Task<ClassificationDetails?> GetAsync(ClassificationKind kind, int id, CancellationToken cancellationToken)
    {
        return kind switch
        {
            ClassificationKind.Topic => context.Topics.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ClassificationDetails(x.Id, x.Name, x.Description, x.RowVersion)).SingleOrDefaultAsync(cancellationToken),
            ClassificationKind.GrammarGroup => context.GrammarGroups.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ClassificationDetails(x.Id, x.Name, x.Description, x.RowVersion)).SingleOrDefaultAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<IReadOnlyList<LevelSummary>> GetLevelsAsync(CancellationToken cancellationToken)
    {
        return await context.Levels.AsNoTracking().OrderBy(x => x.SortOrder)
            .Select(x => new LevelSummary(x.Code, x.SortOrder)).ToArrayAsync(cancellationToken);
    }

    public async Task<ClassificationResult> CreateAsync(ClassificationKind kind, string name, string? description, CancellationToken cancellationToken)
    {
        if (!IsSupported(kind) || !ValidName(name))
            return ClassificationResult.InvalidInput;

        if (kind == ClassificationKind.Topic)
            context.Topics.Add(new Topic { Name = name.Trim(), Description = NormalizeDescription(description) });
        else
            context.GrammarGroups.Add(new GrammarGroup { Name = name.Trim(), Description = NormalizeDescription(description) });

        await context.SaveChangesAsync(cancellationToken);
        return ClassificationResult.Success;
    }

    public async Task<ClassificationResult> EditAsync(ClassificationKind kind, int id, string name, string? description, byte[] rowVersion, CancellationToken cancellationToken)
    {
        if (!IsSupported(kind) || id <= 0 || !ValidName(name) || rowVersion.Length != 8)
            return ClassificationResult.InvalidInput;

        if (kind == ClassificationKind.Topic)
        {
            var topic = await context.Topics.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (topic is null) return ClassificationResult.NotFound;
            if (!topic.RowVersion.SequenceEqual(rowVersion)) return ClassificationResult.Conflict;
            topic.Name = name.Trim();
            topic.Description = NormalizeDescription(description);
            context.Entry(topic).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }
        else
        {
            var group = await context.GrammarGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (group is null) return ClassificationResult.NotFound;
            if (!group.RowVersion.SequenceEqual(rowVersion)) return ClassificationResult.Conflict;
            group.Name = name.Trim();
            group.Description = NormalizeDescription(description);
            context.Entry(group).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return ClassificationResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ClassificationResult.Conflict;
        }
        catch (Exception exception) when (IsDeadlock(exception))
        {
            return ClassificationResult.Conflict;
        }
    }

    public async Task<ClassificationResult> DeleteAsync(ClassificationKind kind, int id, byte[] rowVersion, CancellationToken cancellationToken)
    {
        if (!IsSupported(kind) || id <= 0 || rowVersion.Length != 8)
            return ClassificationResult.InvalidInput;

        try
        {
            return await DeleteCoreAsync(kind, id, rowVersion, cancellationToken);
        }
        catch (Exception exception) when (IsDeadlock(exception))
        {
            return ClassificationResult.Conflict;
        }
    }

    private async Task<ClassificationResult> DeleteCoreAsync(ClassificationKind kind, int id, byte[] rowVersion, CancellationToken cancellationToken)
    {
        // Keep the reference check and deletion together; concurrent FK inserts must wait.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (kind == ClassificationKind.Topic)
        {
            var topic = await context.Topics.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (topic is null) return ClassificationResult.NotFound;
            if (!topic.RowVersion.SequenceEqual(rowVersion)) return ClassificationResult.Conflict;
            if (await context.LearningItems.AnyAsync(x => x.TopicId == id, cancellationToken) ||
                await context.Books.AnyAsync(x => x.TopicId == id, cancellationToken) ||
                await context.Exercises.AnyAsync(x => x.VocabularyTopicId == id, cancellationToken))
                return ClassificationResult.Referenced;
            context.Entry(topic).Property(x => x.RowVersion).OriginalValue = rowVersion;
            context.Topics.Remove(topic);
        }
        else
        {
            var group = await context.GrammarGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (group is null) return ClassificationResult.NotFound;
            if (!group.RowVersion.SequenceEqual(rowVersion)) return ClassificationResult.Conflict;
            if (await context.LearningItems.AnyAsync(x => x.GrammarGroupId == id, cancellationToken))
                return ClassificationResult.Referenced;
            context.Entry(group).Property(x => x.RowVersion).OriginalValue = rowVersion;
            context.GrammarGroups.Remove(group);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ClassificationResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ClassificationResult.Conflict;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            // FK constraints remain the final boundary if another reference is introduced.
            return ClassificationResult.Referenced;
        }
    }

    private static bool IsSupported(ClassificationKind kind) => kind is ClassificationKind.Topic or ClassificationKind.GrammarGroup;
    private static bool IsDeadlock(Exception exception)
    {
        // The SQL Server execution strategy may wrap a deadlock in InvalidOperationException.
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException { Number: 1205 }) return true;
        }
        return false;
    }

    private static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 200;
    private static string? NormalizeDescription(string? description) => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
