static public class GameLayers
{
	// Layer 0: Default
	public const int Default_Index = 0;
	public const int Default_Mask = 1;

	// Layer 1: TransparentFX
	public const int TransparentFX_Index = 1;
	public const int TransparentFX_Mask = 2;

	// Layer 2: Ignore Raycast
	public const int IgnoreRaycast_Index = 2;
	public const int IgnoreRaycast_Mask = 4;

	// Layer 4: Water
	public const int Water_Index = 4;
	public const int Water_Mask = 16;

	// Layer 5: UI
	public const int UI_Index = 5;
	public const int UI_Mask = 32;

	// Layer 8: Player
	public const int Player_Index = 8;
	public const int Player_Mask = 256;

	// Layer 9: PlayerTrigger
	public const int PlayerTrigger_Index = 9;
	public const int PlayerTrigger_Mask = 512;

	// Layer 10: Scannable
	public const int Scannable_Index = 10;
	public const int Scannable_Mask = 1024;

	// Layer 11: PlayerSense
	public const int PlayerSense_Index = 11;
	public const int PlayerSense_Mask = 2048;

	// Layer 12: Critter
	public const int Critter_Index = 12;
	public const int Critter_Mask = 4096;

	// Layer 13: CritterTag
	public const int CritterTag_Index = 13;
	public const int CritterTag_Mask = 8192;

	// Layer 29: Solid
	public const int Solid_Index = 29;
	public const int Solid_Mask = 536870912;

	// Layer 30: SceneClick
	public const int SceneClick_Index = 30;
	public const int SceneClick_Mask = 1073741824;

	// Layer 31: LayoutRegion
	public const int LayoutRegion_Index = 31;
	public const int LayoutRegion_Mask = -2147483648;
}