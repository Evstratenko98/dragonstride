public interface IFieldSnapshotService
{
    FieldGridSnapshot Capture(FieldGrid grid);
    FieldGrid Build(FieldGridSnapshot snapshot);
}
