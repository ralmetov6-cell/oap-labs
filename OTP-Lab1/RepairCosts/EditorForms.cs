using System.Globalization;

namespace RepairCosts;

internal abstract class EditorForm : Form
{
    private readonly TableLayoutPanel fields = new()
    {
        AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 0
    };

    private readonly Label validation = new()
    {
        AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Color.DarkRed
    };

    protected EditorForm(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(455, 180);
        MinimumSize = new Size(465, 0);
        Padding = new Padding(14);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var save = new Button { Text = "Сохранить", AutoSize = true };
        var cancel = new Button { Text = "Отмена", AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        buttons.Controls.AddRange([cancel, save]);

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(fields, 0, 0);
        layout.Controls.Add(validation, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            try
            {
                SaveRecord();
                DialogResult = DialogResult.OK;
            }
            catch (ArgumentException error)
            {
                validation.Text = error.Message;
            }
        };
    }

    protected void AddField(string caption, Control input)
    {
        int row = fields.RowCount++;
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left };
        input.AccessibleName = caption.Replace("&", "", StringComparison.Ordinal);
        input.Dock = DockStyle.Fill;
        fields.Controls.Add(label, 0, row);
        fields.Controls.Add(input, 1, row);
    }

    protected static ComboBox CreatePicker() => new() { DropDownStyle = ComboBoxStyle.DropDownList };

    protected static decimal ReadDecimal(TextBox input, string message)
    {
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        string value = input.Text.Trim().Replace(',', '.');
        return decimal.TryParse(value, style, CultureInfo.InvariantCulture, out decimal result)
            ? result
            : throw new ArgumentException(message);
    }

    protected abstract void SaveRecord();
}

internal abstract class CatalogEditorForm<T>(string title, ICatalogRepository<T> repository) : EditorForm(title)
    where T : class, IEntity
{
    private T? record;

    public void LoadRecord(T item)
    {
        record = item;
        Text = "Редактирование записи";
        Populate(item);
    }

    protected sealed override void SaveRecord()
    {
        if (record is null)
        {
            repository.Add(CreateRecord());
            return;
        }

        EditRecord(record);
        repository.Update(record);
    }

    protected abstract void Populate(T item);
    protected abstract T CreateRecord();
    protected abstract void EditRecord(T item);
}

internal sealed class RoomEditorForm : CatalogEditorForm<Room>
{
    private readonly TextBox address = new();
    private readonly TextBox area = new();

    public RoomEditorForm(IRoomRepository repository) : base("Добавление помещения", repository)
    {
        AddField("&Адрес", address);
        AddField("&Площадь, м²", area);
    }

    protected override void Populate(Room room)
    {
        address.Text = room.Address;
        area.Text = room.Area.ToString(CultureInfo.CurrentCulture);
    }

    protected override Room CreateRecord() => Room.Create(address.Text, ReadArea());

    protected override void EditRecord(Room room) => room.Edit(address.Text, ReadArea());

    private decimal ReadArea() => ReadDecimal(area, "Укажи площадь числом, например 24,5.");
}

internal sealed class CostCategoryEditorForm : CatalogEditorForm<CostCategory>
{
    private readonly TextBox name = new();
    private readonly ComboBox unit = CreatePicker();

    public CostCategoryEditorForm(ICostCategoryRepository repository)
        : base("Добавление статьи затрат", repository)
    {
        unit.Items.AddRange(Enum.GetValues<MeasurementUnit>().Select(value => new UnitOption(value)).ToArray());
        AddField("&Название", name);
        AddField("&Единица измерения", unit);
    }

    protected override void Populate(CostCategory category)
    {
        name.Text = category.Name;
        unit.SelectedItem = new UnitOption(category.Unit);
    }

    protected override CostCategory CreateRecord() => CostCategory.Create(name.Text, ReadUnit());

    protected override void EditRecord(CostCategory category) => category.Edit(name.Text, ReadUnit());

    private MeasurementUnit ReadUnit() => unit.SelectedItem is UnitOption option
        ? option.Unit
        : throw new ArgumentException("Выбери единицу измерения.");

    private sealed record UnitOption(MeasurementUnit Unit)
    {
        public override string ToString() => Unit.ToLabel();
    }
}

internal sealed class ContractorEditorForm : CatalogEditorForm<Contractor>
{
    private readonly TextBox name = new();

    public ContractorEditorForm(IContractorRepository repository) : base("Добавление подрядчика", repository)
    {
        AddField("&Название", name);
    }

    protected override void Populate(Contractor contractor) => name.Text = contractor.Name;

    protected override Contractor CreateRecord() => Contractor.Create(name.Text);

    protected override void EditRecord(Contractor contractor) => contractor.Edit(name.Text);
}

internal sealed class RepairWorkEditorForm : EditorForm
{
    private readonly IRepairWorkRepository works;
    private readonly ComboBox room = CreatePicker();
    private readonly ComboBox category = CreatePicker();
    private readonly ComboBox contractor = CreatePicker();
    private readonly TextBox amount = new();
    private readonly DateTimePicker date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy" };

    public RepairWorkEditorForm(IRepairWorkRepository works, IRoomRepository rooms,
        ICostCategoryRepository categories, IContractorRepository contractors)
        : base("Добавление ремонтной работы")
    {
        this.works = works;
        room.Items.AddRange(rooms.GetAll().ToArray());
        category.Items.AddRange(categories.GetAll().ToArray());
        contractor.Items.Add("Своими силами");
        contractor.Items.AddRange(contractors.GetAll().ToArray());
        contractor.SelectedIndex = 0;

        AddField("&Помещение", room);
        AddField("Ст&атья затрат", category);
        AddField("По&дрядчик", contractor);
        AddField("&Сумма, ₽", amount);
        AddField("Да&та", date);
    }

    protected override void SaveRecord()
    {
        var selectedRoom = room.SelectedItem as Room ?? throw new ArgumentException("Выбери помещение.");
        var selectedCategory = category.SelectedItem as CostCategory
            ?? throw new ArgumentException("Выбери статью затрат.");
        decimal cost = ReadDecimal(amount, "Укажи сумму числом, например 1500,50.");
        works.Add(RepairWork.Create(selectedRoom, selectedCategory, contractor.SelectedItem as Contractor,
            cost, DateOnly.FromDateTime(date.Value)));
    }
}

internal static class MeasurementUnitLabels
{
    public static string ToLabel(this MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Piece => "шт.",
        MeasurementUnit.Meter => "м",
        MeasurementUnit.SquareMeter => "м²",
        MeasurementUnit.Hour => "ч",
        MeasurementUnit.Service => "услуга",
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}
