using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

public class PaymentHub : Hub
{
    public Task JoinOrderGroup(string orderCode)
    {
        return Groups.Add(Context.ConnectionId, orderCode);
    }

    public override Task OnConnected()
    {
        string userId = Context.QueryString["userId"];

        if (!string.IsNullOrEmpty(userId))
        {
            Groups.Add(Context.ConnectionId, userId);
        }

        return base.OnConnected();
    }
}