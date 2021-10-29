using System;using BeauUtil;

static public class JobIds
{
	static public readonly StringHash32 Kelp_urchin_barren = new StringHash32(0x46814808);
	static public readonly StringHash32 Kelp_save_urchin_barren = new StringHash32(0x59D83768);
	static public readonly StringHash32 Kelp_bull_kelp_forest = new StringHash32(0x5E9238A2);
	static public readonly StringHash32 Kelp_welcome = new StringHash32(0x92603728);
	static public readonly StringHash32 Coral_urchin_friends = new StringHash32(0x05B98BBE);
	static public readonly StringHash32 Coral_invade = new StringHash32(0x2C3115E0);
	static public readonly StringHash32 Coral_fishy_bizz = new StringHash32(0x2FFC356C);
	static public readonly StringHash32 Coral_clear_sarg = new StringHash32(0x991B0962);
	static public readonly StringHash32 Coral_remove_sarg = new StringHash32(0xF4302A73);
	static public readonly StringHash32 Bayou_oxygen_tracking = new StringHash32(0xADC978AC);
	static public readonly StringHash32 Bayou_shrimp_tastrophe = new StringHash32(0xDFEDE55E);
	static public readonly StringHash32 Bayou_save_our_shrimp = new StringHash32(0xED435E90);
	static public readonly StringHash32 Arctic_time_of_death = new StringHash32(0x50882F92);
	static public readonly StringHash32 Arctic_missing_whale = new StringHash32(0x90AB11F0);
	static public readonly StringHash32 Arctic_whale_csi = new StringHash32(0xE7CCE2F5);

	static public readonly StringHash32[] All = new StringHash32[]
	{
		Kelp_urchin_barren,
		Kelp_save_urchin_barren,
		Kelp_bull_kelp_forest,
		Kelp_welcome,
		Coral_urchin_friends,
		Coral_invade,
		Coral_fishy_bizz,
		Coral_clear_sarg,
		Coral_remove_sarg,
		Bayou_oxygen_tracking,
		Bayou_shrimp_tastrophe,
		Bayou_save_our_shrimp,
		Arctic_time_of_death,
		Arctic_missing_whale,
		Arctic_whale_csi,
	};

	static public int IndexOf(StringHash32 inJobId)
	{
		return Array.IndexOf(All, inJobId);
	}
}