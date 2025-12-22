using DiffMatchPatch; // 引入命名空间
using System.Collections.Concurrent;

namespace MajdataEdit.ChartShare;

public class HubDataService
{
    public List<ClientConnectDto> ConnectedUsers { get; set; } = new List<ClientConnectDto>();
    public string HostId { get; set; }
    public string Name { get; init; }
    public string Level { get; init; }
    public int Diff { get; init; }
    public string CurrentText { get; set; } = "";
    public float Offset { get; init; }
    public bool UseOgg { get; init; }
    public ConcurrentDictionary<string, RemoteCursor> UserCursors { get; } = new();

    private readonly object _lock = new object();
    private readonly diff_match_patch _dmp;

    public HubDataService(string name, int diff, string level, string text, float offset, bool useOgg)
    {
        Name = name;
        Diff = diff;
        Level = level;
        CurrentText = text;
        Offset = offset;
        UseOgg = useOgg;
        _dmp = new diff_match_patch();
    }

    // 应用补丁并更新服务器状态
    public bool ApplyPatch(string patchText)
    {
        lock (_lock)
        {
            try
            {
                var patches = _dmp.patch_fromText(patchText);
                var results = _dmp.patch_apply(patches, CurrentText);

                CurrentText = (string)results[0];

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}