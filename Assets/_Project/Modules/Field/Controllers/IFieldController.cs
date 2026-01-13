public interface IFieldController
{
    ICellModel StartCellModel { get; }

    void CreateField();
    void Reset();
}
