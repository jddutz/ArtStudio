namespace ArtStudio.Core;

public interface ILayoutManager
{
    void SaveLayout(string layoutName);
    void LoadLayout(string layoutName);
    void ResetToDefault();
}
