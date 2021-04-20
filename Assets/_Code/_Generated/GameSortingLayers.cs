using System;

static public class GameSortingLayers
{
	public const int Skybox = -1434789061;
	public const int Background = 1170053551;
	public const int Default = 0;
	public const int Foreground = -1667136459;
	public const int ReallyClose = -54970771;
	public const int WorldUI = 1779977885;
	public const int Cutscene = 1161918811;
	public const int AboveCutscene = 1727326735;
	public const int System = 1053015367;

	static public readonly int[] Order = new int[]
	{
		-1434789061,
		1170053551,
		0,
		-1667136459,
		-54970771,
		1779977885,
		1161918811,
		1727326735,
		1053015367,
	};

	static public int IndexOf(int inSortingLayerId)
	{
		return Array.IndexOf(Order, inSortingLayerId);
	}
}