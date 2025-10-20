using System.ServiceModel;

namespace SnakeAndLadders.Contracts.Faults
{
    public static class Faults
    {
        public static FaultException<ServiceFault> Create(string code, string message, string correlationId = null)
        {
            var detail = new ServiceFault { Code = code, Message = message, CorrelationId = correlationId };
            return new FaultException<ServiceFault>(detail, new FaultReason(message));
        }
    }
}
