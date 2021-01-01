using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	public enum ThreatStatus : byte
	{
		NoAggro,
		NotTanking,
		NotTankingOverTankThreat,
		InsecurelyTanking,
		SecurelyTanking,
	}


	public struct ThreatInfo
	{
		public ThreatStatus Status;
		public byte ThreatPct;
		public float ThreatPctRaw;
		public Int32 TotalThreatValue;
	}
}
