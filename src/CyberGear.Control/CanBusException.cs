namespace CyberGear.Control
{
	public class CanBusException : Exception
	{
		public CanBusException() { }
		public CanBusException(string message)
			: base(message) { throw new Exception(message); }
		public CanBusException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
