namespace Juno.Payload.Client
{
    /// <summary>
    /// Class used for resolving through IOptions pattern through config only.
    /// </summary>
    public class PayloadClientOptions
    {
        public string ServiceUri { get; set; }
        public string MSIScope { get; set; }
    }
}
