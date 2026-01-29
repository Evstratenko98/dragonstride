public class GenerateFieldUseCase
{
    private readonly FieldMap _fieldMap;
    private readonly MazeGenerator _mazeGenerator;

    public GenerateFieldUseCase(FieldMap fieldMap, MazeGenerator mazeGenerator)
    {
        _fieldMap = fieldMap;
        _mazeGenerator = mazeGenerator;
    }

    public void Execute(int width, int height, float extraConnectionChance)
    {
        _fieldMap.Initialize(width, height);
        _mazeGenerator.Generate(_fieldMap, extraConnectionChance);
    }
}
