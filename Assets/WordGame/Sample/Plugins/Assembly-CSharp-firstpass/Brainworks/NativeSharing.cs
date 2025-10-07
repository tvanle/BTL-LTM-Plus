namespace Brainworks
{
	public class NativeSharing
	{
		private static INativShare _implementation;

		public static void Share(string body, string filePath = null, string url = null, string subject = "", string mimeType = "text/html", bool chooser = false, string chooserText = "Select sharing app")
		{
		}

		public static void ShareMultiple(string body, string[] filePaths = null, string url = null, string subject = "", string mimeType = "text/html", bool chooser = false, string chooserText = "Select sharing app")
		{
		}

		private static INativShare NativeCode()
		{
			return null;
		}
	}
}
