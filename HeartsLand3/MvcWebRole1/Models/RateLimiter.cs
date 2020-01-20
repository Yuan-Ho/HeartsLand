using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Web.Helpers;

namespace MvcWebRole1.Models
{
	public enum CarryType
	{
		Register,
		Login,
		CreateBoard,
		CreateSelection,
		CreateDiscussion,
		CreateLetter,
		EditLetter,
		ControlLetter,
		VoteLetter,
	}
	public class RateLimiter
	{
		private DateTime[] amongLastCarryTimes;
		private int[] amongCarryIntervals;		// second

		public RateLimiter()
		{
			int cnt = Enum.GetNames(typeof(CarryType)).Length;
			amongLastCarryTimes = new DateTime[cnt];

			amongCarryIntervals = new int[cnt];

			amongCarryIntervals[(int)CarryType.Register] = 60;
			amongCarryIntervals[(int)CarryType.Login] = 3;
			amongCarryIntervals[(int)CarryType.CreateBoard] = 60 * 60;
			amongCarryIntervals[(int)CarryType.CreateSelection] = 60 * 60;
			amongCarryIntervals[(int)CarryType.CreateDiscussion] = 5;

			amongCarryIntervals[(int)CarryType.CreateLetter] = 1;
			amongCarryIntervals[(int)CarryType.EditLetter] = 5;
			amongCarryIntervals[(int)CarryType.ControlLetter] = 5;
			amongCarryIntervals[(int)CarryType.VoteLetter] = 1;

#if DEBUG
			for (int i = 0; i < amongCarryIntervals.Length; i++)
				amongCarryIntervals[i] *= 10;
#endif
		}
		public int/*need to wait seconds*/ CanCarry(CarryType type)
		{
			int idx = (int)type;

			DateTime lct = amongLastCarryTimes[idx];
			int ci = amongCarryIntervals[idx];

			if (lct == DateTime.MinValue)
				return 0;

			int elapsed = (int)Math.Truncate((DateTime.Now - lct).TotalSeconds);

			if (elapsed >= ci)		// = for 0 second interval to work.
				return 0;
			return ci - elapsed;
		}
		public void Carry(CarryType type)
		{
			int idx = (int)type;
			amongLastCarryTimes[idx] = DateTime.Now;
		}
		public object Validate(CarryType type)
		{
			if (!ReCaptcha.Validate())
			{
				int wait_seconds = CanCarry(type);
				if (wait_seconds > 0)
					return new { captcha_or_wait_seconds = wait_seconds };
			}
			Carry(type);
			return null;
		}
	}
}