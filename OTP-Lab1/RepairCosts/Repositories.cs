namespace RepairCosts;

public interface ICatalogRepository<T> where T : class, IEntity
{
    IReadOnlyList<T> GetAll();
    T? GetById(Guid id);
    void Add(T item);
    void Update(T item);
    void Delete(Guid id);
}

public interface IRoomRepository : ICatalogRepository<Room> { }

public interface ICostCategoryRepository : ICatalogRepository<CostCategory> { }

public interface IContractorRepository : ICatalogRepository<Contractor> { }

public interface IRepairWorkRepository
{
    IReadOnlyList<RepairWork> GetAll();
    void Add(RepairWork work);
}

internal abstract class CatalogRepositoryStub<T>(IRepairWorkRepository works) : ICatalogRepository<T>
    where T : class, IEntity
{
    private readonly Dictionary<Guid, T> items = [];

    public IReadOnlyList<T> GetAll() => items.Values.ToArray();

    public T? GetById(Guid id) => items.GetValueOrDefault(id);

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        items.Add(item.Id, item);
    }

    public void Update(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!items.ContainsKey(item.Id))
            throw new InvalidOperationException("Запись отсутствует в справочнике.");

        items[item.Id] = item;
    }

    public void Delete(Guid id)
    {
        if (works.GetAll().Any(work => IsReferenced(work, id)))
            throw new InvalidOperationException("Эта запись используется в ремонтных работах.");

        items.Remove(id);
    }

    protected abstract bool IsReferenced(RepairWork work, Guid id);
}

internal sealed class RoomRepositoryStub(IRepairWorkRepository works)
    : CatalogRepositoryStub<Room>(works), IRoomRepository
{
    protected override bool IsReferenced(RepairWork work, Guid id) => work.Room.Id == id;
}

internal sealed class CostCategoryRepositoryStub(IRepairWorkRepository works)
    : CatalogRepositoryStub<CostCategory>(works), ICostCategoryRepository
{
    protected override bool IsReferenced(RepairWork work, Guid id) => work.CostCategory.Id == id;
}

internal sealed class ContractorRepositoryStub(IRepairWorkRepository works)
    : CatalogRepositoryStub<Contractor>(works), IContractorRepository
{
    protected override bool IsReferenced(RepairWork work, Guid id) => work.Contractor?.Id == id;
}

internal sealed class RepairWorkRepositoryStub : IRepairWorkRepository
{
    private readonly List<RepairWork> items = [];

    public IReadOnlyList<RepairWork> GetAll() => items.ToArray();

    public void Add(RepairWork work)
    {
        ArgumentNullException.ThrowIfNull(work);
        items.Add(work);
    }
}
