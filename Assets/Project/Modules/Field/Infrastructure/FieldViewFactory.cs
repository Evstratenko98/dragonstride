using UnityEngine;

public sealed class FieldViewFactory
{
    private readonly LinkView _linkViewPrefab;
    private readonly FogOfWarView _fogOfWarViewPrefab;
    private readonly FieldRoot _fieldRoot;

    private LinkView _linkViewInstance;
    private FogOfWarView _fogOfWarViewInstance;

    public FieldViewFactory(LinkView linkViewPrefab, FogOfWarView fogOfWarViewPrefab, FieldRoot fieldRoot)
    {
        _linkViewPrefab = linkViewPrefab;
        _fogOfWarViewPrefab = fogOfWarViewPrefab;
        _fieldRoot = fieldRoot;
    }

    public LinkView LinkView => _linkViewInstance ??= CreateLinkView();

    public FogOfWarView FogOfWarView => _fogOfWarViewInstance ??= CreateFogOfWarView();

    private LinkView CreateLinkView()
    {
        var root = _fieldRoot.EnsureRoot();
        return Object.Instantiate(_linkViewPrefab, root);
    }

    private FogOfWarView CreateFogOfWarView()
    {
        var root = _fieldRoot.EnsureRoot();
        return Object.Instantiate(_fogOfWarViewPrefab, root);
    }
}
