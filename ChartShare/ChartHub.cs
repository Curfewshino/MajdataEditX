using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.ChartShare;

public class ChartHub : Hub<IEditorClient>
{
    private readonly HubDataService dataService;

    public ChartHub(HubDataService dataService)
    {
        this.dataService = dataService;
    }

    public async Task MovingOrTyping(StateChangeDto data)
    {
        dataService.ApplyPatch(data.PatchText);
        await Clients.Others.OnMovingOrTyping(data);
    }

    public async Task GuestInit(ClientConnectDto data)
    {
        data.UserId = Context.ConnectionId;

        if (data.isHost)
        {
            dataService.HostId = data.UserId;
        }
        await Clients.Caller.OnJoined(new GuestInitDto()
        {
            UserId = data.UserId,
            Name = dataService.Name,
            Diff = dataService.Diff,
            Level = dataService.Level,
            Text = dataService.CurrentText,
            Offset = dataService.Offset,
            UseOgg = dataService.UseOgg
        });
        
        await Clients.Others.OnGuestJoined(data);
    }

    public async Task SaveFumen(bool isStateChangeOnly)
    {
        if (isStateChangeOnly) await Clients.All.OnSaveFumen("");
        else await Clients.Client(dataService.HostId).OnSaveFumen(dataService.CurrentText);
    }
}