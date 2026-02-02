using UnityEngine;

public class FieldViewService
{
    private readonly LinkView _linkViewPrefab;
    private readonly FogOfWarView _fogOfWarViewPrefab;
    private readonly FieldRootService _fieldRootService;

    private LinkView _linkViewInstance;
    private FogOfWarView _fogOfWarViewInstance;

    public FieldViewService(
        LinkView linkViewPrefab,
        FogOfWarView fogOfWarViewPrefab,
        FieldRootService fieldRootService)
    {
        _linkViewPrefab = linkViewPrefab;
        _fogOfWarViewPrefab = fogOfWarViewPrefab;
        _fieldRootService = fieldRootService;
    }

    public LinkView LinkView => _linkViewInstance ??= CreateLinkView();

    public FogOfWarView FogOfWarView => _fogOfWarViewInstance ??= CreateFogOfWarView();

    private LinkView CreateLinkView()
    {
        Transform root = _fieldRootService.EnsureRoot();
        return Object.Instantiate(_linkViewPrefab, root);
    }

    private FogOfWarView CreateFogOfWarView()
    {
        Transform root = _fieldRootService.EnsureRoot();
        return Object.Instantiate(_fogOfWarViewPrefab, root);
    }
}
