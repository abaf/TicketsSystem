using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Message;

namespace AccessProxy
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ITicketCallbackService))]
    public interface ITicketService
    {
        [OperationContract(IsOneWay = false)]
        void Register();

        [OperationContract(IsOneWay = false)]
        void SendReqeust(BuyTicketRequest request);
    }

    public interface ITicketCallbackService
    {
        [OperationContract(IsOneWay = true)]
        void SendResponse(BuyTicketResponse response);
    }
}
