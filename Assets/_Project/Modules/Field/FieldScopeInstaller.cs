using UnityEngine;
using VContainer;
using VContainer.Unity;

public class FieldScopeInstaller : MonoBehaviour
{
    [SerializeField] private CellView cellViewPrefab;
    [SerializeField] private LinkView linkView;
    [SerializeField] private CellColorTheme colorTheme;

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterInstance(colorTheme);
        builder.RegisterComponent(cellViewPrefab);
        builder.RegisterComponent(linkView).As<ILinkView>();
        builder.Register<CellModel>(Lifetime.Transient);
        builder.Register<LinkModel>(Lifetime.Transient);
        builder.Register<IFieldService, FieldService>(Lifetime.Singleton);
        builder.Register<IMazeGenerator, MazeGenerator>(Lifetime.Singleton);
        builder.RegisterEntryPoint<FieldController>().As<IFieldController>();
    }
}
