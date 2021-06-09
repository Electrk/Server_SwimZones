function defaultValue ( %value, %default )
{
	return (%value $= "" ? %default : %value);
}

function SelectiveSwimming_init ()
{
	if ( isObject (SelectiveSwimmingSO) )
	{
		SelectiveSwimmingSO.delete ();
	}

	// This is the ScriptObject we use for most of this mod's logic.
	MissionCleanup.add (new ScriptObject (SelectiveSwimmingSO)
	{
		class = SelectiveSwimming;
	});

	// Initialize mod-related variables and preferences.
	SelectiveSwimmingSO.initPrefs ();

	// Start the main loop.
	SelectiveSwimmingSO.loop ();
}

function SelectiveSwimming::initPrefs ()
{
	//* Constants *//

	// Various mod values.
	$SelectiveSwimming::LoopTick = 33;

	// Properties of the players' swim zones.
	$SelectiveSwimming::WaterViscosity = 40;
	$SelectiveSwimming::WaterDensity = 0.7;
	$SelectiveSwimming::WaterGravityMod = 0;

	// How to scale the swim zone according to the player's bounding box.
	$SelectiveSwimming::WaterScaleMultX = 0.5;
	$SelectiveSwimming::WaterScaleMultY = 0.5;
	$SelectiveSwimming::WaterScaleMultZ = 0.4;

	// We don't want to pollute the $TypeMasks::* variable space.
	$SelectiveSwimming::TypeMask = $TypeMasks::CorpseObjectType
		| $TypeMasks::ItemObjectType
		| $TypeMasks::PlayerObjectType
		| $TypeMasks::VehicleObjectType;

	//* Preferences *//

	$Pref::Server::SelSwim::SurfaceHeight = defaultValue ($Pref::Server::SelSwim::SurfaceHeight, 30);
}
