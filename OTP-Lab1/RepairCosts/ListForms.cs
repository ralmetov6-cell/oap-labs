using Microsoft.Extensions.DependencyInjection;

namespace RepairCosts;

internal sealed class MainForm : Form
{
    public MainForm(IServiceScopeFactory scopes)
    {
        Text = "Учёт стоимости ремонтных работ";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 420);
        MinimumSize = new Size(640, 360);

        var catalogs = new ToolStripMenuItem("Справочники");
        catalogs.DropDownItems.Add("Помещения", null, (_, _) => Open<RoomListForm>());
        catalogs.DropDownItems.Add("Статьи затрат", null, (_, _) => Open<CostCategoryListForm>());
        catalogs.DropDownItems.Add("Подрядчики", null, (_, _) => Open<ContractorListForm>());

        var operations = new ToolStripMenuItem("Операции");
        operations.DropDownItems.Add("Ремонтные работы", null, (_, _) => Open<RepairWorkListForm>());

        MainMenuStrip = new MenuStrip();
        MainMenuStrip.Items.AddRange([catalogs, operations, new ToolStripMenuItem("Отчёты")]);
        Controls.Add(MainMenuStrip);

        void Open<TForm>() where TForm : Form
        {
            using var scope = scopes.CreateScope();
            scope.ServiceProvider.GetRequiredService<TForm>().ShowDialog(this);
        }
    }
}

internal abstract class GridForm : Form
{
    protected DataGridView Grid { get; } = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window
    };

    protected FlowLayoutPanel Actions { get; } = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        Padding = new Padding(0, 5, 0, 0)
    };

    protected GridForm(string title, params string[] columns)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(800, 440);
        MinimumSize = new Size(610, 320);
        foreach (string header in columns)
        {
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(Grid, 0, 0);
        layout.Controls.Add(Actions, 0, 1);
        Controls.Add(layout);
    }

    protected Button AddAction(string caption, EventHandler handler)
    {
        var button = new Button { Text = caption, AutoSize = true };
        button.Click += handler;
        Actions.Controls.Add(button);
        return button;
    }
}

internal abstract class CatalogListForm<T, TEditor> : GridForm
    where T : class, IEntity
    where TEditor : CatalogEditorForm<T>
{
    private readonly Button edit;
    private readonly Button delete;

    protected CatalogListForm(string title, ICatalogRepository<T> repository,
        IServiceScopeFactory scopes, Func<T, object[]> cells, params string[] columns)
        : base(title, columns)
    {
        void RefreshRows()
        {
            Grid.Rows.Clear();
            foreach (var item in repository.GetAll())
                Grid.Rows[Grid.Rows.Add(cells(item))].Tag = item;
            UpdateActions();
        }

        void OpenEditor(T? item)
        {
            using var scope = scopes.CreateScope();
            var editor = scope.ServiceProvider.GetRequiredService<TEditor>();
            if (item is not null)
                editor.LoadRecord(item);

            if (editor.ShowDialog(this) == DialogResult.OK)
                RefreshRows();
        }

        AddAction("Добавить", (_, _) => OpenEditor(null));
        edit = AddAction("Редактировать", (_, _) =>
        {
            if (Selected() is { } item)
                OpenEditor(item);
        });
        delete = AddAction("Удалить", (_, _) =>
        {
            if (Selected() is not { } item ||
                MessageBox.Show(this, "Удалить выбранную запись?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                repository.Delete(item.Id);
                RefreshRows();
            }
            catch (InvalidOperationException error)
            {
                MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
        edit.Enabled = delete.Enabled = false;
        Grid.SelectionChanged += (_, _) => UpdateActions();
        Shown += (_, _) => RefreshRows();

    }

    private T? Selected() => Grid.CurrentRow?.Tag as T;

    private void UpdateActions() => edit.Enabled = delete.Enabled = Selected() is not null;
}

internal sealed class RoomListForm(IRoomRepository repository, IServiceScopeFactory scopes)
    : CatalogListForm<Room, RoomEditorForm>("Помещения", repository, scopes,
        room => [room.Address, room.Area], "Адрес", "Площадь, м²");

internal sealed class CostCategoryListForm(ICostCategoryRepository repository, IServiceScopeFactory scopes)
    : CatalogListForm<CostCategory, CostCategoryEditorForm>("Статьи затрат", repository, scopes,
        category => [category.Name, category.Unit.ToLabel()], "Название", "Единица измерения");

internal sealed class ContractorListForm(IContractorRepository repository, IServiceScopeFactory scopes)
    : CatalogListForm<Contractor, ContractorEditorForm>("Подрядчики", repository, scopes,
        contractor => [contractor.Name], "Название");

internal sealed class RepairWorkListForm : GridForm
{
    public RepairWorkListForm(IRepairWorkRepository repository, IServiceScopeFactory scopes)
        : base("Ремонтные работы", "Дата", "Помещение", "Статья затрат", "Подрядчик", "Сумма, ₽")
    {
        void RefreshRows()
        {
            Grid.Rows.Clear();
            foreach (var work in repository.GetAll().OrderByDescending(work => work.Date))
            {
                Grid.Rows.Add(work.Date.ToString("dd.MM.yyyy"), work.Room.Address,
                    work.CostCategory.Name, work.Contractor?.Name ?? "Своими силами", work.Amount.ToString("N2"));
            }
        }

        AddAction("Добавить", (_, _) =>
        {
            using var scope = scopes.CreateScope();
            if (scope.ServiceProvider.GetRequiredService<RepairWorkEditorForm>().ShowDialog(this) == DialogResult.OK)
                RefreshRows();
        });
        Shown += (_, _) => RefreshRows();
    }
}
