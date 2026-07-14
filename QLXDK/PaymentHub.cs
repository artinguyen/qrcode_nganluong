using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

public class PaymentHub : Hub
{
    // Hàm này cho phép Client (trình duyệt) tham gia vào một nhóm riêng dựa trên mã đơn hàng của họ
    public Task JoinOrderGroup(string orderCode)
    {
        return Groups.Add(Context.ConnectionId, orderCode);
    }
}