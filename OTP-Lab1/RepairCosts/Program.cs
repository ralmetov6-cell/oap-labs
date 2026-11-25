using Microsoft.Extensions.DependencyInjection;

namespace RepairCosts;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var container = Bootstrapper.BuildContainer();
        Application.Run(container.GetRequiredService<MainForm>());
    }
}

internal static class Bootstrapper
{
    public static ServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRoomRepository, RoomRepositoryStub>();
        services.AddSingleton<ICostCategoryRepository, CostCategoryRepositoryStub>();
        services.AddSingleton<IContractorRepository, ContractorRepositoryStub>();
        services.AddSingleton<IRepairWorkRepository, RepairWorkRepositoryStub>();
        services.AddTransient<MainForm>();
        services.AddTransient<RoomListForm>();
        services.AddTransient<CostCategoryListForm>();
        services.AddTransient<ContractorListForm>();
        services.AddTransient<RepairWorkListForm>();
        services.AddTransient<RoomEditorForm>();
        services.AddTransient<CostCategoryEditorForm>();
        services.AddTransient<ContractorEditorForm>();
        services.AddTransient<RepairWorkEditorForm>();
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
