using System;

namespace HideAndSeek.Client.Utils
{
    public class TimeUtils
    {
		//https://dirask.com/posts/C-NET-get-current-timestamp-xpzzxp
		public static int ToUnixTimeSeconds(DateTime date)
		{
			DateTime point = new DateTime(1970, 1, 1);
			TimeSpan time = date.Subtract(point);

			return (int)time.TotalSeconds;
		}

		public static int ToUnixTimeSeconds()
		{
			return ToUnixTimeSeconds(DateTime.UtcNow);
		}
	}
}
