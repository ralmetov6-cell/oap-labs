namespace RepairCosts;

public enum MeasurementUnit
{
    Piece,
    Meter,
    SquareMeter,
    Hour,
    Service
}

[Flags]
public enum EstimateDimensions
{
    None = 0,
    Room = 1,
    CostCategory = 2,
    Contractor = 4,
    All = Room | CostCategory | Contractor
}

public interface IEntity
{
    Guid Id { get; }
}

public sealed class Room : IEntity
{
    public Guid Id { get; private set; }
    public string Address { get; private set; }
    public decimal Area { get; private set; }

    private Room(string address, decimal area)
    {
        Id = Guid.NewGuid();
        Address = RequiredAddress(address);
        Area = RequiredArea(area);
    }

    public static Room Create(string address, decimal area) => new(address, area);

    public void Edit(string address, decimal area)
    {
        var checkedAddress = RequiredAddress(address);
        var checkedArea = RequiredArea(area);
        Address = checkedAddress;
        Area = checkedArea;
    }

    private static string RequiredAddress(string value) =>
        ModelValidation.RequiredText(value, "Укажи адрес помещения.");

    private static decimal RequiredArea(decimal value) =>
        ModelValidation.Positive(value, "Площадь должна быть больше нуля.");

    public override string ToString() => Address;
}

public sealed class CostCategory : IEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public MeasurementUnit Unit { get; private set; }

    private CostCategory(string name, MeasurementUnit unit)
    {
        Id = Guid.NewGuid();
        Name = RequiredName(name);
        Unit = RequiredUnit(unit);
    }

    public static CostCategory Create(string name, MeasurementUnit unit) => new(name, unit);

    public void Edit(string name, MeasurementUnit unit)
    {
        var checkedName = RequiredName(name);
        var checkedUnit = RequiredUnit(unit);
        Name = checkedName;
        Unit = checkedUnit;
    }

    private static string RequiredName(string value) =>
        ModelValidation.RequiredText(value, "Укажи название статьи затрат.");

    private static MeasurementUnit RequiredUnit(MeasurementUnit unit) =>
        Enum.IsDefined(unit) ? unit : throw new ArgumentException("Выбери единицу измерения.");

    public override string ToString() => Name;
}

public sealed class Contractor : IEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Contractor(string name)
    {
        Id = Guid.NewGuid();
        Name = RequiredName(name);
    }

    public static Contractor Create(string name) => new(name);

    public void Edit(string name) => Name = RequiredName(name);

    private static string RequiredName(string value) =>
        ModelValidation.RequiredText(value, "Укажи название подрядчика.");

    public override string ToString() => Name;
}

public sealed class RepairWork : IEntity
{
    public Guid Id { get; private set; }
    public Room Room { get; private set; }
    public CostCategory CostCategory { get; private set; }
    public Contractor? Contractor { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }

    private RepairWork(Room room, CostCategory costCategory, Contractor? contractor,
        decimal amount, DateOnly date)
    {
        Id = Guid.NewGuid();
        Room = room ?? throw new ArgumentException("Выбери помещение.");
        CostCategory = costCategory ?? throw new ArgumentException("Выбери статью затрат.");
        Contractor = contractor;
        Amount = ModelValidation.Positive(amount, "Сумма должна быть больше нуля.");
        Date = date;
    }

    public static RepairWork Create(Room room, CostCategory costCategory, Contractor? contractor,
        decimal amount, DateOnly date) => new(room, costCategory, contractor, amount, date);
}

internal static class ModelValidation
{
    public static string RequiredText(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();

    public static decimal Positive(decimal value, string message) =>
        value > 0 ? value : throw new ArgumentException(message);
}
