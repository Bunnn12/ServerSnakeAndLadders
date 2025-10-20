namespace SnakeAndLadders.Contracts.Faults
{
    
    public sealed class ServiceFault
    {
        public string Code { get; set; }          
        public string Message { get; set; }       
        public string CorrelationId { get; set; } 
    }
}
